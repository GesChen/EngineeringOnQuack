using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class Config {
	public static class Input {
		public static float clickMaxMovement = 5;
		public static int clickMaxTimeMs = 100;
		public static int doubleClickMaxTimeMs = 500;
		public static float doubleClickMaxMovement = 20;
		public static readonly float SmoothingFactor = .3f;
	}
}