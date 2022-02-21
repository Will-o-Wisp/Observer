using NUnit.Framework;
using Observation;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Tests.Helpers;

namespace Tests {

	using Helper = DelegateTestHelper;

	class DelegateGeneratorTest {

		static MethodInfo GetMethodInfo(string name) {
			return typeof(Helper).GetMethod(name,
				BindingFlags.Static | BindingFlags.Public |
				BindingFlags.NonPublic | BindingFlags.Instance);
		}

		static void GetPropertyMethods(in string name, out MethodInfo set, out MethodInfo get) {
			var property = typeof(Helper).GetProperty(name);
			set = property.GetSetMethod();
			get = property.GetGetMethod();
		}

		static Func<object[], object> GetMethod(Helper helper, string name) {
			var method = GetMethodInfo(name);
			return DelegateGenerator.Generate(method, helper);
		}

		static Func<object, object[], object> GetDetached(Helper helper, string name) {
			var method = GetMethodInfo(name);
			return DelegateGenerator.GenerateDetached(method);
		}

		static object[] EmptyObjects = new object[]{ };

		//-------------------------------------------------------------------------------------------------

		#region Generate

		#region Static Method Tests

		[Test]
		public void TestAdd() {
			Helper helper = new Helper();
			var add = GetMethod(helper, nameof(Helper.Add));

			object result = add(new object[]{ 5, 10 });
			Assert.IsTrue((int) result == 15);
		}

		[Test]
		public void TestAndSet() {
			Helper helper = new Helper();
			var AddAndSet = GetMethod(helper, nameof(Helper.AddAndSet));

			object[] items = new object[]{ 5, 10 };
			object result = AddAndSet(items);
			Assert.IsTrue((int) result == 15);
			Assert.IsTrue((int) items[0] == 0 && (int) items[1] == 0);

			object[] items2 = new object[]{ 10, 20, 30 };
			result = AddAndSet(items2);
			Assert.IsTrue((int) result == 30);
			Assert.IsTrue((int) items2[0] == 30 && (int) items2[1] == 30);
		}

		[Test]
		public void TestAddSetFirst() {
			Helper helper = new Helper();
			var AddAndSetFirst = GetMethod(helper, nameof(Helper.AddAndSetFirst));

			object[] items = new object[]{ null, 5, 10 };
			object result = AddAndSetFirst(items);

			Assert.IsTrue(result == null);
			Assert.IsTrue((int) items[0] == 15 && (int) items[1] == 0 && (int) items[2] == 10);

			items = new object[]{ 0, 10, 20, 50 };
			result = AddAndSetFirst(items);

			Assert.IsTrue(result == null);
			Assert.IsTrue((int) items[0] == 30 && (int) items[1] == 50
				&& (int) items[2] == 20 && (int) items[3] == 50);
		}

		[Test]
		public void TestAddAllDefaults() {
			Helper helper = new Helper();
			var AddAllDefaults = GetMethod(helper, nameof(Helper.AddAllDefaults));

			object[] items = EmptyObjects;
			object result = AddAllDefaults(items);
			Assert.IsTrue((int) result == 6);

			items = new object[]{ 5 };
			result = AddAllDefaults(items);
			Assert.IsTrue((int) result == 10);

			items = new object[]{ 5, 10 };
			result = AddAllDefaults(items);
			Assert.IsTrue((int) result == 18);

			items = new object[]{ 5, 10, 15 };
			result = AddAllDefaults(items);
			Assert.IsTrue((int) result == 30);
		}

		#endregion

		#region Non-Static Method Tests

		[Test]
		public static void TestAddToSumGetSum() {
			Helper helper = new Helper();
			var addToSum = GetMethod(helper, nameof(Helper.AddToSum));
			var getSum = GetMethod(helper, nameof(Helper.GetSum));

			Assert.IsTrue(helper.sum == 0);

			object result = addToSum(new object[]{ 5 });
			Assert.IsTrue(result == null);

			result = getSum(EmptyObjects);
			Assert.IsTrue((int) result == 5 && helper.sum == 5);

			result = addToSum(new object[]{ 10 });
			Assert.IsTrue(result == null);

			result = getSum(EmptyObjects);
			Assert.IsTrue((int) result == 15 && helper.sum == 15);
		}

		[Test]
		public static void TestAppend() {
			Helper helper = new Helper();
			var append = GetMethod(helper, nameof(Helper.Append));
			Assert.IsTrue(helper.Concatenated == "");

			object[] items = new object[]{ "Hello World!", "This should become empty" };
			object result = append(items);
			Assert.IsTrue((string) result == "Hello World!");
			Assert.IsTrue((string)items[1] == "");

			items = new object[]{ " Nice day for fishing, ain't it?", "" };
			result = append(items);
			Assert.IsTrue((string) result == "Hello World! Nice day for fishing, ain't it?");
			Assert.IsTrue((string)items[1] == "Hello World!");
		}

		[Test]
		public static void TestAppendMany() {
			Helper helper = new Helper();
			var append = GetMethod(helper, nameof(Helper.AppendMany));
			Assert.IsTrue(helper.Concatenated == "");

			object[] items = new object[]{ "This should become empty", new string[]{ "Hello ", "World!" } };
			object result = append(items);
			Assert.IsTrue((string) result == "Hello World!");
			Assert.IsTrue((string) items[0] == "");
		}

		[Test]
		public static void TestAppendManyWithSpacing() {
			Helper helper = new Helper();
			var append = GetMethod(helper, nameof(Helper.AppendManyWithSpacing));
			Assert.IsTrue(helper.Concatenated == "");
			object[] items = new object[]{ "This should become empty", ' ', new string[]{ "Hello", "World!" } };
			
			object result = append(items);
			Assert.IsTrue((string) result == "Hello World! ");
			Assert.IsTrue((string) items[0] == "");
		}

		#endregion

		#region Properties

		[Test]
		public void TestItem() {
			Helper helper = new Helper();
			MethodInfo getinfo, setinfo;
			GetPropertyMethods("Item", out setinfo, out getinfo);

			var get = DelegateGenerator.Generate(getinfo, helper);
			var set = DelegateGenerator.Generate(setinfo, helper);

			object result = set(new object[]{ 0, 5 });
			Assert.IsTrue(result == null);
			result = get(new object[]{ 0 });
			Assert.IsTrue((int) result == 5);
			Assert.IsTrue(helper[0] == 5);

			result = set(new object[]{ 1, 10 });
			Assert.IsTrue(result == null);
			result = get(new object[]{ 1 });
			Assert.IsTrue((int) result == 10);
			Assert.IsTrue(helper[1] == 10);
		}

		[Test]
		public void TestText() {
			Helper helper = new Helper();
			MethodInfo getinfo, setinfo;
			GetPropertyMethods("Text", out setinfo, out getinfo);

			var get = DelegateGenerator.Generate(getinfo, helper);
			var set = DelegateGenerator.Generate(setinfo, helper);

			Assert.IsTrue(String.IsNullOrEmpty(helper.Text));

			object result = set(new object[]{ "Hello World!" });
			Assert.IsTrue(result == null);
			result = get(EmptyObjects);
			Assert.IsTrue((string) result == "Hello World!");
			Assert.IsTrue(helper.Text == "Hello World!");
		}

		#endregion

		#endregion

		//-------------------------------------------------------------------------------------------------

		#region GenerateDetached

		#region Static Method Tests

		[Test]
		public void TestDetachedAdd() {
			Helper helper = new Helper();
			var add = GetDetached(helper, nameof(Helper.Add));

			object result = add(helper, new object[]{ 5, 10 });
			Assert.IsTrue((int) result == 15);
		}

		[Test]
		public void TestDetachedAndSet() {
			Helper helper = new Helper();
			var AddAndSet = GetDetached(helper, nameof(Helper.AddAndSet));

			object[] items = new object[]{ 5, 10 };
			object result = AddAndSet(helper, items);
			Assert.IsTrue((int) result == 15);
			Assert.IsTrue((int) items[0] ==0 && (int) items[1] == 0);

			object[] items2 = new object[]{ 10, 20, 30 };
			result = AddAndSet(helper, items2);
			Assert.IsTrue((int) result == 30);
			Assert.IsTrue((int) items2[0] == 30 && (int) items2[1] == 30);
		}

		[Test]
		public void TestDetachedAddSetFirst() {
			Helper helper = new Helper();
			var AddAndSetFirst = GetDetached(helper, nameof(Helper.AddAndSetFirst));

			object[] items = new object[]{ 0, 5, 10 };
			object result = AddAndSetFirst(helper, items);

			Assert.IsTrue(result == null);
			Assert.IsTrue((int) items[0] == 15 && (int) items[1] == 0 && (int) items[2] == 10);

			items = new object[]{ 0, 10, 20, 50 };
			result = AddAndSetFirst(helper, items);

			Assert.IsTrue(result == null);
			Assert.IsTrue((int) items[0] == 30 && (int) items[1] == 50
				&& (int) items[2] == 20 && (int) items[3] == 50);
		}

		[Test]
		public void TestDetachedAddAllDefaults() {
			Helper helper = new Helper();
			var AddAllDefaults = GetDetached(helper, nameof(Helper.AddAllDefaults));

			object[] items = EmptyObjects;
			object result = AddAllDefaults(helper, items);
			Assert.IsTrue((int) result == 6);

			items = new object[]{ 5 };
			result = AddAllDefaults(helper, items);
			Assert.IsTrue((int) result == 10);

			items = new object[]{ 5, 10 };
			result = AddAllDefaults(helper, items);
			Assert.IsTrue((int) result == 18);

			items = new object[]{ 5, 10, 15 };
			result = AddAllDefaults(helper, items);
			Assert.IsTrue((int) result == 30);
		}
		#endregion

		#region Non-Static Method Tests

		[Test]
		public static void TestDetachedAddToSumGetSum() {
			Helper helper = new Helper();
			var addToSum = GetDetached(helper, nameof(Helper.AddToSum));
			var getSum = GetDetached(helper, nameof(Helper.GetSum));

			Assert.IsTrue(helper.sum == 0);

			object result = addToSum(helper, new object[]{ 5 });
			Assert.IsTrue(result == null);

			result = getSum(helper, EmptyObjects);
			Assert.IsTrue((int) result == 5 && helper.sum == 5);

			result = addToSum(helper, new object[]{ 10 });
			Assert.IsTrue(result == null);

			result = getSum(helper, EmptyObjects);
			Assert.IsTrue((int) result == 15 && helper.sum == 15);
		}

		[Test]
		public static void TestDetachedAppend() {
			Helper helper = new Helper();
			var append = GetDetached(helper, nameof(Helper.Append));
			Assert.IsTrue(helper.Concatenated == "");

			object[] items = new object[]{ "Hello World!", "This should become empty" };
			object result = append(helper, items);
			Assert.IsTrue((string) result == "Hello World!");
			Assert.IsTrue((string)items[1] == "");

			items = new object[]{ " Nice day for fishing, ain't it?", "" };
			result = append(helper, items);
			Assert.IsTrue((string) result == "Hello World! Nice day for fishing, ain't it?");
			Assert.IsTrue((string)items[1] == "Hello World!");
		}

		[Test]
		public static void TestDetachedAppendMany() {
			Helper helper = new Helper();
			var append = GetDetached(helper, nameof(Helper.AppendMany));
			Assert.IsTrue(helper.Concatenated == "");

			object[] items = new object[]{ "This should become empty", new string[]{ "Hello ", "World!" } };
			object result = append(helper, items);
			Assert.IsTrue((string) result == "Hello World!");
			Assert.IsTrue((string) items[0] == "");
		}

		[Test]
		public static void TestDetachedAppendManyWithSpacing() {
			Helper helper = new Helper();
			var append = GetDetached(helper, nameof(Helper.AppendManyWithSpacing));
			Assert.IsTrue(helper.Concatenated == "");
			object[] items = new object[]{ "This should become empty", ' ', new string[]{ "Hello", "World!" } };
			
			object result = append(helper, items);
			Assert.IsTrue((string) result == "Hello World! ");
			Assert.IsTrue((string) items[0] == "");
		}

		#endregion

		#region Properties

		[Test]
		public void TestDetachedItem() {
			Helper helper = new Helper();
			MethodInfo getinfo, setinfo;
			GetPropertyMethods("Item", out setinfo, out getinfo);

			var get = DelegateGenerator.GenerateDetached(getinfo);
			var set = DelegateGenerator.GenerateDetached(setinfo);

			object result = set(helper, new object[]{ 0, 5 });
			Assert.IsTrue(result == null);
			result = get(helper, new object[]{ 0 });
			Assert.IsTrue((int) result == 5);
			Assert.IsTrue(helper[0] == 5);

			result = set(helper, new object[]{ 1, 10 });
			Assert.IsTrue(result == null);
			result = get(helper, new object[]{ 1 });
			Assert.IsTrue((int) result == 10);
			Assert.IsTrue(helper[1] == 10);
		}

		[Test]
		public void TestDetachedText() {
			Helper helper = new Helper();
			MethodInfo getinfo, setinfo;
			GetPropertyMethods("Text", out setinfo, out getinfo);

			var get = DelegateGenerator.GenerateDetached(getinfo);
			var set = DelegateGenerator.GenerateDetached(setinfo);

			Assert.IsTrue(String.IsNullOrEmpty(helper.Text));

			object result = set(helper, new object[]{ "Hello World!" });
			Assert.IsTrue(result == null);
			result = get(helper, EmptyObjects);
			Assert.IsTrue((string) result == "Hello World!");
			Assert.IsTrue(helper.Text == "Hello World!");
		}

		#endregion

		#endregion
	}

}