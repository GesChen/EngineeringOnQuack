using System.Collections;
using System.Collections.Generic;

public partial class Primitive : Data {
	public partial class Number : Primitive {
		// defines internal type with name and memory
		public static Type InternalType = new("Number", new Dictionary<string, Data>() { 
			{"testmethod", new Function(TestMethod)}
		});

		public double Value; // internal value

		public Number(double value) : base(InternalType) { // default constructor
			Value = value;
		}

		// method
		public static Data TestMethod(List<Data> args) {
			return new Data();
		}
	}
}