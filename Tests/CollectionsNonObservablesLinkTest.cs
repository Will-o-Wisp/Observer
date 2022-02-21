using NUnit.Framework;
using Observation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Tests {
	public class CollectionsNonObservablesLinkTest {

		[Test]
		public void HashSetTest() {
			var ObservableSet = new ObservableHashSet<int>(){ 1, 2, 3 };
			var NonObjervableHashSet = new HashSet<int>();
			ObservableSet.ReflectOn(NonObjervableHashSet);

			Assert.IsTrue(NonObjervableHashSet.Count == 3);
			for(int i=1; i<=3; i++)
				Assert.IsTrue(NonObjervableHashSet.Contains(i));

			ObservableSet.Remove(2);
			Assert.IsTrue(NonObjervableHashSet.Count == 2);
			Assert.IsFalse(NonObjervableHashSet.Contains(2));

			var StringSet = new HashSet<string>() { "test" };
			ObservableSet.ReflectOn(StringSet, (int x) => "-" + x.ToString() + "-", 
				(string x) => int.Parse(x.Substring(1, 1)));

			Assert.IsTrue(StringSet.Count == 2);

			ObservableSet.UnlinkReflection(NonObjervableHashSet);
			ObservableSet.Remove(3);

			Assert.IsTrue(StringSet.Count == 1);
			Assert.IsTrue(NonObjervableHashSet.Count == 2);
			Assert.IsTrue(StringSet.Contains("-1-"));
			Assert.IsTrue(NonObjervableHashSet.Contains(1));
			Assert.IsTrue(NonObjervableHashSet.Contains(3));

			ObservableSet.Clear();
			Assert.IsTrue(StringSet.Count == 0);
			Assert.IsTrue(NonObjervableHashSet.Count == 2);

			ObservableSet.ReflectOn(NonObjervableHashSet,
				ReflectInitialization.NonObservable);
			Assert.IsTrue(ObservableSet.Count == 2);

			foreach(var item in NonObjervableHashSet)
				Assert.IsTrue(ObservableSet.Contains(item));
		}

		[Test]
		public void DictionaryTest() {
			var observed = new ObservableDictionary<string, int>() {
				{ "1" , 1 },
				{ "2" , 2 },
				{ "3" , 3 },
			};

			var StringIntDict = new Dictionary<string, int>() { { "test", 1 } };
			var IntIntDict = new Dictionary<int, int>() { { 0, 0 } };
			var IntStringDict = new Dictionary<int, string>() { { 0, "0" } };

			//----------------------

			var IntToString = new Observation.IValueConverter<int, string>() {
				Convert = (int x) => x.ToString(),
				ConvertBack = (string x) => int.Parse(x)
			};

			var StringToInt = new Observation.IValueConverter<string, int>() {
				Convert = (string x) => int.Parse(x),
				ConvertBack = (int x) => x.ToString()
			};

			var IntToInt = new Observation.IdentityConverter<int>();
			var StringToString = new Observation.IdentityConverter<string>();

			//----------------------

			observed.ReflectOn(StringIntDict);

			observed.ReflectOn(nonObservable : IntIntDict,
				keyConverters : StringToInt,
				valueConverters : IntToInt,
				initialization : ReflectInitialization.Observable);

			observed.ReflectOn(nonObservable : IntStringDict, 
				keyConverters : StringToInt,
				valueConverters : IntToString);

			Assert.IsTrue(StringIntDict.Count == 3);
			Assert.IsTrue(IntIntDict.Count == 3);
			Assert.IsTrue(IntStringDict.Count == 3);

			for(int i=1; i<=3; i++) {
				Assert.IsTrue(DictContains(IntIntDict, i, i));
				Assert.IsTrue(DictContains(StringIntDict, i, i));
				Assert.IsTrue(DictContains(IntStringDict, i, i));
			}

			observed.Remove("3");
			Assert.IsTrue(StringIntDict.Count == 2);
			Assert.IsTrue(IntIntDict.Count == 2);
			Assert.IsTrue(IntStringDict.Count == 2);
			Assert.IsFalse(DictContains(IntIntDict, 3, 3));
			Assert.IsFalse(DictContains(StringIntDict, 3, 3));
			Assert.IsFalse(DictContains(IntStringDict, 3, 3));

			observed.Clear();
			Assert.IsTrue(StringIntDict.Count == 0);
			Assert.IsTrue(IntIntDict.Count == 0);
			Assert.IsTrue(IntStringDict.Count == 0);

			observed.UnlinkReflection(StringIntDict);
			observed.UnlinkReflection(IntStringDict);

			observed["5"] = 10;
			Assert.IsTrue(StringIntDict.Count == 0);
			Assert.IsTrue(IntIntDict.Count == 1);
			Assert.IsTrue(IntStringDict.Count == 0);
			Assert.IsTrue(DictContains(IntIntDict, 5, 10));

			observed["5"] = 20;
			Assert.IsTrue(IntIntDict.Count == 1);
			Assert.IsTrue(DictContains(IntIntDict, 5, 20));
		}

		private static bool DictContains<SKey, SValue>(Dictionary<SKey, SValue> dict, int key, int value) {
			string hash = key.ToString() + ' ' + value.ToString();
			foreach(string keyvalue in dict.Select(x => Stringify(x)))
				if(hash == keyvalue) return true;
			return false;

			string Stringify(KeyValuePair<SKey, SValue> keyvalue){
				return keyvalue.Key.ToString() + ' ' + keyvalue.Value.ToString();
			}
		}

		[Test]
		public void CollectionTest() {
			var NonObservedIntList = new List<int>();
			var Observed = new ObservableCollection<int>(){ 1, 2, 3 };

			Observed.ReflectOn(NonObservedIntList);
			for(int i=1; i<=3; i++)
				Assert.IsTrue(NonObservedIntList.Contains(i));

			Observed.Remove(2);
			Assert.IsTrue(NonObservedIntList.Count == 2);
			Assert.IsFalse(NonObservedIntList.Contains(2));

			var NonObservedStringList = new List<string>(){ "test" };
			Observed.ReflectOn(NonObservedStringList, (int x) => "+" + x.ToString() + "+",
				(string x) => int.Parse(x.Substring(1, 1)));

			Assert.IsTrue(NonObservedStringList.Count == 2);
			Assert.IsTrue(NonObservedStringList.Contains("+1+"));
			Assert.IsTrue(NonObservedStringList.Contains("+3+"));

			Observed.Add(5);
			Assert.IsTrue(NonObservedStringList.Contains("+5+"));
			Assert.IsTrue(NonObservedIntList.Contains(5));

			Observed.UnlinkReflection(NonObservedIntList);
			Observed.Remove(5);
			Assert.IsFalse(NonObservedStringList.Contains("+5+"));
			Assert.IsTrue(NonObservedIntList.Contains(5));

			Observed.Clear();
			Assert.IsTrue(NonObservedStringList.Count == 0);
			Assert.IsTrue(NonObservedIntList.Count == 3);
		}

	}
}