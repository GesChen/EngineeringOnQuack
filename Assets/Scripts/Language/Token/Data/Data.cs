using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Data : Token {
	public string Name;
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


	public virtual Data GetMember(string name) {
		// instance variables with same name as methods override same name in memory
		if (InstanceVariables.ContainsKey(name))
			return InstanceVariables[name];

		return Type.Snapshot.Get(name);
	}

	public override string ToString() {
		return $"Data \"{Name}\" of {Type}";
	}
}
