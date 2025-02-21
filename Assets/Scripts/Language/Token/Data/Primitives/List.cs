using System.Collections;
using System.Collections.Generic;

public partial class Primitive : Data {
	public partial class List : Primitive {
		// defines internal type with name and memory
		public static Type InternalType = new("List", new Dictionary<string, Data>() {
		});

		public List<Data> Value; // internal value

		public List(List<Data> value) : base(InternalType) { // default constructor
			Value = value;
		}

		// methods
	}
}