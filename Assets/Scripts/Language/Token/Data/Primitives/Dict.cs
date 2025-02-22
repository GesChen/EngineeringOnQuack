using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : Data {
	public partial class Dict : Primitive {
		// defines internal type with name and memory
		public static Type InternalType = new("Dict", new Dictionary<string, Data>() {
		});

		public Dictionary<Data, Data> Value; // internal value

		public Dict(Dictionary<Data, Data> value) : base(InternalType) { // default constructor
			Value = value;
		}
		public Dict() : base(InternalType) { // empty constructor
			Value = new();
		}

		public override string ToString() {
			return $"Dict object (add proper serialize if needed)";
		}

		// methods
	}
}