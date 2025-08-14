using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveLoadHelper {
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

	
	static string Pathify(string name) => // turns name into full path
		Path.Combine(
		Config.Saving.AssembliesLocation,
		name + Config.Saving.SaveExtension);

	static string Depath(string path) => // turns path into name
		Path.GetFileNameWithoutExtension(path);

	public struct AssemblyInfo {
		public string Name;
		public int Parts;
	}

	public class HeaderHelper {
		public class HeaderItem {
			public System.Type Type;
			public int Bytes;
			public int Chars;

			public Func<object, byte[]> ToBytesFunction;
			public Func<object, string> ToCharsFunction;
			public Func<byte[], object> FromBytesFunction;
			public Func<string, object> FromCharsFunction;

			/// <summary>
			/// Constructor for a HeaderItem
			/// </summary>
			/// <param name="type">The type that this item represents</param>
			/// <param name="bytes">The amount of bytes that this item needs</param>
			/// <param name="chars">The amounto of chars that this item needs</param>
			/// <param name="toBytesFunction">A function that turns data into bytes</param>
			/// <param name="toCharsFunction">A function that turns data into chars</param>
			/// <param name="fromBytesFunction">A function that turns bytes back into data</param>
			/// <param name="fromCharsFunction">A function that turns chars back into data</param>
			public HeaderItem(
				System.Type type,
				int bytes,
				int chars,
				Func<object, byte[]> toBytesFunction,
				Func<object, string> toCharsFunction,
				Func<byte[], object> fromBytesFunction,
				Func<string, object> fromCharsFunction) {

				Type = type;
				Bytes = bytes;
				Chars = chars;
				ToBytesFunction = toBytesFunction;
				ToCharsFunction = toCharsFunction;
				FromBytesFunction = fromBytesFunction;
				FromCharsFunction = fromCharsFunction;
			}

			public static HeaderItem Int =
				new (
					typeof(int), 4, 11,
					data => BitConverter.GetBytes((int)data),
					data => ((int)data).ToString("D11"),
					bytes => BitConverter.ToInt32(bytes),
					chars => int.Parse(chars)
					);


		}

		public List<HeaderItem> Items;
		
		public int TotalBytes => Items.Sum(x => x.Bytes);
		public int TotalChars => Items.Sum(x => x.Chars);

		public void AddStringHeader(ref string original, params object[] data) {
			string header = "";

			for (int i = 0; i < Items.Count; i++) {
				HeaderItem item = Items[i];
				object o = data[i];

				try {
					header += item.ToCharsFunction(o);
				} catch (Exception ex) {
					throw new($"Error occured while attempting to convert item {i} to chars: {ex}");
				}
			}

			original = header + original;
		}

		public void GetStringHeader(ref string chars, out object[] data) {
			string header = chars[..TotalChars];

			data = new object[Items.Count];

			for (int i = 0; i < Items.Count; i++) {
				HeaderItem h = Items[i];

				int count = h.Chars;

				try {
					string hiString = header[..count]; // will error very easily
					object hiData = h.FromCharsFunction(hiString);
					data[i] = hiData;
				} catch {
					throw new("Failed to fetch metadata! One or more files may be incorrectly formatted.");
				}
				header = header[count..];
			}

			chars = chars[TotalChars..];
		}

		public void AddByteHeader(ref byte[] original, params object[] data) {
			List<byte> header = new(); // list of bytes feels cursed but 
									   // im too lazy to use a byte array :P

			for (int i = 0; i < Items.Count; i++) {
				HeaderItem item = Items[i];
				object o = data[i];

				try {
					header.AddRange(item.ToBytesFunction(o));
				} catch (Exception ex) {
					throw new($"Error occured while attempting to convert item {i} to bytes: {ex}");
				}
			}

			original = header.Concat(original).ToArray();
		}

		public void GetByteHeader(ref byte[] bytes, out object[] data) {
			byte[] header = bytes[..TotalBytes];

			data = new object[Items.Count];

			for (int i = 0; i < Items.Count; i++) {
				HeaderItem h = Items[i];

				int count = h.Bytes;
				byte[] hiBytes = header[..count]; // will error very easily

				object hiData = h.FromBytesFunction(hiBytes);

				data[i] = hiData;
				header = header[count..]; // does this straight up work?
			}

			bytes = bytes[TotalBytes..];
		}
	}

	public static HeaderHelper Header = new(){
		Items = new(){
			HeaderHelper.HeaderItem.Int // parts
		}
	};

	public static void SaveCurrentBuild(string name) {
		string serializedObject = Serialize(name);

		// header data
		int parts = BuildingManager.Instance.Parts.Count;

		if (Config.Saving.SaveAsText) {
			serializedObject = CompressionUtil.EncodeGzipBase64(serializedObject);

			Header.AddStringHeader(ref serializedObject,
				parts
				);

			File.WriteAllText(Pathify(name), serializedObject);
		} else {
			byte[] bytes = CompressionUtil.EncodeGzipBytes(serializedObject);

			Header.AddByteHeader(ref bytes,
				parts
				);

			File.WriteAllBytes(Pathify(name), bytes);
		}
	}

	public static void LoadFromFile(string name) {
		string path = Pathify(name);

		if (!File.Exists(path))
			throw new($"Couldn't load {name} as it doesn't exist in the assemblies folder!");

		string json;
		if (Config.Saving.SaveAsText) {
			json = File.ReadAllText(path);

			Header.GetStringHeader(ref json, out _);
			// do something with this metadata? idk. 

			json = CompressionUtil.DecodeGzippedBase64(json);
		} else {
			byte[] bytes = File.ReadAllBytes(path);

			Header.GetByteHeader(ref bytes, out _);

			json = CompressionUtil.DecodeGzipBytes(bytes);
		}

		BuildingManager.Instance.ResetParts();

		Assembly assembly = JsonConvert.DeserializeObject<Assembly>(json);

		foreach (PartInfo part in assembly.Parts) {
			Part newPart = BuildingManager.Instance.GeneratePart(part.basePartID);

			newPart.transform.localPosition = new(part.position.x, part.position.y, part.position.z);
			newPart.transform.rotation = new(part.rotation.x, part.rotation.y, part.rotation.z, part.rotation.w);
			newPart.transform.localScale = new(part.scale.x, part.scale.y, part.scale.z);

			newPart.ID = part.id;
			newPart.color = part.color;

			BuildingManager.Instance.Parts.Add(newPart);

			var composition = Compositions.All.FirstOrDefault(c => c.ID == newPart.ID);
			if (composition != null) {
				newPart.composition = composition;
			} else {
				newPart.composition = Compositions.Concrete;
				// somehow tell the player that there was an invalid composition
			}
		}

		BuildingManager.Instance.CurrentAssemblyName = name;
	}

	static string Serialize(string name) {
		Assembly assembly = new(){ Name = name };

		List<Part> workingParts = BuildingManager.Instance.Parts;
		Vector3 localOrigin = BuildingManager.Instance.mainPartsContainer.transform.position;

		List<PartInfo> parts = new();
		for (int i = 0; i < workingParts.Count; i++) {
			Part part = workingParts[i];
			parts.Add(new() {
				basePartID = part.basePart.ID,
				id = part.ID,
				position = part.transform.position - localOrigin,
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

	public static string[] GetAllAssemblyNames() => 
		Directory.GetFiles(Config.Saving.AssembliesLocation, "*" + Config.Saving.SaveExtension)
		.Select(path => Depath(path)).ToArray();

	public static string[] GetRecentAssemblyNames(int count) => 
		GetAllAssemblyNames()
		.Select(name => (name, File.GetLastWriteTime(Pathify(name)).Ticks))
		.OrderBy(timepair => timepair.Ticks)
		.Select(name => name.name)
		.Take(count)
		.ToArray();

	public static string[] GetSortedAssemblyNames() =>
		GetAllAssemblyNames()
		.Select(name => (name, File.GetLastWriteTime(Pathify(name)).Ticks))
		.OrderBy(timepair => timepair.Ticks)
		.Select(name => name.name)
		.ToArray();

	public static object[] GetAssemblyMetadata(string name) {
		string filePath = Pathify(name);
		
		if (Config.Saving.SaveAsText) {
			var text = File.ReadAllText(filePath);

			Header.GetStringHeader(ref text, out object[] data);

			return data;
		} else {
			byte[] bytes = File.ReadAllBytes(filePath);

			Header.GetByteHeader(ref bytes, out object[] data);

			return data;
		}
	}

	public static AssemblyInfo[] GetSortedAssemblyInfos() {
		string[] sortedNames = GetSortedAssemblyNames();

		AssemblyInfo[] infos = new AssemblyInfo[sortedNames.Length];
		for (int i = 0; i < sortedNames.Length; i++) {
			string name = sortedNames[i];

			object[] metadata = GetAssemblyMetadata(name);

			infos[i] = new AssemblyInfo() {
				Name = name,
				Parts = (int)metadata[0]
			};
		}

		return infos;
	}

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