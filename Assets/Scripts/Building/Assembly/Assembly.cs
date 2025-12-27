using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using System.Linq;

/// <summary>
/// New shiny improved? centralized class to keep track of the 
/// current assembly and centralize parts, groups
/// serializatoin/deserialzation and other stuff
/// </summary>
public class Assembly {
	public string Name = "";
	public List<Part> Parts = new();
	public List<PartGroup> Groups = new();
	public BuildingClipboard Clipboard = new(); // might move to main
	public List<string> Outputs = new();

	// putting this code in here violates SRP btw dude
	// somehow put the S classes into their own files

	// we are REALLY gonna need to refactor this
	// fixt - 12-1-25

	// S prefix for serializable
	// they need to be converted this way because newtonsoft json
	// just fucking hates me i guess

	public Construct ConvertToConstruct() => new() {
		Name = Name,
		Parts = Parts.Select(p => (Construct.Part)p).ToList(),
		Groups = Groups.Select(group => (Construct.Group)group).ToList(),
		Clipboard = Config.SaveLoad.BuildingConfig_SaveClipboard ? Clipboard : null,
		Outputs = Outputs
	};
}