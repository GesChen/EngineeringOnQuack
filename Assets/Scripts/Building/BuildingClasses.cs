using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Assembly {
	public string name;
	public List<PartInfo> parts;
	public bool didPrecomputations;
	public List<SerializableSubassembly> precomputedSubassemblies;
	// to add onto
}