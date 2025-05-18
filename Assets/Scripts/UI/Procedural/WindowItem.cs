using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WindowItem {
	public class FourSides {
		public float Up;
		public float Right;
		public float Down;
		public float Left;
		public FourSides(float up, float right, float down, float left) {
			Up = up;
			Right = right;
			Down = down;
			Left = left;
		}
	}
	public struct LayoutConfig {
		public FourSides Margins;
		public FourSides Padding;
		public FourSides Position;
	}
	public LayoutConfig Layout;

	public Component[] Construction;

	public class Component {

	}

	public class Components {
		public class Image : Component {
			public Color Color;
			public Sprite Sprite;
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

			public Button() : this(
				true,
				Config.UI.Visual.BackgroundColor,
				Config.UI.Visual.ButtonHoverColor,
				Config.UI.Visual.ButtonPressedColor,
				Config.UI.Visual.ButtonDisabledColor,
				new()) { }

			public Button(List<UnityEngine.Events.UnityAction> onClick) : this(
				true,
				Config.UI.Visual.BackgroundColor,
				Config.UI.Visual.ButtonHoverColor,
				Config.UI.Visual.ButtonPressedColor,
				Config.UI.Visual.ButtonDisabledColor,
				onClick) { }
		}

		public class Text : Component {
			public string Content;
			public TMP_FontAsset Font;
			public FontStyles Style;
			public int FontSize;
			public Color Color;
			public TextAlignmentOptions Alignment;

			public Text(
					string content, 
					TMP_FontAsset font, 
					FontStyles style, 
					int fontSize, 
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
		}

		public class Dropdown : Component {
			
		}
	}

	private WindowItem(LayoutConfig layout, Component[] components) {
		Layout = layout;
		Construction = components;
	}

	public static WindowItem Image(Components.Image image, LayoutConfig layout) =>
		new(
			layout,
			new[] { image }
		);

	public static WindowItem Text(Components.Text text, LayoutConfig layout) =>
		new(
			layout,
			new[] { text }
		);

	public static WindowItem Button(Components.Image image, Components.Button button, LayoutConfig layout) =>
		new(
			layout,
			new Component[] { image, button }
		);
}