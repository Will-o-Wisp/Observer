using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Observation;
using System.Text;
using System.Reflection;

namespace Observation {
	public partial class Observer<ObjectType, ObjInterface> : DispatchProxy
		where ObjectType : class, ObjInterface
		where ObjInterface : class, IObserver {

		static bool IsInitialized { get; set; } = false;

		private static Dictionary<MethodInfo, CallMapping> CallMappings { get; }
			= new Dictionary<MethodInfo, CallMapping>();

		private static Dictionary<String, RelayingPropertyData> RelayingProperties { get; }
			= new Dictionary<string, RelayingPropertyData>();

		public static void Initialize() {
			if(IsInitialized)
				return;

			SetupMethods();

			foreach(var property in typeof(ObjInterface).GetProperties())
				MapProperty(property.Name);

			IsInitialized = true;
		}

		//----------------------------------------------------------------------

		private static void SetupMethods() {
			var intfaces = typeof(ObjInterface).FindInterfaces(
				(Type typeObj, Object criteriaObj) => true, null);

			List<Type> types = new List<Type>();
			foreach(var intface in intfaces) types.Add(intface);
			types.Add(typeof(ObjInterface));

			foreach(var type in types) {
				var properties = type.GetProperties();
				HashSet<MethodInfo> propertymethods = new HashSet<MethodInfo>();
				foreach(var property in properties) {
					var getmethod = property.GetGetMethod();
					var setmethod = property.GetSetMethod();

					if(getmethod != null)
						propertymethods.Add(getmethod);
					if(setmethod != null)
						propertymethods.Add(setmethod);
				}

				new HashSet<MethodInfo>(type.GetMethods())
					.Except(propertymethods).Where(x => !x.IsStatic)
					.ToList().ForEach(method => CallMappings[method] = new CallMapping(method));
			}
		}

		private static bool IsSpecialThisProperty(PropertyInfo property,
			CallMapping get, CallMapping set) {

			if(get == null && set == null)
				return false;

			if(property.Name == "Item") {
				if(get != null && property.GetGetMethod().GetParameters().Length == 1)
					return true;

				if(set != null && property.GetSetMethod().GetParameters().Length == 2)
					return true;
			}

			return false;
		}

		private static void MapProperty(string propertyname) {
			Debug.Assert(!String.IsNullOrEmpty(propertyname));

			PropertyInfo instanceProperty = typeof(ObjectType).GetProperty(propertyname);
			PropertyInfo interfaceProperty = typeof(ObjInterface).GetProperty(propertyname);
			Debug.Assert(instanceProperty != null && interfaceProperty != null);

			MethodInfo instancePropertyGet = typeof(ObjectType)
				.GetProperty(propertyname).GetGetMethod();
			MethodInfo instancePropertySet = typeof(ObjectType)
				.GetProperty(propertyname).GetSetMethod();

			CallMapping get = instancePropertyGet != null ?
				new CallMapping(propertyname, instancePropertyGet) : null;
			if(get != null)
				get.GetPropertyCall = get.Call;
			CallMapping set = instancePropertySet != null ?
				new CallMapping(propertyname, instancePropertySet,
				get != null ? get.Call : null) : null;

			bool isSpecialThisProperty = IsSpecialThisProperty(interfaceProperty, get, set);
			bool isObserved, isRelaying;
			CheckAttributes(interfaceProperty, out isObserved, out isRelaying);

			if(isSpecialThisProperty && (isObserved || isRelaying))
				throw new Exception(
					"Special 'this' property is an invalid target for observation or relaying");

			if((isObserved || isRelaying) && !(get != null && set != null))
				throw new Exception("Observed/Relaying property \"" + propertyname +
					"\" needs both a set and a get public method");

			if(isObserved)
				set.IsObserved = true;

			if(isRelaying) {
				set.IsRelaying = true;
				RelayingProperties[propertyname] = new RelayingPropertyData(interfaceProperty, get);
			}

			if(get != null) {
				MethodInfo interfaceGet = typeof(ObjInterface)
					.GetProperty(propertyname).GetGetMethod();
				Debug.Assert(interfaceGet != null);
				CallMappings[interfaceGet] = get;
			}

			if(set != null) {
				MethodInfo interfaceSet = typeof(ObjInterface)
					.GetProperty(propertyname).GetSetMethod();
				Debug.Assert(interfaceSet != null);
				CallMappings[interfaceSet] = set;
			}
		}

		private static void CheckAttributes(PropertyInfo prop,
			out bool observe, out bool relay) {

			observe = false;
			relay = false;

			foreach(var atr in prop.GetCustomAttributes()) {
				if(atr is Observe)
					observe = true;
				else if(atr is Relay)
					relay = true;
			}
		}

		private static bool IsRelayingType(Type type) {
			if(typeof(INotifyCollectionChanged).IsAssignableFrom(type) ||
				typeof(INotifyPropertyChanged).IsAssignableFrom(type))
				return true;
			return false;
		}
	}
}