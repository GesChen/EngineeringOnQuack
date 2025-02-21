using System.Collections;
using System.Collections.Generic;

public partial class Primitive : Data {
	public partial class Bool : Primitive {
		// defines internal type with name and memory
		public static Type InternalType = new("Bool", new Dictionary<string, Data>() {
		});

		public bool Value; // internal value

		public Bool(bool value) : base(InternalType) { // default constructor
			Value = value;
		}

		// methods
	}
}