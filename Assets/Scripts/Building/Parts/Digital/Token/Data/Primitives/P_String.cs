using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public abstract partial class Primitive : Data {
	public partial class String : Primitive {
		public static String Default = new();

		// defines internal type with name and memory
		public static Type InternalType = new("String", new Dictionary<string, Data>() {
			{ "eq"			, new Function("eq"			, eq)			},
			{ "lt"			, new Function("lt"			, lt)			},
			{ "ad"			, new Function("ad"			, ad)			},
			{ "mu"			, new Function("mu"			, mu)			},
			{ "tostring"	, new Function("tostring"	, tostring)		},
			{ "upper"		, new Function("upper"		, upper)		},
			{ "lower"		, new Function("lower"		, lower)		},
			{ "count"		, new Function("count"		, count)		},
			{ "contains"	, new Function("contains"	, contains)		},
			{ "startswith"	, new Function("startswith"	, startswith)	},
			{ "endswith"	, new Function("endswith"	, endswith)		},
			{ "find"		, new Function("find"		, find)			},
			{ "allletters"	, new Function("allletters"	, allletters)	},
			{ "allnumbers"	, new Function("allnumbers"	, allnumbers)	},
			{ "allsymbols"	, new Function("allsymbols"	, allsymbols)	},
			{ "alllower"	, new Function("alllower"	, alllower)		},
			{ "allupper"	, new Function("allupper"	, allupper)		},
			{ "allspace"	, new Function("allspace"	, allspace)		},
			{ "trimleft"	, new Function("trimleft"	, trimleft)		},
			{ "trimright"	, new Function("trimright"	, trimright)	},
			{ "trim"		, new Function("trim"		, trim)			},
			{ "stripleft"	, new Function("stripleft"	, stripleft)	},
			{ "stripright"	, new Function("stripright"	, stripright)	},
			{ "strip"		, new Function("strip"		, strip)		},
			{ "replace"		, new Function("replace"	, replace)		},
			{ "split"		, new Function("split"		, split)		}
		});

		public string Value; // internal value

		public String(string value) : base(InternalType) { // default constructor
			Value = value;
		}
		public String(String original) : base(original) {
			Value = original.Value;
		}
		public String() : base(InternalType) {
			Value = "";
		}

		public override string ToString() {
			return (tostring(this, new()) as String).Value;
		}

		public override Data Copy() {
			return new String(this);
		}

		#region methods
		public static Data eq(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("eq", 1, args.Count);
			if (args[0] is not String) return new Bool(false);
			return new Bool((thisRef as String).Value == (args[0] as String).Value);
		}
		public static Data lt(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("lt", 1, args.Count);
			if (args[0] is not String)
				return Errors.CannotCompare("String", args[0].Type.Name);
			return new Bool(LessThan((thisRef as String).Value, (args[0] as String).Value));
		}
		public static Data ad(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("ad", 1, args.Count);
			if (args[0] is not String str) {
				Data cast = args[0].Cast(InternalType);
				if (cast is Error) return cast;

				str = cast as String;
			}

			return new String((thisRef as String).Value + str.Value);
		}
		public static Data mu(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("mu", 1, args.Count);

			Data castToNum = args[0].Cast(Number.InternalType);
			if (castToNum is Error)
				return Errors.UnsupportedOperation("*", "String", "Non-numeric String");

			double amount = (castToNum as Number).Value;
			if (amount != Math.Floor(amount))
				return Errors.UnsupportedOperation("*", "String", "Non-whole Number");

			string repeated = System.String.Concat(Enumerable.Repeat((thisRef as String).Value, (int)amount));
			return new String(repeated);
		}
		
		private static bool LessThan(string a, string b) {
			int minLen = Math.Min(a.Length, b.Length);
			for (int i = 0; i < minLen; i++) {
				if (a[i] < b[i]) return true;
				else if (a[i] > b[i]) return false;
			}
			return a.Length < b.Length;
		}
		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);

			return new String((thisRef as String).Value); // make sure its copied
		}

		public static Data upper(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("upper", 0, args.Count);
			return new String((thisRef as String).Value.ToUpper());
		}

		public static Data lower(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("lower", 0, args.Count);
			return new String((thisRef as String).Value.ToLower());
		}

		public static Data count(Data thisRef, List<Data> args) { // MIGHT cause issues
			if (args.Count != 1) return Errors.InvalidArgumentCount("count", 1, args.Count);
			string text = (thisRef as String).Value;
			string value = (args[0].Cast(InternalType) as String).Value;

			int count = 0, minIndex = text.IndexOf(value, 0);
			while (minIndex != -1) {
				minIndex = text.IndexOf(value, minIndex + value.Length);
				count++;
			}

			return new Number(count);
		}

		public static Data contains(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("contains", 1, args.Count);
			string text = (thisRef as String).Value;
			string value = (args[0].Cast(InternalType) as String).Value;

			return new Bool(text.Contains(value));
		}

		public static Data startswith(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("startswith", 1, args.Count);
			string text = (thisRef as String).Value;
			string value = (args[0].Cast(InternalType) as String).Value;

			return new Bool(text.StartsWith(value));
		}

		public static Data endswith(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("endswith", 1, args.Count);
			string text = (thisRef as String).Value;
			string value = (args[0].Cast(InternalType) as String).Value;

			return new Bool(text.EndsWith(value));
		}

		public static Data find(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("find", 1, args.Count);
			string text = (thisRef as String).Value;
			string value = (args[0].Cast(InternalType) as String).Value;

			return new Number(text.IndexOf(value));
		}

		public static Data allletters(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("allletters", 0, args.Count);
			string text = (thisRef as String).Value;

			return new Bool(text.All(char.IsLetter));
		}

		public static Data allnumbers(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("allnumbers", 0, args.Count);
			string text = (thisRef as String).Value;

			return new Bool(text.All(char.IsNumber));
		}

		public static Data allsymbols(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("allsymbols", 0, args.Count);
			string text = (thisRef as String).Value;

			return new Bool(text.All(char.IsSymbol));
		}

		public static Data alllower(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("alllower", 0, args.Count);
			string text = (thisRef as String).Value;

			return new Bool(text.All(char.IsLower));
		}

		public static Data allupper(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("allupper", 0, args.Count);
			string text = (thisRef as String).Value;

			return new Bool(text.All(char.IsUpper));
		}

		public static Data allspace(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("allspace", 0, args.Count);
			string text = (thisRef as String).Value;

			return new Bool(text.All(char.IsWhiteSpace));
		}

		public static Data trimleft(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("trimleft", 0, args.Count);
			string text = (thisRef as String).Value;

			return new String(text.TrimStart());
		}

		public static Data trimright(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("trimright", 0, args.Count);
			string text = (thisRef as String).Value;

			return new String(text.TrimEnd());
		}

		public static Data trim(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("trim", 0, args.Count);
			string text = (thisRef as String).Value;

			return new String(text.Trim());
		}

		public static Data stripleft(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("stripleft", 1, args.Count);
			string text = (thisRef as String).Value;
			string value = (args[0].Cast(InternalType) as String).Value;

			return new String(text.TrimStart(value.ToCharArray()));
		}

		public static Data stripright(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("stripright", 1, args.Count);
			string text = (thisRef as String).Value;
			string value = (args[0].Cast(InternalType) as String).Value;

			return new String(text.TrimEnd(value.ToCharArray()));
		}

		public static Data strip(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("strip", 1, args.Count);
			string text = (thisRef as String).Value;
			string value = (args[0].Cast(InternalType) as String).Value;

			return new String(text.Trim(value.ToCharArray()));
		}

		public static Data replace(Data thisRef, List<Data> args) {
			if (args.Count != 2) return Errors.InvalidArgumentCount("replace", 2, args.Count);
			string text = (thisRef as String).Value;
			string from = (args[0].Cast(InternalType) as String).Value;
			string to = (args[1].Cast(InternalType) as String).Value;

			return new String(text.Replace(from, to));
		}

		public static Data split(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("split", 1, args.Count);
			string text = (thisRef as String).Value;
			string value = (args[0].Cast(InternalType) as String).Value;

			return new List(text.Split(value).Select(s => new String(s) as Data).ToList());
		}
		#endregion
	}
}