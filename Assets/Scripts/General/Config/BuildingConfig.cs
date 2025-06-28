using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static partial class Config {
	public static class Building {
		private static readonly float saturation	= .6f;
		private static readonly float value			= 1f;

		public static readonly Color[] Colors = 
			new[]{
/*0*/			0f / 360f,		// Red
/*1*/			30f / 360f,		// Orange
/*2*/			60f / 360f,		// Yellow
/*3*/			120f / 360f,	// Green
/*4*/			210f / 360f,	// Blue
/*5*/			270f / 360f		// Purple
			}.Select(f => Color.HSVToRGB(f, saturation, value))
			.Concat(new[]{
/*6*/			Color.HSVToRGB(206/360f, .02f, .95f), // soft white
/*7*/			Color.HSVToRGB(206/360f, .02f, .60f), // gray
/*8*/			Color.HSVToRGB(206/360f, .02f, .06f) // soft black
			})
			.ToArray();

		public static readonly int PartDefaultColorIndex = 6;
		public static readonly int PartDefaultCompositionIndex = 1;
		
		public static readonly Vector2 ColorPickerItemSize = new(50, 20);
		public static readonly float MaterialPickerItemSize = 50;

		private static Sprite m_MaterialIcon;
		public static Sprite MaterialIcon =>
			HF.LoadResource(ref m_MaterialIcon, Locations.IconsFolder + "Composition/material");

		private static Sprite m_ColorIcon;
		public static Sprite ColorIcon =>
			HF.LoadResource(ref m_ColorIcon, Locations.IconsFolder + "Composition/color1");
	}
}