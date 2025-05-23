using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// window, class form (class window, cwindow)
public class CWindow {
	public string Name;
	public WindowItem[] Items;

	public class Configuration {
		public static SizeData FixedSize(Vector2 oneSize)
			=> new(oneSize, oneSize, oneSize);
		public static SizeData FreeSize(Vector2 defaultSize)
			=> new(defaultSize, Vector2.zero, Vector2.positiveInfinity);
		public static SizeData BoundedSize(Vector2 @default, Vector2 min, Vector2 max)
			=> new(@default, min, max);

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
		public SizeData Size				= FreeSize(new(100, 100));
		public UIPosition Position			= UIPosition.AnchoredAt(UIPosition.MiddleCenter);

		// scales with the content, overrides resizing
		public bool ContentDynamic			= false;
	}

	public Configuration Config = new();
}

public class UIPosition {
	public Vector2 AnchorMin;
	public Vector2 AnchorMax;
	public Vector2 Pivot;
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

	public UIPosition(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position) {
		AnchorMin = anchorMin;
		AnchorMax = anchorMax;
		Pivot = pivot;
		Position = position;
	}

	public UIPosition(Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
		: this(anchorMin, anchorMax, position, new(.5f,.5f)) { }
	
	public static UIPosition AnchoredAt(Vector2 pos) {
		return new(pos, pos, pos, Vector2.zero);
	}

	public static UIPosition AnchoredOffset(Vector2 pos, Vector2 offset) {
		return new(pos, pos, pos, offset);
	}

	public static UIPosition CenterAnchoredAt(Vector2 pos, Vector2 offset)
		=> new(pos, pos, new(.5f, .5f), offset);
}