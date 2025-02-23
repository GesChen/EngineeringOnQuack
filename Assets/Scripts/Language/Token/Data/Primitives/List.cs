using System.Collections;
using System.Collections.Generic;

using System.Text;

public abstract partial class Primitive : Data {
	public partial class List : Primitive {
		public static List Default = new();

		// defines internal type with name and memory
		public static Type InternalType = new("List", new Dictionary<string, Data>() {
			{ "tostring", new Function(tostring) }
		});

		public List<Data> Value; // internal value

		public List(List<Data> value) : base(InternalType) { // default constructor
			Value = value;
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

		// methods
		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvaidArgumentCount("tostring", 0, args.Count);

			List<Data> L = (thisRef as List).Value; // get this list

			StringBuilder builder = new();
			builder.Append("[");

			for (int i = 0; i < L.Count; i++) {
				Data d = L[i];

				// try to cast the data at i to a string
				Data casted = d.Cast(String.Default) as String;
				if (casted is Error) return casted;

				// add the new string
				builder.Append((casted as String).Value);

				if (i != L.Count - 1) builder.Append(",");

				// don't let list get too long
				if (builder.Length > LanguageConfig.MaxContainerSerializeLength)
					return new String("List object"); // lists too long will just get defaulted
			}
			builder.Append("]");

			return new String(builder.ToString());
		}
	}
}