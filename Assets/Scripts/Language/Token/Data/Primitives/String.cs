using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class Primitive : Data {
	public partial class String : Primitive {
		public static String Default = new("");

		// defines internal type with name and memory
		public static Type InternalType = new("String", new Dictionary<string, Data>() {
			{ "tostring", new Function(tostring) },
			{ "upper", new Function(upper) }
		});

		public string Value; // internal value

		public String(string value) : base(InternalType) { // default constructor
			Value = value;
		}

		public override string ToString() {
			return (tostring(this, new()) as String).Value;
		}

		// methods
		public static Data tostring(Data thisRef, List<Data> args) {
			if (args.Count != 0) return Errors.InvaidArgumentCount("tostring", 0, args.Count);

			return thisRef;
		}

		public static Data upper(Data thisReference, List<Data> args) {
			Debug.Log($"called upper");
			if (args.Count != 0) return Errors.InvaidArgumentCount("upper", 0, args.Count);

			String _this = thisReference as String;
			Debug.Log($"this is {_this}");

			return new String(_this.Value.ToUpper());
		}
	}
}