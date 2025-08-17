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
	public string Name = "New Assembly";
	public List<Part> Parts = new();
	public List<PartGroup> Groups = new();
	public BuildingClipboard Clipboard = new();
	public List<Output> Outputs = new();

	// putting this code in here violates SRP btw dude

	// S prefix for serializable
	// they need to be converted this way because newtonsoft json
	// just fucking hates me i guess
	public struct SVector3 {
		public float x, y, z;
		public SVector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
		public static implicit operator Vector3(SVector3 other) =>
			new(other.x, other.y, other.z);
		public static implicit operator SVector3(Vector3 other) =>
			new(other.x, other.y, other.z);
		public static implicit operator Color(SVector3 other) =>
			new(other.x, other.y, other.z);
		public static implicit operator SVector3(Color other) =>
			new(other.r, other.g, other.b);
	}
	public struct SVector4 {
		public float x, y, z, w;
		public SVector4(float X, float Y, float Z, float W) { x = X; y = Y; z = Z; w = W; }
		public static implicit operator Quaternion(SVector4 other) =>
			new(other.x, other.y, other.z, other.w);
		public static implicit operator SVector4(Quaternion other) =>
			new(other.x, other.y, other.z, other.w);
	}
	public struct SPart {
		public int basePartID;
		public int id;
		public SVector3 position;
		public SVector4 rotation;
		public SVector3 scale;

		public SVector3 color;
		public int compositionID;

		public static explicit operator SPart(Part other) {
			Vector3 localOrigin = BuildingManager.Instance.MainPartsContainer.transform.position;
			return new() {
				basePartID = other.basePart.ID,
				id = other.ID,
				position = other.transform.position - localOrigin,
				rotation = other.transform.rotation,
				scale = other.transform.localScale,

				color = other.color,
				compositionID = other.composition.ID
			};
		}


	}
	public struct SGroup {
		public List<int> PartIDs;

		public static explicit operator SGroup(PartGroup other) => new() {
			PartIDs = other.Parts.Select(p => p.ID).ToList(),
		};
	}

	public struct SAssembly {
		public string Name;
		public List<SPart> Parts;
		public List<SGroup> Groups;
		public BuildingClipboard Clipboard; // should serialize just fine

		public static explicit operator SAssembly(Assembly other) => new() {
			Name = other.Name,
			Parts = other.Parts.Select(p => (SPart)p).ToList(),
			Groups = other.Groups.Select(group => (SGroup)group).ToList(),
			Clipboard = Config.Building.Saving.SaveClipboard ? other.Clipboard : null
		};
	}

	public static string Serialize(Assembly assembly) => JsonConvert.SerializeObject((SAssembly)assembly);
	public static Assembly Reconstruct(SAssembly assembly) {
		var reconstructed = new Assembly {
			Name = assembly.Name
		};

		foreach (SPart part in assembly.Parts) {
			Part newPart = BuildingManager.Instance.GeneratePart(part.basePartID);

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
		}

		var partIDlookup = reconstructed.Parts.ToDictionary(p => p.ID);

		// reconstruct groups
		foreach (var group in assembly.Groups) {
			// make a new partgroup by finding all the new parts with the ids
			var parts = group.PartIDs.Select(i => partIDlookup[i]).ToList();
			
			reconstructed.Groups.Add(new(parts));
		}

		reconstructed.Clipboard = assembly.Clipboard ?? new(); // hope it serializes well

		return reconstructed;
	}


}