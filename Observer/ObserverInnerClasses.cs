using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Observation;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace Observation {
	public partial class Observer<ObjectType, ObjInterface> : DispatchProxy
        where ObjectType : class, ObjInterface
        where ObjInterface: class, IObserver{

        private static readonly object[] VoidObjectList = new object[]{ };

		private class CallMapping {
            public bool IsObserved{ get; set; } = false;
            public bool IsRelaying{ get; set; } = false;
            public Func<object, object[], object> Call{ get; set; } = null;
            public Func<object, object[], object> GetPropertyCall{ get; set; } = null;
            public String PropertyName{ get; set; } = null;

            public object GetPropertyValue(object instance) {
                return GetPropertyCall(instance, VoidObjectList);
            }

            public CallMapping(){ }

            public CallMapping(MethodInfo method) {
                Call = DelegateGenerator.GenerateDetached(method);
            }

            //Property
            public CallMapping(String propertyname, MethodInfo method,
                Func<object, object[], object> getcall = null) {

                PropertyName = propertyname;
                Call = DelegateGenerator.GenerateDetached(method);
                GetPropertyCall = getcall;
            }
        }

        private class RelayingDelegates {
            public PropertyChangedEventHandler PropertyChanged { get; set; } = null;
            public NotifyCollectionChangedEventHandler CollectionChanged{ get; set; } = null;
            public object Target { get; set; } = null;
            public string PropertyName { get; set; } = null;
        }

        private class RelayingPropertyData {
            private CallMapping Call;
            public bool FireCollectionEvents{ get; } = false;
            public bool FirePropertyEvents{ get; } = false;
            public string PropertyName{ get; }

            public object GetPropertyValue(object instance ) {
                return Call.GetPropertyValue(instance);
            }

            public RelayingPropertyData(PropertyInfo property, CallMapping mapping) {
                Debug.Assert(property!=null && mapping!=null);
                Call = mapping;
                PropertyName = property.Name;

                Relay atr = (Relay) property.GetCustomAttribute(typeof(Relay));
                Debug.Assert(atr!=null);

                if (atr.Collection) {
                    FireCollectionEvents = true;
                    if(!typeof(INotifyCollectionChanged).IsAssignableFrom(property.PropertyType))
                        throw new Exception("Property \"" + PropertyName + "\" was marked to relay " +
                        "CollectionChanged events, but it does not implement INotifyCollectionChanged interface");
                }

                if (atr.Property) {
                    FirePropertyEvents = true;
                    if(!typeof(INotifyPropertyChanged).IsAssignableFrom(property.PropertyType))
                        throw new Exception("Property \"" + PropertyName + "\" was marked to relay " +
                        "PropertyChanged events, but it does not implement INotifyPropertyChanged interface");
                }

                if(!FireCollectionEvents && !FirePropertyEvents) throw new Exception("Property \""
                    + PropertyName + "\" was marked to relay events, but specified no event type to relay.");
            }
        }
	}
}