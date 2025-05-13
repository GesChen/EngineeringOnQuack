using UnityEngine;

public static partial class Config {
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
}