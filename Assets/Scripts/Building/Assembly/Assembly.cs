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

	// S prefix for serializable
	// they need to be converted this way because newtonsoft json
	// just fucking hates me i guess

	public Construct ConvertToConstruct() => new() {
		Name = Name,
		Parts = Parts.Select(p => ConvertToCPart(p)).ToList(),
		Groups = Groups.Select(group => (Construct.Group)group).ToList(),
		Clipboard = Config.Building.Saving.SaveClipboard ? Clipboard : null,
		Outputs = Outputs
	};

	public static string Serialize(Assembly assembly) => 
		JsonConvert.SerializeObject(assembly.ConvertToConstruct(), SaveLoadHelper.Settings);
	public static Assembly Reconstruct(Construct construct) {
		var reconstructed = new Assembly {
			Name = construct.Name
		};

		foreach (Construct.Part part in construct.Parts) {
			ReconstructPart(reconstructed, part);
		}

		var partIDlookup = reconstructed.Parts.ToDictionary(p => p.ID);

		// reconstruct groups
		foreach (var group in construct.Groups) {
			// make a new partgroup by finding all the new parts with the ids
			var parts = group.PartIDs.Select(i => partIDlookup[i]).ToList();
			
			reconstructed.Groups.Add(new(parts));
		}

		reconstructed.Clipboard = construct.Clipboard ?? new(); // hope it serializes well

		// reconstruct outputs
		reconstructed.Outputs = construct.Outputs;

		return reconstructed;
	}

	internal static Construct.Part ConvertToCPart(Part other) {
		Vector3 localOrigin = GameManager.Instance.MainPartsContainer.transform.position;
		Construct.Part cpa = new() {
			basePartID = other.basePart.ID,
			id = other.ID,
			position = other.transform.position - localOrigin,
			rotation = other.transform.rotation,
			scale = other.transform.localScale,

			color = other.color,
			compositionID = other.composition.ID
		};

		if (other.IsNonStaticPart(out var nsp)) {
			nsp.FinalizeCPartConversion(ref cpa);
		}

		return cpa;
	}

	private static void ReconstructPart(Assembly reconstructed, Construct.Part part) {
		Part newPart = BuildingManager.Instance.MakeNewPart(part.basePartID, false);

		newPart.transform.localPosition = new(part.position.x, part.position.y, part.position.z);
		newPart.transform.rotation = new(part.rotation.x, part.rotation.y, part.rotation.z, part.rotation.w);
		newPart.transform.localScale = new(part.scale.x, part.scale.y, part.scale.z);

		newPart.ID = part.id;
		newPart.color = part.color;

		reconstructed.Parts.Add(newPart);

		var composition = Compositions.All.FirstOrDefault(c => c.ID == newPart.ID);
		if (composition != null) {
			newPart.composition = composition;
		} else {
			newPart.composition = Compositions.Concrete;
			// somehow tell the player that there was an invalid composition
		}

		if (newPart.IsNonStaticPart(out var nsp)) {
			nsp.FinalizeCPartReconstruction(part, newPart, reconstructed);
		}
	}
}