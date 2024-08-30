using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class Saver : MonoBehaviour
{
	public BuildingManager BuildingManager;

	public void SaveCurrentBuild(string name, bool precompute)
	{
		string serializedObject = Serializer(name, BuildingManager, precompute);

		string assembliesDir = Path.Combine(Config.FileSaveLocation, "Assemblies");
		if (!Directory.Exists(assembliesDir))
			Directory.CreateDirectory(assembliesDir);

		File.WriteAllText(Path.Combine(assembliesDir, $"{name}.assembly"), serializedObject);
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
			precomputedSubassemblies = bm.Assembler.ComputeAssemblies(bm);
		
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
