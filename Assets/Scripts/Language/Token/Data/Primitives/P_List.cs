using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

public abstract partial class Primitive : T_Data {
	public partial class List : Primitive {
		public static List Default = new();

		// defines internal type with name and memory
		public static Type InternalType = new("List", new Dictionary<string, T_Data>() {
			{ "eq"			, new Function("eq"				, eq)			},
			{ "lt"			, new Function("lt"				, lt)			},
			{ "ad"			, new Function("ad"				, ad)			},
			{ "mu"			, new Function("mu"				, mu)			},
			{ "tostring"	, new Function("tostring"		, tostring)		},
			{ "todict"		, new Function("todict"			, todict)		},
			{ "add"			, new Function("add"			, add)			},
			{ "clear"		, new Function("clear"			, clear)		},
			{ "contains"	, new Function("contains"		, contains)		},
			{ "count"		, new Function("count"			, count)		},
			{ "extend"		, new Function("extend"			, extend)		},
			{ "find"		, new Function("find"			, find)			},
			{ "inject"		, new Function("inject"			, inject)		},
			{ "insert"		, new Function("insert"			, insert)		},
			{ "remove"		, new Function("remove"			, remove)		},
			{ "removeall"	, new Function("removeall"		, removeall)	},
			{ "removeindex"	, new Function("removeindex"	, removeindex)	},
			{ "reverse"		, new Function("reverse"		, reverse)		}
		});

		public List<T_Data> Value; // internal value

		public List(List<T_Data> value) : base(InternalType) { // default constructor
			Value = value;
		}
		public List(List original) : base(original) {
			Value = original.Value;
		}
		public List() : base(InternalType) { // default constructor
			Value = new();
		}

		public override string ToString() {
			T_Data tryInternal = tostring(this, new());
			if (tryInternal is Error)
				return "List object";

			return (tryInternal as String).Value;
		}

		public override T_Data Copy() {
			return new List(this);
		}

		// methods
		public static T_Data eq(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("eq", 1, args.Count);
			if (args[0] is not List) return new Bool(false);
			return new Bool((thisRef as List).Value.SequenceEqual((args[0] as List).Value));
		}
		public static T_Data lt(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("lt", 1, args.Count);
			if (args[0] is not List)
				return Errors.CannotCompare("List", args[0].Type.Name);

			List<T_Data> a = (thisRef as List).Value;
			List<T_Data> b = (args[0] as List).Value;

			T_Data get = Memory.GetEvaluator(thisRef, out Evaluator evaluator);
			if (get is Error) return get;

			T_Data tryComp;
			int minLen = Math.Min(a.Count, b.Count);
			for (int i = 0; i < minLen; i++) {
				// a[i] < b[i]?
				tryComp = evaluator.Compare(a[i], b[i], new("<"), thisRef.Memory);
				if (tryComp is Error) return tryComp;
				if ((tryComp as Bool).Value) return new Bool(true); // ai < bi

				// b[i] < a[i]?
				tryComp = evaluator.Compare(b[i], a[i], new("<"), thisRef.Memory);
				if (tryComp is Error) return tryComp;
				if ((tryComp as Bool).Value) return new Bool(false); // bi < ai
			}

			return new Bool(a.Count < b.Count);
		}
		public static T_Data ad(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("ad", 1, args.Count);
			if (args[0] is not List L) {
				T_Data cast = args[0].Cast(InternalType);
				if (cast is Error) return cast;

				L = cast as List;
			}

			List<T_Data> newList = (thisRef as List).Value.Concat(L.Value).ToList();
			return new List(newList);
		}
		public static T_Data mu(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("mu", 1, args.Count);
			
			T_Data castToNum = args[0].Cast(Number.InternalType);
			if (castToNum is Error)
				return Errors.UnsupportedOperation("*", "List", "Non-numeric String");

			double amount = (castToNum as Number).Value;
			if (amount != Math.Floor(amount))
				return Errors.UnsupportedOperation("*", "List", "Non-whole Number");


			List<T_Data> repeated =
				Enumerable.Repeat((thisRef as List).Value, (int)amount)
				.SelectMany(x => x).
				ToList();

			return new List(repeated);
		}	
		public static T_Data tostring(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);

			List<T_Data> L = (thisRef as List).Value; // get this list

			StringBuilder builder = new();
			builder.Append("[");

			for (int i = 0; i < L.Count; i++) {
				T_Data d = L[i];

				// try to cast the data at i to a string
				T_Data casted = d.Cast(String.InternalType) as String;
				if (casted is Error) return casted;
				string castedString = (casted as String).Value;
				if (d is String)
					castedString = $"\"{castedString}\"";

				// add the new string
				builder.Append(castedString);

				if (i != L.Count - 1) builder.Append(", ");

				// don't let list get too long
				if (builder.Length > Config.Language.MaxContainerSerializeLength)
					return new String("List object"); // lists too long will just get defaulted
			}
			builder.Append("]");

			return new String(builder.ToString());
		}
		public static T_Data todict(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);

			List<T_Data> L = (thisRef as List).Value; // get this list

			Dictionary<T_Data, T_Data> newDict = new();

			// check list structure while performing cast
			foreach (T_Data d in L) {
				if (!(d is List SubL && SubL.Value.Count != 2))
					return Errors.BadSyntaxFor("List to Dict conversion");
				
				newDict[SubL.Value[0]] = SubL.Value[1]; // nothing can go wrong right!!!! plz?
			}

			return new Dict(newDict);
		}

		public static T_Data add(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("add", 1, args.Count);
			(thisRef as List).Value.Add(args[0]);
			return thisRef;
		}
		public static T_Data clear(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("clear", 0, args.Count);
			(thisRef as List).Value.Clear();
			return thisRef;
		}
		public static T_Data contains(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("contains", 1, args.Count);

			T_Data find = List.find(thisRef, args); // it contains it if the index is not -1
			if (find is Error) return find;

			return new Bool((find as Number).Value != -1);
		}
		public static T_Data count(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("count", 1, args.Count);

			T_Data get = Memory.GetEvaluator(thisRef, out Evaluator evaluator);
			if (get is Error) return get;

			int amount = 0;
			T_Operator equals = new ("==");
			foreach (T_Data d in (thisRef as List).Value) {
				T_Data compare = evaluator.Compare(d, args[0], equals, thisRef.Memory);
				if (compare is Error) return compare;

				if ((compare as Bool).Value)
					amount++;
			}

			return new Number(amount);
		}
		public static T_Data extend(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("extend", 1, args.Count);

			if (args[0] is not List extension) // make sure extension is a list so addrange works
				extension = new(new List<T_Data>() { args[0] });

			(thisRef as List).Value.AddRange(extension.Value);
			return thisRef;
		}
		public static T_Data find(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("find", 1, args.Count);

			T_Data get = Memory.GetEvaluator(thisRef, out Evaluator evaluator);
			if (get is Error) return get;

			T_Operator equals = new("==");
			List<T_Data> list = (thisRef as List).Value;
			for (int i = 0; i < list.Count; i++) {
				T_Data compare = evaluator.Compare(list[i], args[0], equals, thisRef.Memory);
				if (compare is Error) return compare;

				if ((compare as Bool).Value)
					return new Number(i);
			}
			return new Number(-1);
		}
		public static T_Data inject(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 2) return Errors.InvalidArgumentCount("inject", 2, args.Count);
			if (args[0] is not Number index) return Errors.InvalidArgumentType("inject", 0, "Number", args[0].Type.Name);
			if (index.Value != Math.Round(index.Value)) return Errors.InvalidArgumentType("inject", 0, "whole number", "decimal");

			if (args[1] is not List extension)
				extension = new(new List<T_Data>() { args[1] });

			(thisRef as List).Value.InsertRange((int)index.Value, extension.Value); // safety cast even tho already checked
			return thisRef;
		}
		public static T_Data insert(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 2) return Errors.InvalidArgumentCount("insert", 2, args.Count);
			if (args[0] is not Number index) return Errors.InvalidArgumentType("insert", 0, "Number", args[0].Type.Name);
			if (index.Value != Math.Round(index.Value)) return Errors.InvalidArgumentType("insert", 0, "whole number", "decimal");

			(thisRef as List).Value.Insert((int)index.Value, args[1]); // safety cast even tho already checked
			return thisRef;
		}
		public static T_Data remove(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("remove", 1, args.Count);
			T_Data index = find(thisRef, args);
			if (index is Error) return index; // idk how this would happen lmao

			if ((index as Number).Value == -1) // return original if not found
				return thisRef;

			(thisRef as List).Value.RemoveAt((int)(index as Number).Value);
			return thisRef;
		}
		public static T_Data removeall(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("removeall", 1, args.Count);

			T_Data get = Memory.GetEvaluator(thisRef, out Evaluator evaluator);
			if (get is Error) return get;

			T_Data[] items = (thisRef as List).Value.ToArray();
			T_Data item = args[0];
			T_Operator equals = new("==");

			// below code ripped straight from removeall and replaced with own functions.
			// i got no fuckin clue how this works. :(

			int size = items.Length;
			int i = 0;
			while (i < size) {
				T_Data compare = evaluator.Compare(items[i], item, equals, thisRef.Memory);
				if (compare is Error) return compare;
				if ((compare as Bool).Value)
					break;
				i++;
			}

			if (i >= size) return new Bool(false); // no matches

			int j = i + 1;
			while (j < size) {
				while (j < size) {
					T_Data compare = evaluator.Compare(items[j], item, equals, thisRef.Memory);
					if (compare is Error) return compare;
					if ((compare as Bool).Value)
						break;
					
					j++;
				}

				if (j < size)
					items[i++] = items[j++];
			}

			Array.Clear(items, i, size - i);

			(thisRef as List).Value = items.ToList();
			return thisRef;
		}
		public static T_Data removeindex(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("removeindex", 1, args.Count);
			if (args[0] is not Number index) return Errors.InvalidArgumentType("removeindex", 0, "Number", args[0].Type.Name);
			if (index.Value != Math.Round(index.Value)) return Errors.InvalidArgumentType("removeindex", 0, "whole number", "decimal");

			(thisRef as List).Value.RemoveAt((int)index.Value); // safety cast even tho already checked
			return thisRef;
		}
		public static T_Data reverse(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("reverse", 0, args.Count);

			(thisRef as List).Value.Reverse();
			return thisRef;
		}
		/*public static Data sort(Data thisRef, List<Data> args) {
			if (args.Count != ) return Errors.InvalidArgumentCount("sort", , args.Count);

		}*/
	}
}