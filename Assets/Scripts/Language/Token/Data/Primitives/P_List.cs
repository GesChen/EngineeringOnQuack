using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

public abstract partial class Primitive : Data {
	public partial class List : Primitive {
		public static List Default = new();

		// defines internal type with name and memory
		public static Type InternalType = new("List", new Dictionary<string, Data>() {
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

		public List<Data> Value; // internal value

		public List(List<Data> value) : base(InternalType) { // default constructor
			Value = value;
		}
		public List(List original) : base(original) {
			Value = original.Value;
		}
		public List() : base(InternalType) { // default constructor
			Value = new();
		}

		public override string ToString() {
			Data tryInternal = tostring(this, new());
			if (tryInternal is Error)
				return "List object";

			return (tryInternal as String).Value;
		}

		public override Data Copy() {
			return new List(this);
		}

		// methods
		public static Data eq(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("eq", 1, args.Count);
			if (args[0] is not List) return new Bool(false);
			return new Bool((thisRef as List).Value.SequenceEqual((args[0] as List).Value));
		}
		public static Data lt(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("lt", 1, args.Count);
			if (args[0] is not List)
				return Errors.CannotCompare("List", args[0].Type.Name);

			List<Data> a = (thisRef as List).Value;
			List<Data> b = (args[0] as List).Value;

			Data get = Memory.GetEvaluator(thisRef, out Evaluator evaluator);
			if (get is Error) return get;

			Data tryComp;
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
		public static Data ad(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("ad", 1, args.Count);
			if (args[0] is not List L) {
				Data cast = args[0].Cast(InternalType);
				if (cast is Error) return cast;

				L = cast as List;
			}

			List<Data> newList = (thisRef as List).Value.Concat(L.Value).ToList();
			return new List(newList);
		}
		public static Data mu(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("mu", 1, args.Count);
			
			Data castToNum = args[0].Cast(Number.InternalType);
			if (castToNum is Error)
				return Errors.UnsupportedOperation("*", "List", "Non-numeric String");

			double amount = (castToNum as Number).Value;
			if (amount != Math.Floor(amount))
				return Errors.UnsupportedOperation("*", "List", "Non-whole Number");


			List<Data> repeated =
				Enumerable.Repeat((thisRef as List).Value, (int)amount)
				.SelectMany(x => x).
				ToList();

			return new List(repeated);
		}	
		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);

			List<Data> L = (thisRef as List).Value; // get this list

			StringBuilder builder = new();
			builder.Append("[");

			for (int i = 0; i < L.Count; i++) {
				Data d = L[i];

				// try to cast the data at i to a string
				Data casted = d.Cast(String.InternalType) as String;
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
		public static Data todict(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("tostring", 0, args.Count);

			List<Data> L = (thisRef as List).Value; // get this list

			Dictionary<Data, Data> newDict = new();

			// check list structure while performing cast
			foreach (Data d in L) {
				if (!(d is List SubL && SubL.Value.Count != 2))
					return Errors.BadSyntaxFor("List to Dict conversion");
				
				newDict[SubL.Value[0]] = SubL.Value[1]; // nothing can go wrong right!!!! plz?
			}

			return new Dict(newDict);
		}

		public static Data add(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("add", 1, args.Count);
			(thisRef as List).Value.Add(args[0]);
			return thisRef;
		}
		public static Data clear(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("clear", 0, args.Count);
			(thisRef as List).Value.Clear();
			return thisRef;
		}
		public static Data contains(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("contains", 1, args.Count);

			Data find = List.find(thisRef, args); // it contains it if the index is not -1
			if (find is Error) return find;

			return new Bool((find as Number).Value != -1);
		}
		public static Data count(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("count", 1, args.Count);

			Data get = Memory.GetEvaluator(thisRef, out Evaluator evaluator);
			if (get is Error) return get;

			int amount = 0;
			Operator equals = new ("==");
			foreach (Data d in (thisRef as List).Value) {
				Data compare = evaluator.Compare(d, args[0], equals, thisRef.Memory);
				if (compare is Error) return compare;

				if ((compare as Bool).Value)
					amount++;
			}

			return new Number(amount);
		}
		public static Data extend(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("extend", 1, args.Count);

			if (args[0] is not List extension) // make sure extension is a list so addrange works
				extension = new(new List<Data>() { args[0] });

			(thisRef as List).Value.AddRange(extension.Value);
			return thisRef;
		}
		public static Data find(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("find", 1, args.Count);

			Data get = Memory.GetEvaluator(thisRef, out Evaluator evaluator);
			if (get is Error) return get;

			Operator equals = new("==");
			List<Data> list = (thisRef as List).Value;
			for (int i = 0; i < list.Count; i++) {
				Data compare = evaluator.Compare(list[i], args[0], equals, thisRef.Memory);
				if (compare is Error) return compare;

				if ((compare as Bool).Value)
					return new Number(i);
			}
			return new Number(-1);
		}
		public static Data inject(Data thisRef, List<Data> args) {
			if (args.Count != 2) return Errors.InvalidArgumentCount("inject", 2, args.Count);
			if (args[0] is not Number index) return Errors.InvalidArgumentType("inject", 0, "Number", args[0].Type.Name);
			if (index.Value != Math.Round(index.Value)) return Errors.InvalidArgumentType("inject", 0, "whole number", "decimal");

			if (args[1] is not List extension)
				extension = new(new List<Data>() { args[1] });

			(thisRef as List).Value.InsertRange((int)index.Value, extension.Value); // safety cast even tho already checked
			return thisRef;
		}
		public static Data insert(Data thisRef, List<Data> args) {
			if (args.Count != 2) return Errors.InvalidArgumentCount("insert", 2, args.Count);
			if (args[0] is not Number index) return Errors.InvalidArgumentType("insert", 0, "Number", args[0].Type.Name);
			if (index.Value != Math.Round(index.Value)) return Errors.InvalidArgumentType("insert", 0, "whole number", "decimal");

			(thisRef as List).Value.Insert((int)index.Value, args[1]); // safety cast even tho already checked
			return thisRef;
		}
		public static Data remove(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("remove", 1, args.Count);
			Data index = find(thisRef, args);
			if (index is Error) return index; // idk how this would happen lmao

			if ((index as Number).Value == -1) // return original if not found
				return thisRef;

			(thisRef as List).Value.RemoveAt((int)(index as Number).Value);
			return thisRef;
		}
		public static Data removeall(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("removeall", 1, args.Count);

			Data get = Memory.GetEvaluator(thisRef, out Evaluator evaluator);
			if (get is Error) return get;

			Data[] items = (thisRef as List).Value.ToArray();
			Data item = args[0];
			Operator equals = new("==");

			// below code ripped straight from removeall and replaced with own functions.
			// i got no fuckin clue how this works. :(

			int size = items.Length;
			int i = 0;
			while (i < size) {
				Data compare = evaluator.Compare(items[i], item, equals, thisRef.Memory);
				if (compare is Error) return compare;
				if ((compare as Bool).Value)
					break;
				i++;
			}

			if (i >= size) return new Bool(false); // no matches

			int j = i + 1;
			while (j < size) {
				while (j < size) {
					Data compare = evaluator.Compare(items[j], item, equals, thisRef.Memory);
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
		public static Data removeindex(Data thisRef, List<Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("removeindex", 1, args.Count);
			if (args[0] is not Number index) return Errors.InvalidArgumentType("removeindex", 0, "Number", args[0].Type.Name);
			if (index.Value != Math.Round(index.Value)) return Errors.InvalidArgumentType("removeindex", 0, "whole number", "decimal");

			(thisRef as List).Value.RemoveAt((int)index.Value); // safety cast even tho already checked
			return thisRef;
		}
		public static Data reverse(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvalidArgumentCount("reverse", 0, args.Count);

			(thisRef as List).Value.Reverse();
			return thisRef;
		}
		/*public static Data sort(Data thisRef, List<Data> args) {
			if (args.Count != ) return Errors.InvalidArgumentCount("sort", , args.Count);

		}*/
	}
}