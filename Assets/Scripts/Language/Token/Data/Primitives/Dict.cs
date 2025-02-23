using System.Collections;
using System.Collections.Generic;
using System.Text;

public abstract partial class Primitive : Data {
	public partial class Dict : Primitive {
		public static Dict Default = new();

		// defines internal type with name and memory
		public static Type InternalType = new("Dict", new Dictionary<string, Data>() {
			{ "tostring ",  new Function(tostring) },
			{ "tolist",     new Function(tolist) }
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

		// methods
		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvaidArgumentCount("tostring", 0, args.Count);

			// init
			Dictionary<Data, Data> v = (thisRef as Dict).Value;
			StringBuilder sb = new();
			sb.Append("{");

			int i = 0;
			foreach (KeyValuePair<Data, Data> kv in v) {
				// cast key to string
				Data tryCast = kv.Key.Cast(String.Default);
				if (tryCast is Error) return tryCast;
				string keyString = (tryCast as String).Value;

				// cast value to string
				tryCast = kv.Value.Cast(String.Default);
				if (tryCast is Error) return tryCast;
				string valueString = (tryCast as String).Value;

				// add to sb
				sb.Append($"{keyString} : {valueString}");

				// handle commas
				if (i != v.Count - 1)
					sb.Append(", ");
				i++;
			}
			return new String(sb.ToString());
		}


		public static Data tolist(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvaidArgumentCount("tolist", 0, args.Count);

			List<Data> newList = new();

			Dictionary<Data, Data> v = (thisRef as Dict).Value;
			foreach (KeyValuePair<Data, Data> kv in v) {
				List pair = new(new() { kv.Key, kv.Value });
				newList.Add(pair);
			}

			return new List(newList);
		}
	}
}