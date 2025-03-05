using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token {
	public partial class Name : Token {
		public string Value;

		public Name(string name) {
			Value = name;
		}

		public override string ToString() {
			return $"#N {Value}";
		}
	}
}
