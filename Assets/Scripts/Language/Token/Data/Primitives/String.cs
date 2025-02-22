using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class Primitive : Data {
	public partial class String : Primitive {
		// defines internal type with name and memory
		public static Type InternalType = new("String", new Dictionary<string, Data>() {
			{ "upper", new Function(upper) }
		});

		public string Value; // internal value

		public String(string value) : base(InternalType) { // default constructor
			Value = value;
		}

		public override string ToString() {
			return $"String object \"{Value}\"";
		}

		// methods
		public static Data upper(Data thisReference, List<Data> args) {
			Debug.Log($"called upper");
			if (args.Count != 0) return Errors.InvaidArgumentCount("upper", 0, args.Count);

			String _this = thisReference as String;
			Debug.Log($"this is {_this}");

			return new String(_this.Value.ToUpper());
		}
	}
}