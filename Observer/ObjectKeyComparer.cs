using System;
using System.Collections.Generic;
using System.Text;

namespace Observation {
	internal class ObjectKeyComparer : IEqualityComparer<object> {
		public new bool Equals(object x, object y) {
			return x.Equals(y);
		}

		public int GetHashCode(object obj) {
			return obj.GetHashCode();
		}
	}
}
