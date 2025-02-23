using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Primitive;

public class Data : Token {
#pragma warning disable CS0108
	public string Name;
#pragma warning restore CS0108
	public Type Type;
	public Dictionary<string, Data> InstanceVariables;

	// for internal use only, convenient wrapper method to quickly make primitives for testing
	// should probably not use this in the actual language
	// for those, just explicitly use the proper primitive type
	public static Data DynamicData(dynamic value) {
		System.Type valueType = value.GetType();

		if (valueType == typeof(double))
			return new Primitive.Number(value);
		else if (valueType == typeof(string))
			return new Primitive.String(value);
		else if (valueType == typeof(bool))
			return new Primitive.Bool(value);
		else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(List<>))
			return new Primitive.List(value);
		else if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			return new Primitive.Dict(value);
		else
			throw new("Bad value in dynamic data call"); // probably replace this later with return error if needed but fear error will propogate and be hard to track down
	}

	// constructors
	public Data(string name, Type type) {
		Name = name;
		Type = type;
		InstanceVariables = new();
	}
	public Data(Type type) {
		Type = type;
		InstanceVariables = new();
	}

	// test constructor DO NOT USE 
	public Data() {
	}

	// statics
	public static Data Success = new Bool(true);

	// methods

	public virtual Data GetMember(string name) {
		// instance variables with same name as methods override same name in memory
		if (InstanceVariables.ContainsKey(name))
			return InstanceVariables[name];

		return Type.Snapshot.Get(name);
	}

	#region Casting
	// self cast
	public Data Cast(Data toType) {
		return CastFromTo(this, toType);
	}

	// cast any two types
	public static Data CastFromTo(Data fromValue, Data toType) {
		string FTN = fromValue.Type.Name;
		string TTN = toType.Type.Name;

		// have to be primitives, no cast (from or to function) or (from dict)
		if (!Primitive.TypeNames.Contains(FTN) || !Primitive.TypeNames.Contains(TTN))
			return Errors.InvalidCast(FTN, TTN);
		if (FTN == "Function" || TTN == "Dict" || TTN == "Function")
			return Errors.InvalidCast(FTN, TTN);

		switch (FTN) {
			case "Number": return NumberCast(fromValue as Number, TTN);
			case "String": return StringCast(fromValue as String, TTN);
			case "Bool": return BoolCast(fromValue as Bool, TTN);
			case "List": return ListCast(fromValue as List, TTN);
			case "Dict": return DictCast(fromValue as Dict, TTN);
			default: return Errors.InvalidCast(FTN, TTN);
		}
	}

	private static Data NumberCast(Number value, string to) {
		double v = value.Value;
		switch (to) {
			case "String": return Number.tostring(value, new());
			case "Bool": return new Bool(v != 0);
			case "List": return new List(new() { value });
		}
		return Errors.InvalidCast("Number", to);
	}
	private static Data StringCast(String value, string to) {
		string v = value.Value;
		switch (to) {
			case "Number":
				if (double.TryParse(v, out double val)) return new Number(val);
				return Errors.CannotParseValueAs("String", "Number");
			case "Bool": return new Bool(v != "");
			case "List": return new List(new() { value });
		}
		return Errors.InvalidCast("String", to);
	}
	private static Data BoolCast(Bool value, string to) {
		bool v = value.Value;
		switch (to) {
			case "Number": return new Number(v ? 1 : 0);
			case "String": return Bool.tostring(value, new());
			case "List": return new List(new() { value });
		}
		return Errors.InvalidCast("Bool", to);
	}
	private static Data ListCast(List value, string to) {
		List<Data> v = value.Value;
		switch (to) {
			case "Number": return new Number(v.Count == 0 ? 1 : 0);
			case "String": return List.tostring(value, new());
			case "Bool": return new Bool(v.Count != 0);
		}
		return Errors.InvalidCast("List", to);
	}
	private static Data DictCast(Dict value, string to) {
		Dictionary<Data, Data> v = value.Value;
		switch (to) {
			case "Number": return new Number(v.Count == 0 ? 1 : 0);
			case "String": return Dict.tostring(value, new());
			case "Bool": return new Bool(v.Count != 0);
			case "List": return Dict.tolist(value, new());
		}
		return Errors.InvalidCast("Dict", to);
	}
	#endregion

	public override string ToString() {
		return $"{Type} object \"{Name}\"";
	}
}
