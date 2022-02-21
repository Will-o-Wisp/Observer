using NUnit.Framework;
using Observation;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace Tests {

	class RelayingTest {

		private interface Test{
			string ToString();
		}

		private interface IKeyValue : IObserver, INotifyPropertyChanged, Test {

			[Observe]
			string Key { get; set; }

			[Observe]
			string Value { get; set; }

		}

		private class KeyValue : IKeyValue {

			public event PropertyChangedEventHandler PropertyChanged;
			public string Key { get; set; }
			public string Value { get; set; }

			private KeyValue(string Key, string Value) {
				this.Key = Key;
				this.Value = Value;
			}

			public override string ToString() {
				return $"{Key} {Value}";
			}

			public static IKeyValue New(string Key, string Value) {
				return Observer<KeyValue, IKeyValue>.GenerateProxy(new KeyValue(Key, Value));
			}

			public void ObservedChanged(string propertyname) {
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
			}

			public void RelayingChanged(string propertyname, object sender, PropertyChangedEventArgs e) {
				throw new NotImplementedException();
			}

			public void RelayingCollectionChanged(string propertyname, object sender, NotifyCollectionChangedEventArgs e) {
				throw new NotImplementedException();
			}
		}

		private interface IChangesTracker : IObserver {

			[Relay(Collection = true)][Observe]
			ObservableCollection<IKeyValue> Collection{ get; set; }

			List<string> Changes { get; }

			public string LastChange { get; }
		}

		private class ChangesTracker : IChangesTracker {

			public ObservableCollection<IKeyValue> Collection{ get; set; }
				= new ObservableCollection<IKeyValue>();

			public List<string> Changes { get; } = new List<string>();

			private ChangesTracker() { }

			public string LastChange => Changes[Changes.Count - 1];
			

			public void ObservedChanged(string propertyname) {
				Changes.Add("Changed Collection");
			}

			public void RelayingChanged(string propertyname,
				object sender, PropertyChangedEventArgs e) {
				throw new NotImplementedException();
			}

			public void RelayingCollectionChanged(string propertyname,
				object sender, NotifyCollectionChangedEventArgs e) {
				//we only check add in this test
				if(e.Action != NotifyCollectionChangedAction.Add) return;
				Changes.Add($"{e.NewItems[0].ToString()}");
				int index = e.NewStartingIndex;
				(e.NewItems[0] as IKeyValue).PropertyChanged +=
					(object sender, PropertyChangedEventArgs e) =>
						Changes.Add($"Collection item {index} changed");
			}

			private void ChangesTracker_PropertyChanged(object sender, PropertyChangedEventArgs e) {
				for(int i=0; i<Collection.Count; i++) {
					if(sender == Collection[i]) {
						Changes.Add($"Collection item {i} changed");
					}
				}
			}

			public static IChangesTracker New() {
				return Observer<ChangesTracker, IChangesTracker>.GenerateProxy(new ChangesTracker());
			}
		}

		[Test]
		public void IndirectRelayingTest() {
			IChangesTracker Tracker = ChangesTracker.New();
			Tracker.Collection.Add(KeyValue.New("key1", "value1"));

			Assert.IsTrue(Tracker.Changes.Count > 0);
			Assert.IsTrue(Tracker.LastChange == "key1 value1");

			Tracker.Collection = new ObservableCollection<IKeyValue>();
			Assert.IsTrue(Tracker.LastChange == "Changed Collection");
			Assert.IsTrue(Tracker.Collection.Count == 0);

			Tracker.Collection.Add(KeyValue.New("key2", "value2"));
			Assert.IsTrue(Tracker.Collection.Count == 1);
			Assert.IsTrue(Tracker.LastChange == "key2 value2");

			Tracker.Collection.Add(KeyValue.New("key3", "value3"));
			Assert.IsTrue(Tracker.Collection.Count == 2);
			Assert.IsTrue(Tracker.LastChange == "key3 value3");

			Tracker.Collection[1].Key = "ChangedKey";
			Assert.IsTrue(Tracker.LastChange == "Collection item 1 changed");
		}


	}
}
