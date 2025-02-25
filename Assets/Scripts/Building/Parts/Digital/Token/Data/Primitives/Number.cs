using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : Data {
	public partial class Number : Primitive {
		public static Number Default = new(0);
		
		// defines internal type with name and memory
		public static Type InternalType = new("Number", new Dictionary<string, Data>() {
			{ "eq",			new Function(eq)		},
			{ "lt",			new Function(lt)		},
			{ "tostring",	new Function(tostring)	}
		});

		public double Value; // internal value

		public Number(double value) : base(InternalType) { // default constructor
			Value = value;
		}

		public override string ToString() {
			return (tostring(this, new()) as String).Value;
		}


		#region methods
		public static Data eq(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("eq", 1, args.Count);
			if (args[0] is not Number) return new Bool(false);
			return new Bool((thisRef as Number).Value == (args[0] as Number).Value);
		}
		public static Data lt(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("lt", 1, args.Count);
			if (args[0] is not Number)
				return Errors.CannotCompare("Number", args[0].Type.Name);
			return new Bool((thisRef as Number).Value < (args[0] as Number).Value);
		}

		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);
			return new String(((thisRef as Number).Value).ToString());
		}
		#endregion
	}
}