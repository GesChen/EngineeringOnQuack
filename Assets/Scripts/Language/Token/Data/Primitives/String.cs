using System.Collections;
using System.Collections.Generic;

public partial class Primitive : Data {
	public partial class String : Primitive {
		// defines internal type with name and memory
		public static Type InternalType = new("String", new Dictionary<string, Data>() {
		});

		public string Value; // internal value

		public String(string value) : base(InternalType) { // default constructor
			Value = value;
		}

		// methods
		public static Data Upper(List<Data> args) {
			if (args.Count != 0) 
		}
	}
}