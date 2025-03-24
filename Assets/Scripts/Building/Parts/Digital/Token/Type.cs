using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Type {
	public string Name;
	public Memory Snapshot;

	public Type(string name, Memory snapshot) {
		Name = name;
		Snapshot = snapshot;
	}

	public Type(string name, Dictionary<string, Data> data) { // should be used for the primitives
		Name = name;
		Snapshot = new(data, new());
	}

	public override string ToString() {
		return $"Type \"{Name}\"";
	}
}