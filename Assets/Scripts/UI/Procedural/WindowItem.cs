using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class WindowItem {
	public string Name;

	public struct LayoutConfig {
		public bool IsCustom;

		// fixed or dynamic? anchors together or not?
		public bool IsFixed;

		// fixed values
		public Vector2 SizeDelta;
		public UIPosition FixedPosition;

		// applies for both
		public FourSides Padding;

		// dynamic values
		public FourSides Margins; // offset min 

		// anchor settings
		// left = min.x		right = 1 - max.x
		// up = min.y		down = 1 - max.y
		public FourSides Position; // responsible for the anchor
								   // 0 is fill
								   // think of each side as being pushed in by the amount
								   // of the position value so
								   // |       |<-.4--| is .4 right

		public static LayoutConfig FillLayout => new() {
			IsFixed = false,
			Padding = new(0),

			Margins = new(0),
			Position = new(0)
		};

		/// <summary>
		/// <para>Constructor for a layout that changes with the parent</para> 
		/// <para>Default full fill anchors with 0 margin 0 padding</para>
		/// </summary>
		/// <param name="margin">Offsets</param>
		/// <param name="padding">Inner container padding</param>
		/// <param name="position">Anchors</param>
		public static LayoutConfig DynamicLayout(
			FourSides? margin = null,
			FourSides? padding = null,
			FourSides? position = null) => new() {

				IsFixed = false,
				Padding = padding ?? FourSides.Zero,

				Position = position ?? FourSides.Zero,
				Margins = margin ?? FourSides.Zero,
			};

		public static LayoutConfig FixedLayout(UIPosition position, Vector2 size, FourSides? padding = null) => new() {
			IsFixed = true,
			Padding = padding ?? FourSides.Zero,

			SizeDelta = size,
			FixedPosition = position
		};

		public static LayoutConfig LayoutElement(Vector2 size, FourSides? padding = null) => new() {
			IsFixed = true,
			Padding = padding ?? FourSides.Zero,

			SizeDelta = size,
			FixedPosition = UIPosition.AnchoredAt(UIPosition.TopLeft) // should be overriden by the layout
		};

		public static LayoutConfig LayoutElementDynamic(FourSides? padding = null) => new() {
			IsFixed = true,
			Padding = padding ?? FourSides.Zero,

			SizeDelta = new(0, 0),
			FixedPosition = UIPosition.AnchoredAt(UIPosition.TopLeft) // should be overriden by the layout
		};

		public static LayoutConfig LayoutToWidth(
			float anchory,
			float pivoty,
			float offsety,
			float height,
			FourSides padding = default) => FixedLayout(
				new UIPosition(
					new(0, anchory), new(1, anchory), new(.5f, pivoty), new(0, offsety)
				),
				new(0, height),
				padding
				);

		public static LayoutConfig Custom(
			Vector2 sizeDelta = default,
			UIPosition fixedPosition = default,
			FourSides padding = default,
			FourSides margins = default,
			FourSides position = default
		) => new() {
			IsCustom		= true,
			SizeDelta		= sizeDelta,
			FixedPosition	= fixedPosition,
			Padding			= padding,
			Margins			= margins,
			Position		= position
		};
	}
	public LayoutConfig Layout;

	// originally arrays but the memory difference was negligible
	public List<PComponents.Component> Construction = new();

	public List<WindowItem> SubItems = new();

	private RectTransform m_realObject;
	public Func<RectTransform> RealObject => () => m_realObject;

	// we can store the container in here i guess? like 
	// i really cant think of a better way to do this tbh
	public RectTransform ContentsObject;

	public List<TimedEventInvoker.TimedEvent> CustomEvents;

	public WindowItem SetSubItems(params WindowItem[] subs) {
		SubItems = subs.ToList();
		return this;
	}
	public WindowItem AddSubItems(params WindowItem[] subs) {
		if (SubItems == null) {
			SubItems = new();
			Debug.LogWarning("Subitems was null with AddSubItems usage. Perhaps Set should have been used instead?");
		}

		SubItems.AddRange(subs.ToList());
		return this;
	}
	public WindowItem AddDescription(string description) {
		Construction.Add(new PComponents.Description(description));
		return this;
	}

	/// <summary>
	/// Adds a list of components to this WI. Any duplicately typed components will be ignored.
	/// </summary>
	public WindowItem AddComponents(params PComponents.Component[] comps) {
		// hashset construction faster still O(n+m) vs O(n*m)
		var typesSeen = new HashSet<Type>(Construction.Select(c => c.GetType()));

		foreach (var comp in comps)
			if (typesSeen.Add(comp.GetType()))
				Construction.Add(comp);

		return this;
	}

	// honestly idk the diff between this and onrealised atp the codebase
	// is so chopped
	public WindowItem AddEvent(
		TimedEventInvoker.Timing timing,
		TimedEventInvoker.TimedEventCall action) {

		CustomEvents ??= new();
		CustomEvents.Add(new(timing, action));
		return this;
	}

	public delegate void Realization(RectTransform rt, WindowItem self);
	public event Realization RealizationEvent;

	/// <param name="action">Signature: (RectTransform rt, WindowItem self)</param>
	public WindowItem OnRealized(Realization action) {
		RealizationEvent += action;
		return this;
	}
	internal void BecomeRealised(RectTransform rt, WindowItem self) {
		m_realObject = rt;
		RealizationEvent?.Invoke(rt, self);
	}

	public PComponents.Component GetComponent<T>() where T : PComponents.Component {
		var tryFind = Construction.FirstOrDefault(c => c is T);

		if (tryFind == null)
			Debug.LogError($"No component found of type {typeof(T).Name}");

		return tryFind;
	}

	private WindowItem(
		string name,
		LayoutConfig layout,
		List<PComponents.Component> components,
		List<WindowItem> items) {
		Name = name;
		Layout = layout;
		Construction = components;
		SubItems = items;
	}

	#region Custom Constructors
	public static WindowItem NewEmpty(string name, LayoutConfig layout, List<WindowItem> subitems = null) =>
		new(
			name,
			layout,
			new() { },
			subitems
			);
	public static WindowItem NewEmpty(LayoutConfig layout, List<WindowItem> subitems = null) =>
		NewEmpty("Empty Object", layout, subitems);

	public static WindowItem NewImage(string name, PComponents.Image image, LayoutConfig layout) =>
		new(
			name,
			layout,
			new() { image },
			null
		);
	public static WindowItem NewImage(PComponents.Image image, LayoutConfig layout) =>
		NewImage("Image", image, layout);

	public static WindowItem NewText(string name, PComponents.Text text, LayoutConfig layout) =>
		new(
			name,
			layout,
			new() { text },
			null
		);
	public static WindowItem NewText(PComponents.Text text, LayoutConfig layout) =>
		NewText("Text", text, layout);

	// theres GOTTA be a better way other than overloading the fuck out of these but i cant think of one rn
	// im just gonna give em unique names for now idk

	public static WindowItem NewButton(string name, PComponents.Button button, LayoutConfig layout) =>
		new(
			name,
			layout,
			new() {
				new PComponents.Image(),
				button
			},
			null
			);
	public static WindowItem NewButton(PComponents.Button button, LayoutConfig layout) =>
		NewButton("Button", button, layout);


	// privating these for now cuz they're kinda weird? idk
	private static WindowItem NewButton(string name, PComponents.Button button, LayoutConfig layout, PComponents.Component inner) =>
		new(
			name,
			layout,
			new() {
				new PComponents.Image(),
				button
			},
			new() {
				new(
					"Inner component",
					LayoutConfig.FillLayout,
					new() { inner },
					null
					)
			}
		);
	private static WindowItem NewButton(PComponents.Button button, LayoutConfig layout, PComponents.Component inner) =>
		NewButton("Button", button, layout, inner);

	/// <summary>
	/// Adds extra subitem with the image instead of setting the button's image directly
	/// </summary>
	public static WindowItem NewButtonCustomImageOverlay(string name, PComponents.Button button, PComponents.Image image, LayoutConfig layout) =>
		NewButton(name, button, layout, image);
	public static WindowItem NewButtonCustomImageOverlay(PComponents.Button button, PComponents.Image image, LayoutConfig layout) =>
		NewButtonCustomImageOverlay("Button", button, image, layout);

	/// <summary>
	/// Replaces the button's image with a custom one. This image's color will be affected by button
	/// transition. Reccomended to use the whitecolorblock from config.ui for this's button
	/// </summary>
	public static WindowItem NewButtonCustomImageComponent(string name, PComponents.Button button, PComponents.Image image, LayoutConfig layout) =>
		new(
			name,
			layout,
			new() {
				image,
				button
			},
			null
			);
	public static WindowItem NewButtonCustomImageComponent(PComponents.Button button, PComponents.Image image, LayoutConfig layout) =>
		NewButtonCustomImageComponent("Button", button, image, layout);

	public static WindowItem NewButtonCustomText(string name, PComponents.Button button, PComponents.Text text, LayoutConfig layout, LayoutConfig? textLayout = null) => new(
		name,
		layout,
		new() {
			new PComponents.Image(),
			button
		},
		new() {
			NewText(
				text,
				textLayout ?? LayoutConfig.DynamicLayout(Config.UI.Button.CustomTextDefaultMargins * FourSides.One)
			)
		}
	);
	public static WindowItem NewButtonCustomText(PComponents.Button button, PComponents.Text text, LayoutConfig layout) =>
		NewButtonCustomText("Button", button, text, layout);

	public static WindowItem NewInputField(string name, PComponents.InputField inputField, LayoutConfig layout) =>
		new(
			name,
			layout,
			new() {
				new PComponents.Image(),
				inputField
			},
			null
			);
	public static WindowItem NewInputField(PComponents.InputField inputField, LayoutConfig layout) =>
		NewInputField("InputField", inputField, layout);

	/// <param name="layoutComponent">Use Pcomp.layout.direction. ...</param>
	public static WindowItem NewLayout(string name, PComponents.Layout layoutComponent, LayoutConfig layout, List<WindowItem> items) => 
		new(
			name,
			layout,
			new() { layoutComponent },
			items
			);

	/// <param name="layoutComponent">Use Pcomp.layout.direction. ...</param>
	public static WindowItem NewLayout(PComponents.Layout layoutComponent, LayoutConfig layout, List<WindowItem> items) => 
		NewLayout("Layout", layoutComponent, layout, items);

	public static WindowItem NewFlyoutTrigger(string name, PComponents.FlyoutTrigger trigger, LayoutConfig layout, PComponents.HoverTarget hover = null) => 
		new(
			name,
			layout,
			new() {
				new PComponents.Image(),
				hover ?? new PComponents.HoverTarget(),
				new PComponents.FlyoutHider(),
				trigger
			},
			null
			);
	public static WindowItem NewFlyoutTrigger(PComponents.FlyoutTrigger trigger, LayoutConfig layout, PComponents.HoverTarget hover = null) => 
		NewFlyoutTrigger("Flyout Trigger", trigger, layout, hover);

	public static WindowItem NewFlyoutTriggerWithLabel(string name, PComponents.FlyoutTrigger trigger, PComponents.Text label, LayoutConfig layout, PComponents.HoverTarget hover = null) =>
		new(
			name,
			layout,
			new() {
				new PComponents.Image(),
				hover ?? new PComponents.HoverTarget(),
				new PComponents.FlyoutHider(),
				trigger
			},
			new() {
				NewText(
					label,
					LayoutConfig.FillLayout
				)
			}
			);
	public static WindowItem NewFlyoutTriggerWithLabel(PComponents.FlyoutTrigger trigger, PComponents.Text label, LayoutConfig layout, PComponents.HoverTarget hover = null) =>
		NewFlyoutTriggerWithLabel("Flyout Trigger", trigger, label, layout, hover);


	public static WindowItem NewScrollView(string name, PComponents.ScrollView scroll, LayoutConfig layout, List<WindowItem> items) => 
		new(
			name,
			layout,
			new() {
				scroll
			},
			items
		);
	public static WindowItem NewScrollView(PComponents.ScrollView scroll, LayoutConfig layout, List<WindowItem> items) =>
		NewScrollView("Scroll View", scroll, layout, items);

	/// <summary>
	/// Wraps this WI in a parent wrapper, useful to bypass layoutelement scaling
	/// </summary>
	/// <remarks>
	/// This method should be placed at the end of a chain
	/// </remarks>
	public WindowItem Wrap() {
		WindowItem wrapper = NewEmpty(
			$"Wrapper for {Name}",
			Layout);

		Layout = LayoutConfig.FillLayout;
		wrapper.SetSubItems(this);
		
		if (Construction.TryFind(c => c is PComponents.LayoutElement, out var comp)) {
			Construction.Remove(comp);
			wrapper.AddComponents(comp);
		}
		
		return wrapper;
	}

	#endregion

	public override string ToString() {
		return $"WI {Name}";
	}
}