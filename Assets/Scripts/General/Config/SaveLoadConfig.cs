using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public partial class Config {
	public class SaveLoad {
		public class SaveLoadConfiguration {
			public Dictionary<ushort, HeaderHelper> Headers;
			public ushort CurrentHeaderVersion;

			public bool SaveAsText;

			public string SaveLocation;
			public string SaveExtension;

			public SaveLoadConfiguration(
				ushort currentHeaderVersion,
				Dictionary<ushort, HeaderHelper> headers,
				bool saveAsText,
				string saveLocation,
				string saveExtension) {

				SaveAsText = saveAsText;
				SaveLocation = saveLocation;
				SaveExtension = saveExtension;
				Headers = headers;
				CurrentHeaderVersion = currentHeaderVersion;
			}
		}

		public static readonly JsonSerializerSettings SerializerSettings = new() {
			TypeNameHandling = TypeNameHandling.Auto
		};

		static SaveLoadConfiguration m_buildingConfig;
		public static SaveLoadConfiguration BuildingConfig => HF.LoadCached(
			ref m_buildingConfig,
			() => {
				string AssembliesLocation = HF.GuaranteePath(
					Path.LocalPath("Assemblies").ToString()
				);
				return new SaveLoadConfiguration(
					1,
					new(){
						{ 1, new HeaderHelper(
							HeaderHelper.HeaderItem.Int
						) }
					},
					true,
					AssembliesLocation,
					".assembly"
				);
			}
		);
		public static readonly bool BuildingConfig_SaveClipboard = true;

		static SaveLoadConfiguration m_scriptsConfig;
		public static SaveLoadConfiguration ScriptsConfig => HF.LoadCached(
			ref m_scriptsConfig,
			() => {
				string scriptsLocation = HF.GuaranteePath(
					Path.LocalPath("Scripts").ToString()
				);

				return new SaveLoadConfiguration(
					1,
					new() {
						{ 1, new HeaderHelper(
							HeaderHelper.HeaderItem.Int
						) }
					},
					true,
					scriptsLocation,
					".qk"
				);
			}
		);

		static SaveLoadConfiguration m_worldConfig;
		public static SaveLoadConfiguration WorldConfig => HF.LoadCached(
			ref m_worldConfig,
			() => {
				string savesLocation = HF.GuaranteePath(
					Path.LocalPath("Worlds").ToString()
				);

				return new SaveLoadConfiguration(
					1,
					new() {
						{ 1, new HeaderHelper(
							HeaderHelper.HeaderItem.Int
						) }
					},
					true,
					savesLocation,
					".eoq"
				);
			}
		);
	}
}