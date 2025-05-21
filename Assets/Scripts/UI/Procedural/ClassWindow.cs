using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassWindow {
	public string Name;
	public WindowItem[] Items;

	public class Configuration {
		public static SizeData FixedSize(Vector2 oneSize)
			=> new(oneSize, oneSize, oneSize);
		public static SizeData NoLimitSize(Vector2 defaultSize)
			=> new(defaultSize, Vector2.zero, Vector2.positiveInfinity);

		public class SizeData {
			public Vector2 Default;
			public Vector2 Minimum;
			public Vector2 Maximum;

			public SizeData(Vector2 @default, Vector2 minimum, Vector2 maximum) {
				Default = @default;
				Minimum = minimum;
				Maximum = maximum;
			}
		}

		public bool Resizable				= true;
		public bool Movable					= true;
		public Color Color					= global::Config.UI.Visual.BackgroundColor;
		public SizeData Size				= NoLimitSize(new(100, 100));
		public UIPosition DefaultPosition	= UIPosition.AnchoredAt(UIPosition.MiddleCenter);
	}

	public Configuration Config = new();
}

public class UIPosition {
	public Vector2 AnchorMin;
	public Vector2 AnchorMax;
	public Vector2 Position;

	public static readonly Vector2 TopLeft		= new(0, 1);
	public static readonly Vector2 TopCenter	= new(.5f, 1);
	public static readonly Vector2 TopRight		= new(1, 1);
	public static readonly Vector2 MiddleLeft	= new(0, .5f);
	public static readonly Vector2 MiddleCenter	= new(.5f, .5f);
	public static readonly Vector2 MiddleRight	= new(1, .5f);
	public static readonly Vector2 BottomLeft	= new(0, 0);
	public static readonly Vector2 BottomCenter	= new(.5f, 0);
	public static readonly Vector2 BottomRight	= new(1, 0);

	public UIPosition(Vector2 anchorMin, Vector2 anchorMax, Vector2 position) {
		AnchorMin = anchorMin;
		AnchorMax = anchorMax;
		Position = position;
	}
	
	public static UIPosition AnchoredAt(Vector2 pos) {
		return new(pos, pos, Vector2.zero);
	}

	public static UIPosition AnchoredOffset(Vector2 pos, Vector2 offset) {
		return new(pos, pos, offset);
	}
}