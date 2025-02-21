using System.Collections;
using System.Collections.Generic;

public partial class Primitive : Data {
	public partial class Error : Primitive {
		// defines internal type with name and memory
		public static Type InternalType = new("Error", new Dictionary<string, Data>() {
		});

		public string Value; // internal value

		public Error(string value) : base(InternalType) { // default constructor
			Value = value;
		}

		// methods
	}
}