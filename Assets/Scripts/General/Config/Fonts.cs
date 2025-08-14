using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static partial class Config {
	public static class Fonts {
		public static void Reset() { Fetched = false; }
		static bool Fetched = false;

		
		static TMP_FontAsset I_Roboto;
		public static TMP_FontAsset Roboto { get { Fetch(); return I_Roboto; } }

		public static void Fetch() {
			if (Fetched) return;

			I_Roboto = Resources.Load<TMP_FontAsset>("Fonts/Roboto");
			if (I_Roboto == null) throw new("Unable to load Roboto!");

			Fetched = true;
		}
	}
}