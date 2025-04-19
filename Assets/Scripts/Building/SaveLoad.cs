using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class SaveLoad : MonoBehaviour
{
	public BuildingManager BuildingManager;

	public void SaveCurrentBuild(string name, bool precompute)
	{
		string serializedObject = Serializer(name, BuildingManager, precompute);

		string assembliesDir = Path.Combine(Config.tempfilesaveloc, "Assemblies");
		if (!Directory.Exists(assembliesDir))
			Directory.CreateDirectory(assembliesDir);

		File.WriteAllText(Path.Combine(assembliesDir, $"{name}.assembly"), serializedObject);
	}

	public void LoadFromFile(string filename)
	{
		string filePath = Path.Combine(Config.tempfilesaveloc, "Assemblies", $"{filename}.assembly");

		if (!File.Exists(filePath))
			throw new($"couldn't load {filename} as it doesn't exist");

		string json = File.ReadAllText(filePath);
		Assembly assembly = JsonConvert.DeserializeObject<Assembly>(json);
		assembly.parts = assembly.parts.OrderByDescending(part => part.id).ToList();

		foreach (PartInfo part in assembly.parts)
		{
			Part newPart = BuildingManager.GeneratePart(part.basePartName);
			newPart.transform.localPosition = new(part.position.x, part.position.y, part.position.z);
			newPart.transform.rotation = new(part.rotation.x, part.rotation.y, part.rotation.z, part.rotation.w);
			newPart.transform.localScale = new(part.scale.x, part.scale.y, part.scale.z);
		}
	}

	public string Serializer(string name, BuildingManager bm, bool precompute)
	{
		List<Part> parts = bm.Parts;

		List<PartInfo> infos = new();
		for (int i = 0; i < parts.Count; i++)
		{
			Part part = parts[i];
			infos.Add(new()
			{
				basePartName = part.basePart.partName,
				id = part.ID,
				position = new()
				{
					x = part.transform.localPosition.x,
					y = part.transform.localPosition.y,
					z = part.transform.localPosition.z
				},
				rotation = new()
				{
					x = part.transform.rotation.x,
					y = part.transform.rotation.y,
					z = part.transform.rotation.z,
					w = part.transform.rotation.w
				},
				scale = new()
				{
					x = part.transform.localScale.x,
					y = part.transform.localScale.y,
					z = part.transform.localScale.z
				}
			});
		}

		List<Assembler.Subassembly> precomputedSubassemblies = new();
		if (precompute)
			precomputedSubassemblies = Assembler.Instance.ComputeAssemblies(bm);
		
		List<SerializableSubassembly> serializableSubassemblies = new();
		foreach (Assembler.Subassembly subassembly in precomputedSubassemblies)
		{
			List<int> partids = new();
			foreach (Part part in subassembly.parts)
				partids.Add(part.ID);

			serializableSubassemblies.Add(new() { partIds = partids });
		}

		Assembly assembly = new()
		{
			name = name,
			parts = infos,
			didPrecomputations = precompute,
			precomputedSubassemblies = serializableSubassemblies 
		};

		return JsonConvert.SerializeObject(assembly);
	}
}
