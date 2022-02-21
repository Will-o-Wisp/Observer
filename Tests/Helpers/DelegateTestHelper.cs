using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.Helpers {
		
	public class DelegateTestHelper {

		public DelegateTestHelper() {
			ThisItems = new List<int>();
			for(int i=0; i<10; i++) ThisItems.Add(i);
		}

		#region Static Methods

		public static int Add(int x, int y) {
			return x+y;
		}

		public static int AddAndSet(ref int x, ref int y, int target = 0) {
			int result = x+y;
			x = target;
			y = target;
			return result;
		}

		public static void AddAndSetFirst(out int result, ref int x, in int y, int target = 0) {
			result = x+y;
			x = target;
		}

		public static int AddAllDefaults(int x=1, int y=2, int z=3) {
			return x+y+z;
		}

		#endregion

		#region Non-Static Methods

		public int sum = 0;
		public void AddToSum(int number) {
			sum+=number;
		}

		public int GetSum() {
			return sum;
		}

		public string Concatenated { get; set; } = "";

		public string Append(ref string appending, out string old) {
			old = Concatenated;
			Concatenated += appending;
			return Concatenated;
		}

		public string AppendMany(out string old, params string[] appending) {
			old = Concatenated;
			foreach(string item in appending) Concatenated += item;
			return Concatenated;
		}

		public string AppendManyWithSpacing(out string old, in char spacing=' ', params object[] appending) {
			old = Concatenated;
			foreach(string item in appending) Concatenated += item.ToString() + spacing;
			return Concatenated;
		}

		#endregion

		#region Properties
		private List<int> ThisItems{ get; }

		public int this[in int key]{
			get => ThisItems[key];
			set => ThisItems[key] = value;
		}

		public string Text { get; set; }

		#endregion
	}
}
