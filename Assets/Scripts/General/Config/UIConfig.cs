using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static partial class Config {
	public static class UI {
		public static class Window {
			public static readonly float CenterSnapRange = .65f; // 0-1: 0=no center 1=full center

			public static class CornerNode {
				public static readonly Color Color = new(1, 1, 1);
				public static readonly float NormalSize = 15;
				public static readonly float HoverSize = 20;
				public static readonly float DragSize = 10;
				public static readonly float ExpansionStartDist = 60;
				public static readonly float ExpansionEndDist = 40;

				public static float EasingFunction(float x) {
					if (x <= 0) return 0;
					if (x >= 1) return 1;

					// can be changed to customize behaviour
					float a = 2;
					float b = 2;

					float powStart = Mathf.Pow(x, a);

					return powStart / (powStart + Mathf.Pow(1 - x, b));
				}
			}
		}

		public static class Visual {
			public static readonly float Smoothness = 20;

			public static readonly Color BackgroundColor		= new(.21f, .21f, .21f);
			public static readonly Color PreviewWindowColor		= new(.53f, .84f, 1.0f, .20f);
			public static readonly Color ButtonHoverColor		= new(.39f, .39f, .39f);
			public static readonly Color ButtonPressedColor		= new(.25f, .25f, .25f);
			public static readonly Color ButtonDisabledColor	= new(.16f, .16f, .16f);
			public static readonly Color TextColor				= new(1.0f, 1.0f, 1.0f);
			public static readonly float FontSize = 22;
			public static readonly Color OutlineColor			= new(.40f, .40f, .40f);
			public static readonly int OutlineThickness = 2;
		}
	}
	/*
		public static class UI {
			public static readonly int		MouseValidityMargin			= 50;
			public static readonly float	MaxRightClickTime			= .1f;
			public static readonly float	MaxMovementAfterClick		= 30;
			public static readonly float	MaxMouseMovementToCount		= 10;
			public static readonly float	HoverTimeUntilDescription	= .5f;
			public static readonly Color	BackgroundColor				= new(.21f, .21f, .21f);
			public static readonly Color	DescriptionBackgroundColor	= new(.1f, .1f, .1f);
			public static readonly Color	ButtonHoverColor			= new(.39f, .39f, .39f);
			public static readonly Color	ButtonPressedColor			= new(.25f, .25f, .25f);
			public static readonly Color	TextColor					= new(1, 1, 1);
			public static readonly int		OutlineThickness			= 2;
			public static readonly Color	OutlineColor				= new(.4f, .4f, .4f);
			public static readonly int		ItemHeight					= 30;
			public static readonly int		VerticalSpacing				= 2;
			public static readonly int		SidePadding					= 2;
			public static readonly int		InsidePadding				= 3;
			public static readonly int		IconSize					= 30;
			public static readonly float	DropDownArrowSize			= 20;
			public static readonly int		FontSize					= 22;
			public static readonly string	FontLocation				= "";
			public static readonly Vector2	DisplayTopLeftCornerOffset	= new(10, 10);
			public static readonly float	MinDistFromSides			= 5;
			public static readonly float	DropDownDisplayOffset		= 6;
			public static readonly int		DescriptionFontSize			= 20;
			public static readonly int		DescriptionHeight			= 25;
		}
	*/
}