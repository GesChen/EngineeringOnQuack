using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static partial class Config {
	public static class Fonts {
		public static void Reset() { Fetched = false; }
		static bool Fetched = false;

		// just copy the pattern if you want to add new fonts
		static TMP_FontAsset I_Roboto;
		public static TMP_FontAsset Roboto { get { Fetch(); return I_Roboto; } }

		static TMP_FontAsset I_Consolas;
		public static TMP_FontAsset Consolas { get { Fetch(); return I_Consolas; } }

		public static void Fetch() {
			if (Fetched) return;

			I_Roboto = LoadFont("Fonts/Roboto", "Roboto");
			I_Consolas = LoadFont("Fonts/Consolas", "Consolas");

			Fetched = true;
		}

		private static TMP_FontAsset LoadFont(string path, string fontName) {
			TMP_FontAsset font = Resources.Load<TMP_FontAsset>(path);
			if (font == null) throw new($"Unable to load {fontName}!");
			return font;
		}
	}
}