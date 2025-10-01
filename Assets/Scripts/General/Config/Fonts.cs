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

			I_Roboto = Resources.Load<TMP_FontAsset>("Fonts/Roboto");
			if (I_Roboto == null) throw new("Unable to load Roboto!");

			I_Consolas = Resources.Load<TMP_FontAsset>("Fonts/Consolas");
			if (I_Consolas == null) throw new("Unable to load Consolas!");

			Fetched = true;
		}
	}
}