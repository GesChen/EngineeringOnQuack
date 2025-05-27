using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class WindowItem {
	public string Name;

	public struct LayoutConfig {
		public bool IsFixed;
		public FourSides Padding;

		// dynamic values
		public FourSides Margins;
		public FourSides Position; // anchor min max, side offset 

		// fixed values
		public Vector2 SizeDelta;
		public UIPosition FixedPosition;

		public static LayoutConfig FillLayout => new() {
			IsFixed = false,
			Padding = new(0),

			Margins = new(0),
			Position = new(0)
		};

		public static LayoutConfig DynamicLayout(FourSides margin, FourSides padding, FourSides position) => new() {
			IsFixed = false,
			Padding = padding,

			Margins = margin,
			Position = position
		};

		public static LayoutConfig FixedLayout(UIPosition position, Vector2 size, FourSides padding) => new() {
			IsFixed = true,
			Padding = padding,

			SizeDelta = size,
			FixedPosition = position
		};
		public static LayoutConfig FixedLayout(UIPosition position, Vector2 size) => new() {
			IsFixed = true,
			Padding = FourSides.Zero,

			SizeDelta = size,
			FixedPosition = position
		};
	}
	public LayoutConfig Layout;

	// originally arrays but the memory difference was negligible
	public List<PComponents.Component> Construction = new();

	public List<WindowItem> SubItems = new();

	public WindowItem WithSubItems(params WindowItem[] subs) {
		SubItems = subs.ToList();
		return this;
	}
	public WindowItem AddSubItems(params WindowItem[] subs) {
		SubItems.AddRange(subs.ToList());
		return this;
	}
	public WindowItem WithDescription(string description) {
		Construction.Add(new PComponents.Description(description));
		return this;
	}
	public void AddDescription(string description) {
		Construction.Add(new PComponents.Description(description));
	}
	public WindowItem SetLayoutElement(PComponents.LayoutElement element) {
		Construction.Add(element);
		return this;
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
	public static WindowItem NewImage(string name, PComponents.Image image, LayoutConfig layout) 
		=> new(
			name,
			layout,
			new() { image },
			null
		);
	public static WindowItem NewImage(PComponents.Image image, LayoutConfig layout)
		=> NewImage("Image", image, layout);

	public static WindowItem NewText(string name, PComponents.Text text, LayoutConfig layout) 
		=> new(
			name,
			layout,
			new() { text },
			null
		);
	public static WindowItem NewText(PComponents.Text text, LayoutConfig layout)
		=> NewText("Text", text, layout);

	public static WindowItem NewButton(string name, PComponents.Button button, LayoutConfig layout, PComponents.Component inner)
		=> new(
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
	public static WindowItem NewButton(PComponents.Button button, LayoutConfig layout, PComponents.Component inner)
		=> NewButton("Button", button, layout, inner);

	public static WindowItem NewButton(string name, PComponents.Button button, LayoutConfig layout)
		=> new(
			name,
			layout,
			new() {
				new PComponents.Image(),
				button
			},
			null
		);
	public static WindowItem NewButton(PComponents.Button button, LayoutConfig layout)
		=> NewButton("Button", button, layout);

	public static WindowItem NewLayout(string name, PComponents.Layout layoutComponent, LayoutConfig layout, List<WindowItem> items)
		=> new(
			name,
			layout,
			new() { layoutComponent },
			items
			);
	public static WindowItem NewLayout(PComponents.Layout layoutComponent, LayoutConfig layout, List<WindowItem> items)
		=> NewLayout("Layout", layoutComponent, layout, items);

	public static WindowItem NewFlyoutTrigger(string name, PComponents.FlyoutTrigger trigger, LayoutConfig layout)
		=> new(
			name,
			layout,
			new() {
				new PComponents.Image(),
				new PComponents.HoverTarget(),
				trigger },
			null
			);
	public static WindowItem NewFlyoutTrigger(PComponents.FlyoutTrigger trigger, LayoutConfig layout)
		=> NewFlyoutTrigger("Flyout Trigger", trigger, layout);
	#endregion
}