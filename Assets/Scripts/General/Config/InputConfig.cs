using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class Config {
	public static class Input {
		public static readonly float	clickMaxMovement = 5;
		public static readonly int		clickMaxTimeMs = 100;
		public static readonly int		doubleClickMaxTimeMs = 500;
		public static readonly float	doubleClickMaxMovement = 20;
		public static readonly float	SmoothingFactor = .3f;

		public static readonly float	ScrollSensitivity = .05f;
	}
}