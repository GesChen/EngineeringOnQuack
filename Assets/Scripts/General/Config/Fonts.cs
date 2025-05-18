using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static partial class Config {
	public static class Fonts {
		static bool FetchedYet = false;

		
		static TMP_FontAsset I_Roboto;
		public static TMP_FontAsset Roboto { get { Fetch(); return I_Roboto; } }

		public static void Fetch() {
			if (FetchedYet) return;

			I_Roboto = Resources.Load("Fonts/Roboto") as TMP_FontAsset;
		}
	}
}