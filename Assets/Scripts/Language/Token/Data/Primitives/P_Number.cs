using System.Collections;
using System.Collections.Generic;
using System;

public abstract partial class Primitive : Data {
	public partial class Number : Primitive {
		public static Number Default = new();
		
		// defines internal type with name and memory
		public static Type InternalType = new("Number", new Dictionary<string, Data>() {
			{ "eq",			new Function("eq", eq)		},
			{ "lt",			new Function("lt", lt)		},
			{ "ad",			new Function("ad", ad)		},
			{ "su",			new Function("su", su)		},
			{ "mu",			new Function("mu", mu)		},
			{ "di",			new Function("di", di)		},
			{ "mo",			new Function("mo", mo)		},
			{ "po",			new Function("po", po)		},
			{ "tostring",	new Function("tostring", tostring)	}
		});

		public double Value; // internal value

		public Number(double value) : base(InternalType) { // default constructor
			Value = value;
		}
		public Number(Number original) : base(original) {
			Value = original.Value;
		}
		public Number() : base(InternalType) {
			Value = 0;
		}
		public override string ToString() {
			return (tostring(this, new()) as String).Value;
		}

		public override Data Copy() {
			return new Number(this);
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
		public static Data ad(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("ad", 1, args.Count);
			if (args[0] is not Number othernum) return Errors.UnsupportedOperation("+", "Number", args[0].Type.Name);

			return new Number((thisRef as Number).Value + othernum.Value);
		}
		public static Data su(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("su", 1, args.Count);
			if (args[0] is not Number othernum) return Errors.UnsupportedOperation("-", "Number", args[0].Type.Name);

			return new Number((thisRef as Number).Value - othernum.Value);
		}
		public static Data mu(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("mu", 1, args.Count);
			if (args[0] is not Number othernum) return Errors.UnsupportedOperation("*", "Number", args[0].Type.Name);


			return new Number((thisRef as Number).Value * othernum.Value);
		}
		public static Data di(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("di", 1, args.Count);
			if (args[0] is not Number othernum) return Errors.UnsupportedOperation("/", "Number", args[0].Type.Name);

			if (othernum.Value == 0)
				return Errors.DivisonByZero();

			return new Number((thisRef as Number).Value / othernum.Value);
		}
		public static Data mo(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("mo", 1, args.Count);
			if (args[0] is not Number othernum) return Errors.UnsupportedOperation("%", "Number", args[0].Type.Name);

			// python mod code
			double a = (thisRef as Number).Value;
			double b = othernum.Value;
			if (b == 0)
				return Errors.DivisonByZero();

			double remainder = a % b;
			if (remainder != 0 && Math.Sign(a) != Math.Sign(b))
				return new Number(remainder + b);

			return new Number(remainder);
		}
		public static Data po(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("po", 1, args.Count);
			if (args[0] is not Number othernum) return Errors.UnsupportedOperation("^", "Number", args[0].Type.Name);

			return new Number(Math.Pow((thisRef as Number).Value, othernum.Value));
		}
		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);
			return new String((thisRef as Number).Value.ToString());
		}
		#endregion
	}
}