using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AllParts {
	// this looks ridiculous only for the reason that i removed the prefixes
	// from the different components' names and now we're left with
	// fuckin "cube cube cube cube" lmao
	public static List<BasePart> BaseParts = new() {
		new("cube", "Cube", "Cube", "Cube"),
		new("sphere", "Sphere", "Sphere", "Sphere"),
		new("axle", "Axle", "Axle", "Axle")
	};
}