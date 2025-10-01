using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveLoadHelper {
	public static readonly JsonSerializerSettings Settings = new() {
		TypeNameHandling = TypeNameHandling.Auto
	};

	static string Pathify(string name) => // turns name into full path
		Path.Combine(
		Config.Building.Saving.AssembliesLocation,
		name + Config.Building.Saving.SaveExtension);

	static string Depath(string path) => // turns path into name
		Path.GetFileNameWithoutExtension(path);

	public struct AssemblyInfo {
		public string Name;
		public int Parts;
	}

	// collapse this
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

		public HeaderHelper(params HeaderItem[] items) {
			Items = items;
		}

		public HeaderItem[] Items;
		
		public int TotalBytes => Items.Sum(x => x.Bytes);
		public int TotalChars => Items.Sum(x => x.Chars);

		public void AddStringHeader(ref string original, params object[] data) {
			if (data.Length != Items.Length)
				throw new ArgumentException("Data params must be the same length as items definition");
			
			string header = "";

			for (int i = 0; i < Items.Length; i++) {
				HeaderItem item = Items[i];
				object o = data[i];
			
				if (o.GetType() != item.Type)
					throw new InvalidDataException($"Object at index {i} of header data must be a {item.Type.Name}, got {o.GetType().Name}");

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

			data = new object[Items.Length];

			for (int i = 0; i < Items.Length; i++) {
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
			if (data.Length != Items.Length)
				throw new ArgumentException("Data params must be the same length as items definition");

			List<byte> header = new(); // list of bytes feels cursed but 
									   // im too lazy to use a byte array :P

			for (int i = 0; i < Items.Length; i++) {
				HeaderItem item = Items[i];
				object o = data[i];

				if (o.GetType() != item.Type)
					throw new InvalidDataException($"Object at index {i} of header data must be a {item.Type.Name}, got {o.GetType().Name}");

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

			data = new object[Items.Length];

			for (int i = 0; i < Items.Length; i++) {
				HeaderItem h = Items[i];

				int count = h.Bytes;
				byte[] hiBytes = header[..count]; // will error very easily

				object hiData = h.FromBytesFunction(hiBytes);

				data[i] = hiData;
				header = header[count..]; // does this straight up work?
			}

			bytes = bytes[TotalBytes..];
		}
	
		public static void AddVersionHeader(ref byte[] original, in bool isText, in ushort version) {
			byte[] vh = new byte[2];

			// set the version bits first

			// keep only lower 14 bits
			ushort value = version;
			value &= 0x3FFF; // 0011_1111_1111_1111

			// byte 0: bits 13..8 (shift down 8) into lower 6 bits
			vh[0] = (byte)((value >> 8) & 0x3F);

			// byte 1: bits 7..0
			vh[1] = (byte)(value & 0xFF);
			
			// then set the type
			vh[0] |= 1 << 6; // always set the 1 bit to prevent a 0 byte

			if (isText) vh[0] |= 1 << 7;

			original = vh.Concat(original).ToArray(); // linq is just less buggy
		}

		public static void GetVersionHeader(ref byte[] original, out bool isText, out ushort version) {
			ushort high = (ushort)(original[0] & 0x3F); // mask 6 bits
			ushort low  = original[1];

			ushort value = (ushort)((high << 8) | low);
			version = value;

			isText = (original[0] & (1 << 7)) != 0; // check first bit

			original = original.Skip(2).ToArray(); // same reason its not that laggy
		}
	}

	/// <summary>
	/// Dict containing all of the header definitions based on version
	/// </summary>
	public static HeaderHelper GetHeaderVersion(ushort version) => version switch {
		1 => new HeaderHelper(HeaderHelper.HeaderItem.Int),
		_ => throw new ArgumentOutOfRangeException(nameof(version), $"Unsupported header version: {version}")
	};

	static HeaderHelper Header => GetHeaderVersion(Config.Building.Saving.VERSION);

	public static void SaveCurrentBuild() {
		var assem = BuildingManager.Instance.Assembly;

		string serializedObject = Assembly.Serialize(assem);

		// header data
		string name = assem.Name;
		int parts = assem.Parts.Count;

		byte[] bytes;

		if (Config.Building.Saving.SaveAsText) {
			serializedObject = CompressionUtil.EncodeGzipBase64(serializedObject);

			Header.AddStringHeader(ref serializedObject,
				parts
				);

			bytes = Encoding.UTF8.GetBytes(serializedObject);
		} else {
			bytes = CompressionUtil.EncodeGzipBytes(serializedObject);

			Header.AddByteHeader(ref bytes,
				parts
				);
		}

		HeaderHelper.AddVersionHeader(ref bytes, Config.Building.Saving.SaveAsText, Config.Building.Saving.VERSION);

		File.WriteAllBytes(Pathify(name), bytes);
	}

	public static void LoadFromFile(string name) {
		string path = Pathify(name);
		ReadFile(path, out string json, out _);

		BuildingManager.Instance.ResetPartsAndGroups();

		var assembly = JsonConvert.DeserializeObject<Assembly.SAssembly>(json, Settings);

		var reconstructed = Assembly.Reconstruct(assembly);

		BuildingManager.Instance.Assembly = reconstructed;
	}

	public static string[] GetAllAssemblyNames() => 
		Directory.GetFiles(Config.Building.Saving.AssembliesLocation, "*" + Config.Building.Saving.SaveExtension)
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

		ReadFile(filePath, out _, out object[] data);

		return data;
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

	private static void ReadFile(string path, out string json, out object[] data) {
		if (!File.Exists(path))
			throw new($"Couldn't load {path} as it doesn't exist in the assemblies folder!");

		byte[] bytes = File.ReadAllBytes(path);
		HeaderHelper.GetVersionHeader(ref bytes, out bool isText, out ushort version);
		var versionCorrectHeader = GetHeaderVersion(version); // may error

		if (isText) {
			json = Encoding.UTF8.GetString(bytes);

			versionCorrectHeader.GetStringHeader(ref json, out data);

			json = CompressionUtil.DecodeGzippedBase64(json);
		} else {
			versionCorrectHeader.GetByteHeader(ref bytes, out data);

			json = CompressionUtil.DecodeGzipBytes(bytes);
		}
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