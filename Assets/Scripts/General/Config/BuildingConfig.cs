using System.Linq;
using UnityEngine;
public static partial class Config {
	public static class Building {
		private static readonly float saturation	= .6f;
		private static readonly float value			= 1f;

		public static readonly Color[] Colors = 
			new[]{
				0f / 360f,		// Red
				30f / 360f,		// Orange
				60f / 360f,		// Yellow
				120f / 360f,	// Green
				210f / 360f,	// Blue
				270f / 360f		// Purple
			}.Select(f => Color.HSVToRGB(f, saturation, value))
			.ToArray();

		public static readonly Vector2 ColorPickerItemSize = new(50, 20);
		public static readonly float MaterialPickerItemSize = 50;
	}
}