using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static partial class Config {
	public static class UI {
		public static class Visual {
			public static readonly float Smoothness = 20;
			public static readonly Color BackgroundColor		= new(.21f, .21f, .21f);
			public static readonly Color PreviewWindowColor		= new(.53f, .84f, 1.0f, .20f);

			public static TMP_FontAsset DefaultFont				=> Fonts.Roboto;
			public static readonly FontWeight DefaultWeight		= FontWeight.Light;
			public static readonly Color TextColor				= new(1.0f, 1.0f, 1.0f);
			public static readonly float FontSize				= 22;

			public static readonly Color OutlineColor			= new(.40f, .40f, .40f);
			public static readonly float OutlineThickness		= 2;

			public static readonly float DefaultLayoutSpacing   = 5; // might delete

			public static readonly ColorBlock DefaultColorBlock = new(){
				NormalColor	= new(.27f, .27f, .27f),
				HoverColor		= new(.39f, .39f, .39f),
				PressedColor	= new(.45f, .45f, .45f),
				DisabledColor	= new(.16f, .16f, .16f),
				ToggledColor	= new(.33f, .33f, .33f),
				FadeDuration	= .01f
			};

			public static readonly ColorBlock WhiteColorBlock = new(){
				NormalColor	= new(1f, 1f, 1f),
				HoverColor		= new(0.88f, 0.88f, 0.88f),
				PressedColor	= new(0.82f, 0.82f, 0.82f),
				DisabledColor	= new(0.70f, 0.70f, 0.70f),
				ToggledColor	= new(0.94f, 0.94f, 0.94f),
				FadeDuration	= .01f
			};
		}

		public static class Behaviour {
			public static readonly int MaxFramesForRealization	= 5;
			public static readonly float WindowMinDragDist		= 10;
			public static readonly float FlyoutDistance			= 5;
			public static readonly float FlyoutHoverMargin		= 50;
			public static readonly int DescriptionHoverMs		= 500;
			public static readonly float MaxMouseMovementForClick	= 5;
			public static readonly int TimeForDescriptionChangeMs    = 100;
		}

		// experimental design change
		[Serializable]
		public struct ColorBlock {
			public Color NormalColor;
			public Color HoverColor;
			public Color PressedColor;
			public Color DisabledColor;
			public Color ToggledColor;
			public float FadeDuration;
		}

		public static class Button {
		}

		public static class RightClick {
			public static readonly int WindowPadding	= 2;
		}

		public static class Description {
			public static readonly Color Color				= new(.16f, .16f, .16f);
			public static readonly int Padding				= 5;
			public static readonly float FontSize			= 18;
			public static readonly FontWeight FontWeight	= FontWeight.Light;
			public static readonly Vector2 CursorOffset		= new(0, -30);
		}

		public static class Locations {
			public static readonly string FlyoutTriggerOpenSprite	= Config.Locations.IconsFolder + "dropdown open";
			public static readonly string FlyoutTriggerClosedSprite	= Config.Locations.IconsFolder + "dropdown closed";
			public static readonly string CloseIcon					= Config.Locations.IconsFolder + "close";
		}

		public static class Menu {
			public static readonly float TitleHeight			= 30;
			public static readonly float ItemSpacing			= 0;
			public static readonly float ItemHeight				= 30;
			public static readonly float ItemPadding			= 5;
			public static readonly float IconSize				= 25;
			public static readonly float IconLabelSpacing		= 10;
			public static readonly float FlyoutIndicatorSize	= 20;
			public static readonly float FontSize				= Visual.FontSize - 2;
		}
		
		public static class Window {
			public static readonly float CenterSnapRange = .65f; // 0-1: 0=no center 1=full center

			public static class CornerNode {
				public static readonly Color Color = new(1, 1, 1);
				public static readonly Color CloseButtonColor = Color.red;
				public static readonly float NormalSize = 15;
				public static readonly float HoverSize = 20;
				public static readonly float DragSize = 10;
				public static readonly float ExpansionStartDist = 60;
				public static readonly float ExpansionEndDist = 40;

				public static readonly bool DoubleClickToClose = false;

				private static Sprite m_closeSprite;
				public static Sprite CloseSprite => 
					HF.LoadCached(ref m_closeSprite, Locations.CloseIcon);

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