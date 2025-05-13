using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token {
	public partial class T_Name : Token {
		public string Value;

		public T_Name(string name) {
			Value = name;
		}

		public override string ToString() {
			return $"#N {Value}";
		}
	}
}
