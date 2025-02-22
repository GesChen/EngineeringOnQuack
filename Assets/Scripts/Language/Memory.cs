using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Memory : MonoBehaviour {
	public Dictionary<string, Data> Data;
	public Dictionary<string, Type> Types;
	public Section Script;

	public Memory(Dictionary<string, Data> data, Dictionary<string, Type> types, Section script) {
		Data = data;
		Types = types;
		Script = script;
	}
	public Memory() {
		Data = new();
		Types = new();
		Script = null;
	}

	public Memory Copy() {
		return new(
			new Dictionary<string, Data>(Data),
			new Dictionary<string, Type>(Types),
			new(Script.Lines)
			);
	}

	public bool Exists(string name) {
		return Data.ContainsKey(name);
	}

	public Data Get(string name) {
		if (Data.ContainsKey(name)) return Data[name];
		return Errors.UnknownVariable(name);
	}

	//public Data Get(List<Token> location) {
		
	//}

	public void Set(string name, Data data) {
		Data[name] = data;

		if (Types.ContainsKey(name))
			Types.Remove(name);
	}

	public void Set(List<Token> location, Data data) {

	}

	public void NewType(string name, Type type) {
		Types[name] = type;

		if (Data.ContainsKey(name))
			Data.Remove(name);
	}

	public override string ToString() {
		return $"Memory object";
	}
}
