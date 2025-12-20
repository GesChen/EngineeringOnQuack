using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class Config{
	/// <summary>
	/// Path objects manage local, absolute, persistent paths in 
	/// unity's context automatically. hopefully.
	/// </summary>
	/// <remarks>
	/// HF.GuaranteePath() will be very useful for this.
	/// </remarks>
	public class Path {
		public enum Type {
			Absolute,	// absolute path
			Data,		// read-only game folder
			Local		// persistent data folder
		}

		public Type PathType;
		public string Value;

		public Path(Type type, string value) {
			PathType = type;
			Value = value;
		}

		// static constructors for ease of use
		public static Path AbsolutePath(string value) => new(Type.Absolute, value);
		public static Path DataPath(string value) => new(Type.Data, value);
		public static Path LocalPath(string value) => new(Type.Local, value);

		public override string ToString() {
			string basePath = PathType switch {
				Type.Absolute => "",
				Type.Data => Application.dataPath,
				Type.Local => Application.persistentDataPath,
				_ => throw new ArgumentOutOfRangeException()
			};

			string combined = PathType == Type.Absolute ? Value : System.IO.Path.Combine(basePath, Value);

			combined = combined.Replace('\\', '/');

			return System.IO.Path.GetFullPath(combined);
		}
	}
}