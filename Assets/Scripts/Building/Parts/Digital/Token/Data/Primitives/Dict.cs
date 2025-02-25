using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public abstract partial class Primitive : Data {
	public partial class Dict : Primitive {
		public static Dict Default = new();

		// defines internal type with name and memory
		public static Type InternalType = new("Dict", new Dictionary<string, Data>() {
			{ "eq"			, new Function(eq)			},
			{ "lt"			, new Function(lt)			},
			{ "tostring "	, new Function(tostring)	},
			{ "clear"		, new Function(clear)		},
			{ "values"		, new Function(values)		},
			{ "keys"		, new Function(keys)		},
			{ "removekey"	, new Function(removekey)	},
			{ "tolist"		, new Function(tolist)		}
		});

		public Dictionary<Data, Data> Value; // internal value

		public Dict(Dictionary<Data, Data> value) : base(InternalType) { // default constructor
			Value = value;
		}
		public Dict() : base(InternalType) { // empty constructor
			Value = new();
		}

		public override string ToString() {
			return (tostring(this, new()) as String).Value;
		}

		#region comparison operators

		#endregion

		#region methods
		public static Data eq(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("eq", 1, args.Count);
			if (args[0] is not Dict) return new Bool(false);

			Dictionary<Data, Data> dict1 = (thisRef as Dict).Value;
			Dictionary<Data, Data> dict2 = (args[0] as Dict).Value;

			// magic linq bs
			bool areEqual = dict1.Count == dict2.Count &&
				dict1.All(kvp => dict2.ContainsKey(kvp.Key) && dict2[kvp.Key].Equals(kvp.Value));

			return new Bool(areEqual);
		}
		public static Data lt(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("lt", 1, args.Count);
			if (args[0] is not Dict)
				return Errors.CannotCompare("Dict", args[0].Type.Name);
			return new Bool((thisRef as Dict).Value.Count < (args[0] as Dict).Value.Count);
		}
		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);

			// init
			Dictionary<Data, Data> v = (thisRef as Dict).Value;
			StringBuilder sb = new();
			sb.Append("{");

			int i = 0;
			foreach (KeyValuePair<Data, Data> kv in v) {
				// cast key to string
				Data tryCast = kv.Key.Cast(String.InternalType);
				if (tryCast is Error) return tryCast;
				string keyString = (tryCast as String).Value;

				// cast value to string
				tryCast = kv.Value.Cast(String.InternalType);
				if (tryCast is Error) return tryCast;
				string valueString = (tryCast as String).Value;

				// add to sb
				sb.Append($"{keyString} : {valueString}");

				// handle commas
				if (i != v.Count - 1) sb.Append(", ");
				i++;
			}
			return new String(sb.ToString());
		}

		public static Data clear(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("clear", 0, args.Count);
			(thisRef as Dict).Value.Clear();
			return thisRef;
		}
		public static Data values(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("values", 0, args.Count);
			return new List((thisRef as Dict).Value.Values.ToList());
		}
		public static Data keys(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("keys", 0, args.Count);
			return new List((thisRef as Dict).Value.Keys.ToList());
		}
		public static Data removekey(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("removekey", 1, args.Count);

			Data get = GetEvaluator(thisRef, out Evaluator evaluator);
			if (get is Error) return get;
			Operator equals = new("==");

			Dictionary<Data, Data> copy = new();
			foreach (KeyValuePair<Data, Data> kvp in (thisRef as Dict).Value) {
				Data compare = evaluator.Compare(kvp.Key, args[0], equals, thisRef.Memory);
				if (compare is Error) return compare;

				if (!(compare as Bool).Value)
					copy.Add(kvp.Key, kvp.Value);
			}

			(thisRef as Dict).Value = copy;
			return thisRef;
		}
		public static Data tolist(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tolist", 0, args.Count);

			List<Data> newList = new();

			Dictionary<Data, Data> v = (thisRef as Dict).Value;
			foreach (KeyValuePair<Data, Data> kv in v) {
				List pair = new(new() { kv.Key, kv.Value });
				newList.Add(pair);
			}

			return new List(newList);
		}
		#endregion
	}
}