//#define DEBUGMODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public static class AssemblySaveLoadHelper {
	public static SaveLoadHelper<Construct> SaveLoadHelper = new(Config.SaveLoad.BuildingConfig);

	public struct AssemblyInfo {
		public string Name;
		public int Parts;
	}

	public static void SaveCurrentBuild() {
		var assem = BuildingManager.Instance.Assembly;
		var construct = assem.ConvertToConstruct();

		// header data
		string name = assem.Name;
		int parts = assem.Parts.Count;

		SaveLoadHelper.Save(construct, name, new object[] { parts });
	}

	public static void LoadFromFile(string name) {
		var assembly = SaveLoadHelper.Load(name);

		BuildingManager.Instance.ResetPartsAndGroups();

		Load(assembly);
	}

	public static void Load(Construct construct) {
		var reconstructed = Reconstruct(construct);

		BuildingManager.Instance.Assembly = reconstructed;
	}

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

	private static void ReconstructPart(Assembly reconstructed, Construct.Part part) {
		Part newPart = BuildingManager.Instance.MakeNewPart(part.basePartID, false, false);

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

	public static string[] GetAllAssemblyNames() => 
		Directory.GetFiles(Config.SaveLoad.BuildingConfig.SaveLocation, "*" + Config.SaveLoad.BuildingConfig.SaveExtension)
		.Select(path => Path.GetFileNameWithoutExtension(path)).ToArray();

	public static string[] GetRecentAssemblyNames(int count) => 
		GetAllAssemblyNames()
		.Select(name => (name, File.GetLastWriteTime(SaveLoadHelper.Pathify(name)).Ticks))
		.OrderBy(timepair => timepair.Ticks)
		.Select(name => name.name)
		.Take(count)
		.ToArray();

	public static string[] GetSortedAssemblyNames() =>
		GetAllAssemblyNames()
		.Select(name => (name, File.GetLastWriteTime(SaveLoadHelper.Pathify(name)).Ticks))
		.OrderBy(timepair => timepair.Ticks)
		.Select(name => name.name)
		.ToArray();

	public static AssemblyInfo[] GetSortedAssemblyInfos() {
		string[] sortedNames = GetSortedAssemblyNames();

		AssemblyInfo[] infos = new AssemblyInfo[sortedNames.Length];
		for (int i = 0; i < sortedNames.Length; i++) {
			string name = sortedNames[i];

			object[] metadata = SaveLoadHelper.GetMetadata(name);

			infos[i] = new AssemblyInfo() {
				Name = name,
				Parts = (int)metadata[0]
			};
		}

		return infos;
	}
}