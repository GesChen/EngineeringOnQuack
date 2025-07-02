using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class SaveLoadHelper {
	struct Assembly {
		public string Name;
		public List<PartInfo> Parts;
		public List<Group> Groups;
	}
	struct PVector3 {
		public float x, y, z;
		public PVector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
		public static implicit operator Vector3(PVector3 other) =>
			new(other.x, other.y, other.z);
		public static implicit operator PVector3(Vector3 other) =>
			new(other.x, other.y, other.z);
		public static implicit operator Color(PVector3 other) =>
			new(other.x, other.y, other.z);
		public static implicit operator PVector3(Color other) =>
			new(other.r, other.g, other.b);
	}
	struct PVector4 {
		public float x, y, z, w;
		public PVector4(float X, float Y, float Z, float W) { x = X; y = Y; z = Z; w = W; }
		public static implicit operator Quaternion(PVector4 other) =>
			new(other.x, other.y, other.z, other.w);
		public static implicit operator PVector4(Quaternion other) =>
			new(other.x, other.y, other.z, other.w);
	}
	struct PartInfo {
		public int basePartID;
		public int id;
		public PVector3 position;
		public PVector4 rotation;
		public PVector3 scale;

		public PVector3 color;
		public int compositionID;
	}
	struct Group {
		public List<int> PartIDs;
	}

	public void SaveCurrentBuild(string name) {
		string serializedObject = Serialize(name);

		if (Config.Saving.SaveAsText) {
			serializedObject = CompressionUtil.EncodeGzipBase64(serializedObject);

			File.WriteAllText(Path.Combine(
				Config.Saving.AssembliesLocation,
				name + Config.Saving.SaveExtension),
				serializedObject);
		} else {
			byte[] bytes = CompressionUtil.EncodeGzipBytes(serializedObject);

			File.WriteAllBytes(Path.Combine(
				Config.Saving.AssembliesLocation,
				name + Config.Saving.SaveExtension), 
				bytes);
		}
	}

	public void LoadFromFile(string filename) {
		string filePath = Path.Combine(Config.Saving.AssembliesLocation, filename + Config.Saving.SaveExtension);

		if (!File.Exists(filePath))
			throw new($"couldn't load {filename} as it doesn't exist");

		string json;
		if (Config.Saving.SaveAsText) {
			json = File.ReadAllText(filePath);
			json = CompressionUtil.DecodeGzippedBase64(json);
		} else {
			byte[] bytes = File.ReadAllBytes(filePath);
			json = CompressionUtil.DecodeGzipBytes(bytes);
		}
		
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

	public string[] GetAllAssemblies() => 
		Directory.GetFiles(Config.Saving.AssembliesLocation, "*" + Config.Saving.SaveExtension);

	public string[] GetRecentAssemblies(int count) => 
		GetAllAssemblies()
		.Select(name => (name, File.GetLastWriteTime(name).Ticks))
		.OrderBy(timepair => timepair.Ticks)
		.Select(name => name.name)
		.Take(count)
		.ToArray();

/* actual good implementation but nah
	public string[] GetRecentAssemblies(int count) {
		string[] files = GetAllAssemblies();

		List<string> TimeSortedAssemblies =
			files
			.Select(name => (name, File.GetLastWriteTime(name).Ticks))
			.OrderBy(timepair => timepair.Ticks)
			.Select(name => name.name)
			.ToList();

		return TimeSortedAssemblies.Take(count).ToArray();
	}*/
}