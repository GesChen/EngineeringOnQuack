using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : Data {
	public partial class Bool : Primitive {
		public static Bool Default = new(false);

		// defines internal type with name and memory
		public static Type InternalType = new("Bool", new Dictionary<string, Data>() {
			{ "eq"			, new Function(eq)			},
			{ "lt"			, new Function(lt)			},
			{ "tostring"	, new Function(tostring)	}
		});

		public bool Value; // internal value

		public Bool(bool value) : base(InternalType) { // default constructor
			Value = value;
		}

		public override string ToString() {
			return (tostring(this, new()) as String).Value;
		}

		#region methods
		public static Data eq(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("eq", 1, args.Count);
			if (args[0] is not Bool) return new Bool(false);
			return new Bool((thisRef as Bool).Value == (args[0] as Bool).Value);
		}
		public static Data lt(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("lt", 1, args.Count);
			if (args[0] is not Bool)
				return Errors.CannotCompare("Bool", args[0].Type.Name);
			return new Bool(BtoI(thisRef) < BtoI(args[0]));
		}
		private static int BtoI(Data b) => (b as Bool).Value ? 1 : 0;
		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);

			return new String((thisRef as Bool).Value ? "true" : "false");
		}
		#endregion
	}
}