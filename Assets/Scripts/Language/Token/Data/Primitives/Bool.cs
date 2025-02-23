using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : Data {
	public partial class Bool : Primitive {
		public static Bool Default = new(false);

		// defines internal type with name and memory
		public static Type InternalType = new("Bool", new Dictionary<string, Data>() {
			{ "tostring", new Function(tostring) }
		});

		public bool Value; // internal value

		public Bool(bool value) : base(InternalType) { // default constructor
			Value = value;
		}

		public override string ToString() {
			return (tostring(this, new()) as String).Value;
		}

		// methods

		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvaidArgumentCount("tostring", 0, args.Count);

			return new String((thisRef as Bool).Value ? "true" : "false");
		}
	}
}