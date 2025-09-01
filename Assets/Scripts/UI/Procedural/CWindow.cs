using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// window, class form (class window, cwindow)
public class CWindow {
	public string Name;
	public WindowItem[] Items;

	/// <summary>
	/// Configuration for CWindows. 
	/// </summary>
	[Serializable]
	public class Configuration {
		/// <summary>
		/// <para>Resizable (T), Movable (T)</para>
		/// <para>Color, Outline (float, color)</para>
		/// <para>Size (free 100x100), Position (anchored center)</para>
		/// <para>ContentDynamic (F), DynamicPadding (0)</para>
		/// <para>IsFlyout (F), Closable (T), HideOnStart (T)</para>
		/// </summary>
		public Configuration() { }

		public static SizeData FixedSize(Vector2 oneSize) => 
			new(oneSize, oneSize, oneSize);
		public static SizeData FreeSize(Vector2 defaultSize) => 
			new(defaultSize, Vector2.zero, Vector2.positiveInfinity);
		public static SizeData BoundedSize(Vector2 @default, Vector2 min, Vector2 max) => 
			new(@default, min, max);

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
		public (float size, Color color) Outline	
			= (global::Config.UI.Visual.OutlineThickness, global::Config.UI.Visual.OutlineColor);
		public SizeData Size				= FreeSize(new(100, 100));
		public UIPosition Position			= UIPosition.AnchoredAt(UIPosition.MiddleCenter);

		// scales with the content, overrides resizing
		public bool ContentDynamic			= false;
		public FourSides DynamicPadding		= FourSides.Zero;

		// temporary till i can think of a better solution
		public bool IsFlyout				= false;

		// also temporary i guess
		public bool Closable				= true;

		// ok we're just flags at this point
		public bool HideOnStart				= true; // might turn into connfig

		/*public static Configuration FixedConfig(SizeData Size, UIPosition pos, bool flyout = false)
			=> new() {
				Resizable = false,
				Movable = false,
			};
*/
	}

	/// <summary>
	/// Name, Config, Items
	/// </summary>
	public CWindow() { CreationFrame = Time.frameCount; }

	public Configuration Config = new();
	public List<TimedEventInvoker.TimedEvent> CustomEvents;
	public string GroupPath = null;
	public WindowRealiser.Group RealGroup;

	public int CreationFrame { get; }

	private LiveWindow m_realisedWindow;
	public LiveWindow RealisedWindow {
		get {
			if (m_realisedWindow == null) {
				if (!ReferenceEquals(m_realisedWindow, null))
					throw new($"Window \"{Name}\" destroyed!");
				throw new($"Window \"{Name}\" not realised!");
			}
			return m_realisedWindow;
		}
	}
	public LiveWindow GetRealisedOrNull() => m_realisedWindow;
	public void SetRealised(LiveWindow live) {
		m_realisedWindow = live;
	}

	/// <summary>
	/// Adds an Event with a Timing
	/// </summary>
	/// <param name="timing"></param>
	/// <param name="action">(timedeventinvoker source)</param>
	/// <returns></returns>
	public CWindow AddEvent(
		TimedEventInvoker.Timing timing, 
		TimedEventInvoker.TimedEventCall action) {

		CustomEvents ??= new();
		CustomEvents.Add(new(timing, action));
		return this;
	}

	public CWindow SetGroup(string path) {
		GroupPath = path;
		return this;
	}

	public override string ToString() {
		return $"CW {Name}";
	}
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

	public UIPosition() { }

	public static UIPosition AnchoredAt(Vector2 pos) => 
		new(pos, pos, pos, Vector2.zero);

	public static UIPosition AnchoredOffset(Vector2 pos, Vector2 offset) => 
		new(pos, pos, pos, offset);

	public static UIPosition CenterAnchoredAt(Vector2 pos, Vector2 offset) =>
		new(pos, pos, new(.5f, .5f), offset);

	public static UIPosition LayoutItem => AnchoredAt(TopLeft);
}