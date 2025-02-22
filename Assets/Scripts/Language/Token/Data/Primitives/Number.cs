using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : Data {
	public partial class Number : Primitive {
		// defines internal type with name and memory
		public static Type InternalType = new("Number", new Dictionary<string, Data>() { 
		});

		public double Value; // internal value

		public Number(double value) : base(InternalType) { // default constructor
			Value = value;
		}

		public override string ToString() {
			return $"Number object {Value}";
		}

		// method
	}
}