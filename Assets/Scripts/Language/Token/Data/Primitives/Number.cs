using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : Data {
	public partial class Number : Primitive {
		public static Number Default = new(0);
		
		// defines internal type with name and memory
		public static Type InternalType = new("Number", new Dictionary<string, Data>() {
			{ "tostring", new Function(tostring) }
		});

		public double Value; // internal value

		public Number(double value) : base(InternalType) { // default constructor
			Value = value;
		}

		public override string ToString() {
			return (tostring(this, new()) as String).Value;
		}

		// method
		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvaidArgumentCount("tostring", 0, args.Count);
		
			return new String(((thisRef as Number).Value).ToString());
		}

	}
}