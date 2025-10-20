using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AllParts {
	// this looks ridiculous only for the reason that i removed the prefixes
	// from the different components' names and now we're left with
	// fuckin "cube cube cube cube" lmao
	public static List<BasePart> BaseParts = new() {
		new(0,	"cube",		"Cube",		"Cube",		"Cube"),
		new(1,	"sphere",	"Sphere",	"Sphere",	"Sphere"),
		new(2,	"axle",		"Axle",		"Axle",		"Axle"),
		new(3,	"cpu",		"CPU",		"CPU",		"CPU"),
		new(4,	"cable",	"Cable",	"Cable",	"Cable"),
		new(5,	"cc",		"Sphere",	"Sphere",	"CableConnection")
	};
}