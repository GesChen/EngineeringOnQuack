using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PComponents {
	public class Component {

	}

	public class Image : Component {
		public Color Color;
		public string SpriteResource;
		public bool PreserveAspect; // explicity set to false only in special cases needed
									// otherwise it makes sense to have it always true

		public Image(Color color, string spriteResourcePath, bool preserveAspect) {
			Color = color;
			SpriteResource = spriteResourcePath;
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
		public bool Enabled = true;
		public Color NormalColor		= Config.UI.Button.DefaultColor;
		public Color HighlightedColor	= Config.UI.Button.HoverColor;
		public Color PressedColor		= Config.UI.Button.PressedColor;
		public Color DisabledColor		= Config.UI.Button.DisabledColor;
		public List<UnityEngine.Events.UnityAction> OnClick = new();

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

		// less efficient full customization
		public Button(
			List<UnityEngine.Events.UnityAction> onClick,
			bool enabled = true,
			Color? normalColor = null,
			Color? highlightedColor = null,
			Color? pressedColor = null,
			Color? disabledColor = null) {

			OnClick = onClick;
			Enabled = enabled;
			NormalColor			= normalColor		?? Config.UI.Button.DefaultColor;
			HighlightedColor	= highlightedColor	?? Config.UI.Button.HoverColor;
			PressedColor		= pressedColor		?? Config.UI.Button.PressedColor;
			DisabledColor		= disabledColor		?? Config.UI.Button.DisabledColor;
		}

		public Button(List<UnityEngine.Events.UnityAction> onClick) {
			OnClick = onClick;
		}

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
		public TMP_FontAsset Font = Config.UI.Visual.DefaultFont;
		public FontStyles Style = FontStyles.Normal;
		public FontWeight Weight = Config.UI.Visual.DefaultWeight;
		public float FontSize = Config.UI.Visual.FontSize;
		public Color Color = Config.UI.Visual.TextColor;
		public TextAlignmentOptions Alignment = TextAlignmentOptions.TopLeft;

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

		public Text(string content) {
			Content = content;
		}

		public Text(string content, Color color) {
			Content = content;
			Color = color;
		}

		public Text(
				string content,
				TextAlignmentOptions alignment) {
			Content = content;
			Alignment = alignment;
		}
	}

	public class Layout : Component {
		public enum Type {
			Vertical,
			Horizontal,
			Dynamic // might add grid if possible?
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
			=> new(
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

		public static Layout Dynamic(
			float spacing)
			=> new(
				Type.Dynamic,
				spacing,
				TextAnchor.MiddleCenter, // this makes no sense either, no differences
				true, // always fill all dimensions, this is really not compatible with dynamic
				true,
				true);
	}

	public class LayoutElement : Component {
		public float SizeMultiplier;
	}

	public class HoverTarget : Component {
		public Color NormalColor	= Config.UI.Button.DefaultColor;
		public Color HoverColor		= Config.UI.Button.HoverColor;
		public float FadeDuration	= Config.UI.Button.FadeDuration;

		public HoverTarget(Color normalColor, Color hoverColor, float fadeDuration) {
			NormalColor = normalColor;
			HoverColor = hoverColor;
			FadeDuration = fadeDuration;
		}
		public HoverTarget(
			Color? normalColor	= null, 
			Color? hoverColor	= null, 
			float? fadeDuration	= null) {
			
			NormalColor		= normalColor	?? Config.UI.Button.DefaultColor;
			HoverColor		= hoverColor	?? Config.UI.Button.HoverColor;
			FadeDuration	= fadeDuration	?? Config.UI.Button.FadeDuration;
		}

		public HoverTarget() { }
	}

	public class FlyoutTrigger : Component {
		public CWindow TargetFlyout;
		public WindowItem IndicatorImage;
		public string openSpriteLocation = Config.UI.Locations.FlyoutTriggerOpenSprite;
		public string closedSpriteLocation = Config.UI.Locations.FlyoutTriggerClosedSprite;

		public FlyoutTrigger(CWindow targetFlyout, WindowItem indicatorImage, string openSpriteLocation, string closedSpriteLocation) {
			TargetFlyout = targetFlyout;
			IndicatorImage = indicatorImage;
			this.openSpriteLocation = openSpriteLocation;
			this.closedSpriteLocation = closedSpriteLocation;
		}

		public FlyoutTrigger(CWindow targetFlyout) {
			TargetFlyout = targetFlyout;
		}

		public FlyoutTrigger(CWindow targetFlyout, WindowItem indicatorImage) {
			TargetFlyout = targetFlyout;
			IndicatorImage = indicatorImage;
		}
	}

	public class Description : Component {
		public string Text;

		public Description(string text) {
			Text = text;
		}
	}

	public class FlyoutHider : Component {
		// this literally just exists to exist
		public FlyoutHider() {

		}
	}
}