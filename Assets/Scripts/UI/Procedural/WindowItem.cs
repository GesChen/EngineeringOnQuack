using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class WindowItem {
	public string Name;

	public struct LayoutConfig {
		public bool IsFixed;

		// dynamic values
		public FourSides Margins;
		public FourSides Padding;
		public FourSides Position; // anchor min max, side offset 

		// fixed values
		public Vector2 SizeDelta;
		public UIPosition FixedPosition;

		public static LayoutConfig FillLayout => new() {
			IsFixed = false,

			Margins = new(0),
			Padding = new(0),
			Position = new(0)
		};

		public static LayoutConfig DynamicLayout(FourSides margin, FourSides padding, FourSides position) => new() {
			IsFixed = false,

			Margins = margin,
			Padding = padding,
			Position = position
		};

		public static LayoutConfig FixedLayout(UIPosition position, Vector2 size) => new() {
			IsFixed = true,

			SizeDelta = size,
			FixedPosition = position
		};
	}
	public LayoutConfig Layout;

	// originally arrays but the memory difference was negligible
	public List<Component> Construction = new();

	public List<WindowItem> SubItems = new();

	public WindowItem WithSubItems(params WindowItem[] subs) {
		SubItems = subs.ToList();
		return this;
	}
	public WindowItem AddSubItems(params WindowItem[] subs) {
		SubItems.AddRange(subs.ToList());
		return this;
	}
	public WindowItem SetLayoutElement(Components.LayoutElement element) {
		Construction.Add(element);
		return this;
	}

	public class Component {

	}

	public class Components {
		public class Image : Component {
			public Color Color;
			public string TextureResource;
			public bool PreserveAspect; // explicity set to false only in special cases needed
			// otherwise it makes sense to have it always true

			public Image(Color color, string spriteResourcePath, bool preserveAspect) {
				Color = color;
				TextureResource = spriteResourcePath;
				PreserveAspect = preserveAspect;
			}
			public Image(Color color) : this(
				color,
				null,
				true) { }
			public Image(string spriteResourcePath) : this(
				Color.white,
				spriteResourcePath,
				true) { }
			public Image() : this(
				Color.white,
				null, 
				true) { }
		}

		public class Button : Component {
			public bool Enabled;
			public Color NormalColor;
			public Color HighlightedColor;
			public Color PressedColor;
			public Color DisabledColor;
			public List<UnityEngine.Events.UnityAction> OnClick;

			public Button(
					bool enabled, 
					Color normalColor, 
					Color highlightedColor, 
					Color pressedColor, 
					Color disabledColor,
					List<UnityEngine.Events.UnityAction> onClick) {
				Enabled = enabled;
				NormalColor = normalColor;
				HighlightedColor = highlightedColor;
				PressedColor = pressedColor;
				DisabledColor = disabledColor;
				OnClick = onClick;
			}

			public Button(List<UnityEngine.Events.UnityAction> onClick) : this(
				true,
				Config.UI.Button.DefaultColor,
				Config.UI.Button.HoverColor,
				Config.UI.Button.PressedColor,
				Config.UI.Button.DisabledColor,
				onClick) { }

			public Button() : this(
				true,
				Config.UI.Button.DefaultColor,
				Config.UI.Button.HoverColor,
				Config.UI.Button.PressedColor,
				Config.UI.Button.DisabledColor,
				new()) { }
		}

		public class Text : Component {
			public string Content;
			public TMP_FontAsset Font;
			public FontStyles Style;
			public float FontSize;
			public Color Color;
			public TextAlignmentOptions Alignment;

			public Text(
					string content, 
					TMP_FontAsset font, 
					FontStyles style,
					float fontSize, 
					Color color, 
					TextAlignmentOptions alignment) {
				Content = content;
				Font = font;
				Style = style;
				FontSize = fontSize;
				Color = color;
				Alignment = alignment;
			}

			public Text(string content) : this(
				content,
				Config.Fonts.Roboto,
				FontStyles.Normal,
				Config.UI.Visual.FontSize,
				Config.UI.Visual.TextColor,
				TextAlignmentOptions.TopLeft
				) { }

			public Text(string content, Color color) : this(
				content,
				Config.Fonts.Roboto,
				FontStyles.Normal,
				Config.UI.Visual.FontSize,
				color,
				TextAlignmentOptions.TopLeft
				) { }
		}

		public class Layout : Component {
			public enum Type {
				Vertical,
				Horizontal // might add grid if possible?
			}
			public Type LayoutType;

			public float Spacing;
			// padding value is taken care of by the item's layout
			public TextAnchor ItemAlignment;

			// fixed: items are scaled evenly to fit the fixed size container
			// dynamic: this item scales to fit the items, adds a contentfitter
			public bool FixedSize;

			// do you scale items in this axis so they fill this dimension?
			// vertical: scale height of items to fit this contianer?
			// affected by layoutelement scale
			public bool FillDimension;

			// kinda hard to explain
			// in vert, this would force width to match parent
			public bool MatchOtherDimension;

			public Layout(
				Type layoutType, 
				float spacing, 
				TextAnchor itemAlignment, 
				bool fixedSize, 
				bool fillDimension,
				bool matchOtherDimension) {
				
				LayoutType = layoutType;
				Spacing = spacing;
				ItemAlignment = itemAlignment;
				FixedSize = fixedSize;
				FillDimension = fillDimension;
				MatchOtherDimension = matchOtherDimension;
			}

			public static Layout Horizontal(
				float spacing,
				TextAnchor itemAlignment,
				bool fixedSize,
				bool fillHorizontally,
				bool matchOtherDimension)
				=> new (
					Type.Horizontal,
					spacing,
					itemAlignment,
					fixedSize,
					fillHorizontally,
					matchOtherDimension);

			public static Layout HorizontalFixed(
				float spacing,
				TextAnchor itemAlignment,
				bool fillHorizontally,
				bool matchOtherDimension)
				=> new(
					Type.Horizontal,
					spacing,
					itemAlignment,
					true,
					fillHorizontally,
					matchOtherDimension);

			public static Layout HorizontalDynamic(
				float spacing,
				TextAnchor itemAlignment)
				=> new(
					Type.Horizontal,
					spacing,
					itemAlignment,
					false,
					false,
					false);

			public static Layout Vertical(
				float spacing,
				TextAnchor itemAlignment,
				bool fixedSize,
				bool fillVertically,
				bool matchOtherDimension)
				=> new(
					Type.Vertical,
					spacing,
					itemAlignment,
					fixedSize,
					fillVertically,
					matchOtherDimension);

			public static Layout VerticalFixed(
				float spacing,
				TextAnchor itemAlignment,
				bool fillVertically,
				bool matchOtherDimension)
				=> new(
					Type.Vertical,
					spacing,
					itemAlignment,
					true,
					fillVertically,
					matchOtherDimension);

			public static Layout VerticalDynamic(
				float spacing,
				TextAnchor itemAlignment)
				=> new(
					Type.Vertical,
					spacing,
					itemAlignment,
					false,
					false,
					false);

		}

		public class LayoutElement : Component {
			public float SizeMultiplier;
		}

		public class Dropdown : Component {
			
		}
	}

	private WindowItem(string name, LayoutConfig layout, List<Component> components, List<WindowItem> items) {
		Name = name;
		Layout = layout;
		Construction = components;
		SubItems = items;
	}

	#region Custom Constructors
	public static WindowItem NewImage(string name, Components.Image image, LayoutConfig layout) 
		=> new(
			name,
			layout,
			new() { image },
			null
		);
	public static WindowItem NewImage(Components.Image image, LayoutConfig layout)
		=> NewImage("Image", image, layout);

	public static WindowItem NewText(string name, Components.Text text, LayoutConfig layout) 
		=> new(
			name,
			layout,
			new() { text },
			null
		);
	public static WindowItem NewText(Components.Text text, LayoutConfig layout)
		=> NewText("Text", text, layout);

	public static WindowItem NewButton(string name, Components.Button button, LayoutConfig layout, Component inner)
		=> new(
			name,
			layout,
			new() {
				new Components.Image(),
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
	public static WindowItem NewButton(Components.Button button, LayoutConfig layout, Component inner)
		=> NewButton("Button", button, layout, inner);

	public static WindowItem NewButton(string name, Components.Button button, LayoutConfig layout)
		=> new(
			name,
			layout,
			new() {
				new Components.Image(),
				button
			},
			null
		);
	public static WindowItem NewButton(Components.Button button, LayoutConfig layout)
		=> NewButton("Button", button, layout);

	public static WindowItem NewLayout(string name, Components.Layout layoutComponent, LayoutConfig layout, List<WindowItem> items)
		=> new(
			name,
			layout,
			new() { layoutComponent },
			items
			);
	public static WindowItem NewLayout(Components.Layout layoutComponent, LayoutConfig layout, List<WindowItem> items)
		=> NewLayout("Layout", layoutComponent, layout, items);
	#endregion
}