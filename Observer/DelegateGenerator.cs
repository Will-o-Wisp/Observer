/**********************************************************************************
 * MIT License																	  *
 * 																				  *
 * Copyright (c) 2020 Manos Chatzianestis										  *
 * 																				  *
 * Permission is hereby granted, free of charge, to any person obtaining a copy	  *
 * of this software and associated documentation files (the "Software"), to deal  *
 * in the Software without restriction, including without limitation the rights	  *
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell	  *
 * copies of the Software, and to permit persons to whom the Software is		  *
 * furnished to do so, subject to the following conditions:						  *
 * 																				  *
 * The above copyright notice and this permission notice shall be included in all *
 * copies or substantial portions of the Software.								  *
 * 																				  *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR	  *
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,		  *
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE	  *
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER		  *
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  *
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE  *
 * SOFTWARE.																	  *
 * ********************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Observation {

	/// <summary>
	/// This class is used to Create Generic Wrapper Delegates from MethodInfo objects
	/// </summary>
	public static class DelegateGenerator {

		#region LambdaData Inner Class
		private class LambdaData {

			//The code expressions to be compiled
			public List<Expression> Expressions { get; } 
				= new List<Expression>();

			//The code variable declarations
			public List<ParameterExpression> Variables { get; }
				= new List<ParameterExpression>();

			//The target method's out parameters
			public List<Tuple<ParameterExpression, int>> RefOutParameters { get; }
				= new List<Tuple<ParameterExpression, int>>();

			//The wrapper method's object[] parameter
			public ParameterExpression OuterParameters { get; }
				= Expression.Parameter(typeof(object[]), "OuterArgs");

			//The wrapper method's instance parameter
			public ParameterExpression OuterObjectInstanceParameter { get; set; }
				= Expression.Parameter(typeof(object), "Instance");

			//The target method
			public MethodInfo Method{ get; }

			//The target method's return value
			public ParameterExpression ReturnValue { get; set; } 

			//The target method call expression arguments
			public List<Expression> MethodCallArguments{ get; }
				= new List<Expression>();

			//The instance of the target method
			public object Instance{ get; set; }

			public BlockExpression GetBody() {
				return Expression.Block(typeof(object), 
					Variables, Expressions);
			}

			public LambdaData(MethodInfo method, object instance = null) {
				this.Method = method;
				this.Instance = instance;
			}
		}
		#endregion

		#region Private

		private static MethodCallExpression CreateMethodCall(MethodInfo method,
			Expression instance, Expression[] parameters = null) {

			Debug.Assert(method!=null);

			MethodCallExpression call = null;
			if(instance == null && parameters == null)
				call = Expression.Call(method);
			else if(parameters == null)
				call = Expression.Call(instance, method);
			else if(instance == null)
				call = Expression.Call(method, parameters);
			else call = Expression.Call(instance, method, parameters);
			return call;
		}

		private static Type GetAssignableType(Type type) {
			if(type.IsByRef) return type.GetElementType();
			else return type;
		}

		private static void SetupParameters(LambdaData data) {
			var parameters = data.Method.GetParameters();
			for(int i=0; i<parameters.Length; i++) {
				if(parameters[i].IsOut || parameters[i].ParameterType.IsByRef) HandleRefOut(i);
				else if(parameters[i].HasDefaultValue) HandleDefaultParameter(i);
				else data.MethodCallArguments.Add(
					Expression.Convert(
						Expression.ArrayAccess(data.OuterParameters, Expression.Constant(i)),
						GetAssignableType(parameters[i].ParameterType)
					));
			}

			void HandleRefOut(int index) {
				var refvar = Expression.Variable(GetAssignableType(parameters[index].ParameterType));
				data.Variables.Add(refvar);
				data.MethodCallArguments.Add(refvar);
				data.RefOutParameters.Add(new Tuple<ParameterExpression, int>(refvar, index));

				if(!parameters[index].IsOut)
					data.Expressions.Add(
						Expression.Assign(refvar, 
						Expression.Convert( 
							Expression.ArrayAccess(data.OuterParameters, Expression.Constant(index)),
							GetAssignableType(parameters[index].ParameterType))
						));
			}

			void HandleDefaultParameter(int index) {
				var defaultvar = Expression.Variable(GetAssignableType(parameters[index].ParameterType));
				data.Variables.Add(defaultvar);
				data.MethodCallArguments.Add(defaultvar);
				
				var ifthenelse = Expression.IfThenElse(
					Expression.LessThanOrEqual(Expression.ArrayLength(data.OuterParameters),
						Expression.Constant(index)),
					Expression.Assign(defaultvar, Expression.Constant(parameters[index].DefaultValue)),
					Expression.Assign(defaultvar, 
						Expression.Convert( Expression.ArrayAccess(data.OuterParameters,
						Expression.Constant(index)),
						GetAssignableType(parameters[index].ParameterType))
				));

				data.Expressions.Add(ifthenelse);
			}
		}

		private static void SetupReturn(LambdaData data){
			foreach(var parameter in data.RefOutParameters)
				data.Expressions.Add(Expression.Assign(
					Expression.ArrayAccess(data.OuterParameters, Expression.Constant(parameter.Item2)),
					Expression.Convert(parameter.Item1, typeof(object))
				));

			if(data.Method.ReturnType == typeof(void))
				data.Expressions.Add(Expression.Constant(null));
			else data.Expressions.Add(data.ReturnValue);
		}

		private static void SetupMethodCall(LambdaData data, bool embedded) {
			Expression instancexpr;

			if(data.Method.IsStatic) instancexpr = null;
			else if(embedded){
				if(data.Instance == null) instancexpr = null;
				else instancexpr = Expression.Constant(data.Instance);
			} else {
				instancexpr = Expression.Convert(data.OuterObjectInstanceParameter,
					data.Method.DeclaringType);
			}

			MethodCallExpression call = CreateMethodCall(data.Method,
				instancexpr, data.MethodCallArguments.ToArray());

			if(data.Method.ReturnType != typeof(void)) {
				data.ReturnValue = Expression.Variable(typeof(object));
				data.Variables.Add(data.ReturnValue);
				data.Expressions.Add(
					Expression.Assign(data.ReturnValue,
					Expression.Convert(call, typeof(object))));
			}else data.Expressions.Add(call);
		}

		#endregion

		#region Public - Interface

		/// <summary>
		/// <para>
		/// Similar to cref="Generate".
		/// Creates a delegate that expects the method's declaring object as its first argument.
		/// <para>
		/// <para>
		/// This is useful when a type's method delegate needs to be created once,
		/// and then reused between objects of the same type.
		/// </para>
		/// </summary>
		/// <param name="method">The method from which the delegate is to be generated from</param>
		/// <returns>A fast generic delegate method wrapper</returns>
		/// <see cref="Generate"/>
		public static Func<object, object[], object> GenerateDetached(MethodInfo method) {
			LambdaData data = new LambdaData(method);
		
			SetupParameters(data);
			SetupMethodCall(data, false);
			SetupReturn(data);

			return Expression.Lambda<Func<object, object[], object>>(data.GetBody(), 
				data.OuterObjectInstanceParameter, data.OuterParameters).Compile();
		}


		/// <summary>
		/// <para>
		/// Generates a generic delegate from a methodinfo and an optional object instance
		/// that the method belongs to. Provide no instance in case the method is static.
		/// </para>
		/// <para>
		/// When calling the generated delegate, the appropriate method arguments should
		/// be passed as an array of objects. The method's return value is returned as an object.
		/// If the method return type is void, the delegate returns null.
		/// </para>
		/// </summary>
		/// <remarks>
		/// <para>
		/// For efficiency, no checks are made for parameter correctness. It is expected
		/// that the correct method arguments are passed to the delegate.
		/// Calling incorrect parameters is likely to cause an error,
		/// but is otherwise undefined behaviour.
		/// </para>
		/// </remarks>
		/// <param name="method">The method from which the delegate is to be generated from</param>
		/// <param name="instance">An optional object instance that contains the method</param>
		/// <returns>A fast generic delegate method wrapper</returns>
		/// <seealso cref="GenerateDetached"/>
		public static Func<object[], object> Generate(MethodInfo method, object instance=null) {
			LambdaData data = new LambdaData(method, instance);
			SetupParameters(data);
			SetupMethodCall(data, true);
			SetupReturn(data);

			var result = Expression.Lambda<Func<object[], object>>(
				data.GetBody(), data.OuterParameters);
			return result.Compile();
		}

		#endregion

	}

}