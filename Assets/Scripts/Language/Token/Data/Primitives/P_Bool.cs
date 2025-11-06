using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

public abstract partial class Primitive : T_Data {
	public partial class Bool : Primitive {
		public static Bool Default = new();

		// defines internal type with name and memory
		static Type m_IT;
		public static Type InternalType =>
			HF.LoadCached(ref m_IT, 
			() => new("Bool", new Dictionary<string, T_Data>() {
			{ "eq"			, new Function("eq", eq)			},
			{ "lt"			, new Function("lt", lt)			},
			{ "mu"			, new Function("mu", mu)			},
			{ "num"			, new Function("num", num)			},
			{ "str"			, new Function("str", str)			},
			{ "list"		, new Function("list", list)		},
		}));

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
			return (str(this, new()) as String).Value;
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

		public static T_Data num(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("num", 0, args.Count);

			return new Number((thisRef as Bool).Value ? 1 : 0);
		}
		public static T_Data str(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("str", 0, args.Count);

			return new String((thisRef as Bool).Value ? "true" : "false");
		}
		public static T_Data list(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("list", 0, args.Count);

			return new List(new List<T_Data>() { thisRef as Bool });
		}
		#endregion
	}
}