using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : T_Data {
	public partial class Bool : Primitive {
		public static Bool Default = new();

		// defines internal type with name and memory
		public static Type InternalType = new("Bool", new Dictionary<string, T_Data>() {
			{ "eq"			, new Function("eq", eq)			},
			{ "lt"			, new Function("lt", lt)			},
			{ "mu"			, new Function("mu", mu)			},
			{ "tostring"	, new Function("tostring", tostring)	}
		});

		public bool Value; // internal value

		public Bool(bool value) : base(InternalType) { // default constructor
			Value = value;
		}
		public Bool(Bool original) : base(original) {
			Value = original.Value;
		}
		public Bool() : base(InternalType) {
			Value = false;
		}

		public override string ToString() {
			return (tostring(this, new()) as String).Value;
		}

		public override T_Data Copy() {
			return new Bool(this);
		}

		#region methods
		public static T_Data eq(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("eq", 1, args.Count);
			if (args[0] is not Bool) return new Bool(false);
			return new Bool((thisRef as Bool).Value == (args[0] as Bool).Value);
		}
		public static T_Data lt(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("lt", 1, args.Count);
			if (args[0] is not Bool)
				return Errors.CannotCompare("Bool", args[0].Type.Name);
			
			static int BtoI(T_Data b) => (b as Bool).Value ? 1 : 0;
			return new Bool(BtoI(thisRef) < BtoI(args[0]));
		}

		public static T_Data mu(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("mu", 1, args.Count);
			if (args[0] is not Bool b) {
				T_Data cast = args[0].Cast(InternalType);
				if (cast is Error) return cast;

				b = cast as Bool;
			}

			return new Bool((thisRef as Bool).Value && b.Value);
		}
		public static T_Data tostring(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);

			return new String((thisRef as Bool).Value ? "true" : "false");
		}
		#endregion
	}
}