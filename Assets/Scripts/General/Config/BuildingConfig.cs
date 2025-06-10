using UnityEngine;
public static partial class Config {
	public static class Building {
		public static readonly Color[] Colors = {
			MoreColors.PastelRed,
			MoreColors.PastelOrange,
			MoreColors.PastelYellow,
			MoreColors.PastelGreen,
			MoreColors.PastelBlue,
			MoreColors.PastelPurple
		};

		public static readonly Vector2 ColorPickerItemSize = new(50, 20);
		public static readonly float MaterialPickerItemSize = 50;
	}
}