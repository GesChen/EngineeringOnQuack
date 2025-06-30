using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class SaveLoadHelper {
	struct Assembly {
		public string Name;
		public List<PartInfo> Parts;
		public List<Group> Groups;
	}

	struct PartInfo {
		public int basePartID;
		public int id;
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;

		public Color color;
		public int compositionID;
	}
	struct Group {
		public List<int> PartIDs;
	}

	public void SaveCurrentBuild(string name) {
		string serializedObject = Serialize(name);

		File.WriteAllText(Path.Combine(
			Config.Locations.AssembliesLocation, 
			$"{name}.assembly"), serializedObject);
	}

	public void LoadFromFile(string filename) {
		string filePath = Path.Combine(Config.Locations.AssembliesLocation, $"{filename}.assembly");

		if (!File.Exists(filePath))
			throw new($"couldn't load {filename} as it doesn't exist");

		string json = File.ReadAllText(filePath);
		Assembly assembly = JsonConvert.DeserializeObject<Assembly>(json);

		foreach (PartInfo part in assembly.Parts) {
			Part newPart = BuildingManager.Instance.GeneratePart(part.basePartID);

			newPart.transform.localPosition = new(part.position.x, part.position.y, part.position.z);
			newPart.transform.rotation = new(part.rotation.x, part.rotation.y, part.rotation.z, part.rotation.w);
			newPart.transform.localScale = new(part.scale.x, part.scale.y, part.scale.z);

			newPart.ID = part.id;
			newPart.color = part.color;

			var composition = Compositions.All.FirstOrDefault(c => c.ID == newPart.ID);
			if (composition != null) {
				newPart.composition = composition;
			} else {
				newPart.composition = Compositions.Concrete;
				// somehow tell the player that there was an invalid composition
			}
		}
	}

	string Serialize(string name) {
		Assembly assembly = new(){ Name = name };

		List<Part> baseParts = BuildingManager.Instance.Parts;

		List<PartInfo> parts = new();
		for (int i = 0; i < baseParts.Count; i++) {
			Part part = baseParts[i];
			parts.Add(new() {
				basePartID = part.basePart.ID,
				id = part.ID,
				position = part.transform.localPosition,
				rotation = part.transform.rotation,
				scale = part.transform.localScale,

				color = part.color,
				compositionID = part.composition.ID
			});
		}

		var baseGroups = GroupManager.Instance.Groups;

		List<Group> groups = new();
		foreach (var group in baseGroups) {
			groups.Add(new() {
				PartIDs = group.Parts.Select(p => p.ID).ToList(),
			});
		}

		assembly.Parts = parts;
		assembly.Groups = groups;

		return JsonConvert.SerializeObject(assembly);
	}
}
