using NUnit.Framework;
using Observation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Tests.Helpers;

namespace Tests {

	public class ObserverTests {

		private static FieldInfo GetMember(string fieldname) {
			FieldInfo result = typeof(ObserverTestHelper).GetField(fieldname, BindingFlags.Public |
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsTrue(result!=null);
			return result;
		}

		private static object GetPropertyValue(object instance, string propertyname) {
			var propertyinfo = instance.GetType().GetProperty(propertyname,
				BindingFlags.Public | BindingFlags.NonPublic);
			Assert.IsTrue(propertyinfo != null);
			object value = propertyinfo.GetValue(instance);
			Assert.IsTrue(value != null);
			return value;
		}

		private static void CheckLastEvent(ObserverTestHelper helper, int totalEvents,
			ObserverTestHelper.EventType type, string property) {

			Assert.IsTrue(helper.Fired.Count == totalEvents);
			var last = helper.Fired[totalEvents-1];

			Assert.IsTrue(last.Item1 == type);
			Assert.IsTrue(last.Item2 == property);
		}

		//-------------------------------------------------------------------------------------------

		[Test]
		public void TestNumber() {
			ObserverTestHelper original = new ObserverTestHelper();
			IObserverTestHelper proxy = Observer<ObserverTestHelper, IObserverTestHelper>.
				GenerateProxy(original);
			Assert.IsNotNull(proxy);

			proxy.Number = 5;
			CheckLastEvent(original, 1, ObserverTestHelper.EventType.Observed,
				nameof(ObserverTestHelper.Number));

			int x = proxy.Number;
			Assert.IsTrue(x == 5);

			proxy.Number = 10;
			CheckLastEvent(original, 2, ObserverTestHelper.EventType.Observed,
				nameof(ObserverTestHelper.Number));
		}

		[Test]
		public void TestText() {
			ObserverTestHelper original = new ObserverTestHelper();
			IObserverTestHelper proxy = Observer<ObserverTestHelper, IObserverTestHelper>.
				GenerateProxy(original);
			Assert.IsNotNull(proxy);

			proxy.SetText("Hello World");
			string returnedtext = proxy.GetText();
			Assert.IsTrue(returnedtext == "Hello World");

			proxy.SetText("Testing");
			returnedtext = proxy.GetText();
			Assert.IsTrue(returnedtext == "Testing");

			Assert.IsTrue(original.Fired.Count == 0);
		}

		[Test]
		public void TestNumbers() {
			ObserverTestHelper original = new ObserverTestHelper();
			IObserverTestHelper proxy = Observer<ObserverTestHelper,
				IObserverTestHelper>.GenerateProxy(original);
			Assert.IsNotNull(proxy);

			Assert.IsTrue(proxy.Numbers.Count == 0);

			proxy.Numbers.Add(1);
			Assert.IsTrue(proxy.Numbers.Count == 1 && proxy.Numbers[0] == 1);

			CheckLastEvent(original, 1, ObserverTestHelper.EventType.RelayingCollection,
				nameof(ObserverTestHelper.Numbers));

			proxy.Numbers.Add(2);
			Assert.IsTrue(proxy.Numbers.Count == 2 && proxy.Numbers[1] == 2);

			CheckLastEvent(original, 2, ObserverTestHelper.EventType.RelayingCollection,
				nameof(ObserverTestHelper.Numbers));

			proxy.Numbers = new Observation.ObservableCollection<int>();

			CheckLastEvent(original, 3, ObserverTestHelper.EventType.Observed,
				nameof(ObserverTestHelper.Numbers));

			proxy.Numbers.Add(150);

			CheckLastEvent(original, 4, ObserverTestHelper.EventType.RelayingCollection,
				nameof(ObserverTestHelper.Numbers));

			Assert.IsTrue(proxy.Numbers.Count == 1 && proxy.Numbers[0] == 150);
		}

		[Test]
		public void TestObservedImplementer() {
			ObserverTestHelper original = new ObserverTestHelper();
			IObserverTestHelper proxy = Observer<ObserverTestHelper,
				IObserverTestHelper>.GenerateProxy(original);
			Assert.IsNotNull(proxy);

			Assert.IsTrue(proxy.ObservedImplementer == null);

			proxy.ObservedImplementer = new NotifyImplementer();

			CheckLastEvent(original, 1, ObserverTestHelper.EventType.Observed,
				nameof(ObserverTestHelper.ObservedImplementer));

			proxy.ObservedImplementer.Number = 5;

			CheckLastEvent(original, 1, ObserverTestHelper.EventType.Observed,
				nameof(ObserverTestHelper.ObservedImplementer));

			proxy.ObservedImplementer = new NotifyImplementer();
			
			CheckLastEvent(original, 2, ObserverTestHelper.EventType.Observed,
				nameof(ObserverTestHelper.ObservedImplementer));
		}

		[Test]
		public void TestRelayingImplementer() {
			ObserverTestHelper original = new ObserverTestHelper();
			IObserverTestHelper proxy = Observer<ObserverTestHelper,
				IObserverTestHelper>.GenerateProxy(original);
			Assert.IsNotNull(proxy);

			Assert.IsTrue(original.RelayingImplementer == null);

			proxy.RelayingImplementer = new NotifyImplementer();

			Assert.IsTrue(original.Fired.Count == 0);

			proxy.RelayingImplementer.Number = 10;

			Assert.IsTrue(original.RelayingImplementer.Number == 10);

			CheckLastEvent(original, 1, ObserverTestHelper.EventType.Relaying,
				nameof(ObserverTestHelper.RelayingImplementer));
		}

		[Test]
		public void TestReplaceInstance() {
			ObserverTestHelper original = new ObserverTestHelper();
			IObserverTestHelper proxy = Observer<ObserverTestHelper,
				IObserverTestHelper>.GenerateProxy(original);
			Assert.IsNotNull(proxy);

			proxy.ObservedImplementer = new NotifyImplementer();
			CheckLastEvent(original, 1, ObserverTestHelper.EventType.Observed,
				nameof(ObserverTestHelper.ObservedImplementer));

			proxy.Number = 10;
			CheckLastEvent(original, 2, ObserverTestHelper.EventType.Observed,
				nameof(ObserverTestHelper.Number));

			ObserverTestHelper newer = new ObserverTestHelper();
			(proxy as Observer<ObserverTestHelper, IObserverTestHelper>).Instance = newer;

			proxy.Number = 10;
			CheckLastEvent(newer, 1, ObserverTestHelper.EventType.Observed,
				nameof(ObserverTestHelper.Number));
		}

		[Test]
		public void TestGetSum() {
			ObserverTestHelper original = new ObserverTestHelper();
			IObserverTestHelper proxy = Observer<ObserverTestHelper,
				IObserverTestHelper>.GenerateProxy(original);
			Assert.IsNotNull(proxy);

			int result = proxy.GetSum(5, 10, 15, 20, 25);
			Assert.IsTrue(result == 75);

			result = proxy.GetSum();
			Assert.IsTrue(result == 0);
		}

		[Test]
		public void TestGetAndSet() {
			ObserverTestHelper original = new ObserverTestHelper();
			IObserverTestHelper proxy = Observer<ObserverTestHelper,
				IObserverTestHelper>.GenerateProxy(original);
			Assert.IsNotNull(proxy);

			int result=5;
			int x=5, y=20;
			proxy.GetAndSet(out result, ref x, ref y);
			Assert.IsTrue(result == 25 && x ==0 && y == 0);
			
			proxy.GetAndSet(out result, ref x, ref y, 100);
			Assert.IsTrue(result == 0 && x == 100 && y == 100);
		}

		[Test]
		public void TestThisProperty() {
			ObserverTestHelper original = new ObserverTestHelper();
			IObserverTestHelper proxy = Observer<ObserverTestHelper,
				IObserverTestHelper>.GenerateProxy(original);
			Assert.IsNotNull(proxy);

			proxy["Hello"] = 5;
			Assert.IsTrue(proxy["Hello"] == 5);

			proxy["World"] = 10;
			Assert.IsTrue(proxy["World"] == 10);
		}

	}
}