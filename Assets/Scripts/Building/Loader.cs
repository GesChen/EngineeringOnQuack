using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

public class Loader : MonoBehaviour
{
	public BuildingManager BuildingManager;
	
	public void LoadFromFile(string filename)
	{
		string filePath = Path.Combine(Config.FileSaveLocation, "Assemblies", $"{filename}.assembly");

		if (!File.Exists(filePath))
			throw new($"couldn't load {filename} as it doesn't exist");

		string json = File.ReadAllText(filePath);
		Assembly assembly = JsonConvert.DeserializeObject<Assembly>(json);
		assembly.parts = assembly.parts.OrderByDescending(part => part.id).ToList();

		foreach (PartInfo part in assembly.parts) 
		{
			Part newPart = BuildingManager.newPart(part.basePartName);
			newPart.transform.localPosition = new(part.position.x, part.position.y, part.position.z);
			newPart.transform.rotation = new(part.rotation.x, part.rotation.y, part.rotation.z, part.rotation.w);
			newPart.transform.localScale = new(part.scale.x, part.scale.y, part.scale.z);
		}
	}
}