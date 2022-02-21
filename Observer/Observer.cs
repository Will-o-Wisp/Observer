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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Observation;
using System.Linq;
using System.Reflection;

namespace Observation {
    public partial class Observer<ObjectType, ObjInterface> : DispatchProxy
        where ObjectType : class, ObjInterface
        where ObjInterface: class, IObserver{

        public Observer() { Initialize(); }

        public ObjectType Instance{ 
            get => instance;
            set => Setup(value);
        }

        private ObjectType instance = null;

        private Dictionary<string, RelayingDelegates> RelayingMappings { get; }
            = new Dictionary<string, RelayingDelegates>();

        protected override object Invoke( MethodInfo targetMethod, object[] args ) {
            if(!CallMappings.ContainsKey(targetMethod)) 
                throw new Exception("An unmapped method was found '" + targetMethod.Name + "'");
                //return targetMethod.Invoke(Instance, args);

            var mapping = CallMappings[targetMethod];
            if(!mapping.IsObserved && !mapping.IsRelaying)
                return mapping.Call(Instance, args);

            object oldvalue = mapping.GetPropertyValue(Instance);
            object returnvalue = mapping.Call(Instance, args);
            object newvalue = mapping.GetPropertyValue(Instance);

            if(Object.Equals(oldvalue, newvalue)) return returnvalue;

            if(mapping.IsObserved) Instance.ObservedChanged(mapping.PropertyName);
            if(mapping.IsRelaying) AddRelaying(RelayingProperties[mapping.PropertyName]);

            return returnvalue;
        }

        private void Clear() {
            RelayingMappings.Keys.ToList().ForEach(key => RemoveRelaying(key));
            instance = null;
        }

        private void Setup(ObjectType instance) {
            Clear();
            this.instance = instance;

            foreach(var relaying in RelayingProperties.Values)
                AddRelaying(relaying);
        }

        ~Observer() {
            Clear();
        }

        private void AddRelaying(RelayingPropertyData relaying) {
            Debug.Assert(relaying!=null);
            RemoveRelaying(relaying.PropertyName);
            Debug.Assert(!RelayingMappings.ContainsKey(relaying.PropertyName));

            object item  = relaying.GetPropertyValue(instance);
            if(item == null) return;

            RelayingDelegates newmappings = new RelayingDelegates() {
                PropertyName = relaying.PropertyName,
                Target = item
            };

            if (relaying.FirePropertyEvents) {
                Debug.Assert(typeof(INotifyPropertyChanged).IsAssignableFrom(item.GetType()));
                PropertyChangedEventHandler handler = new PropertyChangedEventHandler(
					(object sender, PropertyChangedEventArgs e)=>{
						Instance.RelayingChanged(relaying.PropertyName, sender, e);
				});

				(item as INotifyPropertyChanged).PropertyChanged += handler;
                newmappings.PropertyChanged = handler;
            }

			if(relaying.FireCollectionEvents) {
                Debug.Assert(typeof(INotifyCollectionChanged).IsAssignableFrom(item.GetType()));
                NotifyCollectionChangedEventHandler handler = new NotifyCollectionChangedEventHandler(
					(object sender, NotifyCollectionChangedEventArgs e ) => { 
						Instance.RelayingCollectionChanged(relaying.PropertyName, sender, e);
				});

				(item as INotifyCollectionChanged).CollectionChanged += handler;
                newmappings.CollectionChanged = handler;
			}

            Debug.Assert(newmappings.PropertyChanged != null
                || newmappings.CollectionChanged != null);
            RelayingMappings[relaying.PropertyName] = newmappings;
        }

        private void RemoveRelaying(string propertyname) {
            if(!RelayingMappings.ContainsKey(propertyname)) return;

            var mappings = RelayingMappings[propertyname];
            Debug.Assert(mappings!=null && mappings.Target!=null);

            if(mappings.CollectionChanged != null)
                ((INotifyCollectionChanged) mappings.Target).CollectionChanged
                    -= mappings.CollectionChanged;

            if(mappings.PropertyChanged != null)
                ((INotifyPropertyChanged) mappings.Target).PropertyChanged
                    -= mappings.PropertyChanged;
            
            RelayingMappings.Remove(propertyname);
        }

        public static bool HasReferenceParameters(MethodInfo method) {
		    if(method == null) throw new Exception("Method is null");

		    var found = method.GetParameters().ToList().Find(
		        param => param.IsOut || param.IsIn || param.ParameterType.IsByRef);

		    if(found == null) return false;
		    return true;
		}

        //--------------------------------------------------------------------------------------------------

        /// <summary>
        /// Use this method to generate a proxy that derives from Observer and implements
        /// ObjInterface interface. Instance is used as the target object when
        /// an ObjInterface method is called through the generated proxy.
        /// </summary>
        /// <param name="instance">The object that proxy methods are bound to</param>
        /// <returns>The proxy that implements ObjInterface</returns>
        public static ObjInterface GenerateProxy(ObjectType instance){
            var proxy = Create<ObjInterface, Observer<ObjectType, ObjInterface>>();
            (proxy as Observer<ObjectType, ObjInterface>).Instance = instance;
            return proxy;
		}

        //--------------------------------------------------------------------------------------------------

    }
}