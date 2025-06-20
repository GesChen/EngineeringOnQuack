using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PComponents {
	public class Component {
		public UnityEngine.Component RealComponent;
	}

	public class Image : Component {
		public Color Color;
		public string SpriteLocation; // will try to use the acutal sprite first
		public Sprite SpriteAsset; // only use the loc if this doesn't exist
		public bool PreserveAspect; // explicity set to false only in special cases needed
									// otherwise it makes sense to have it always true

		public Image(
			Color? color = null,
			string spriteLocation = null,
			Sprite spriteAsset = null,
			bool preserveAspect = true) {
			Color = color ?? Color.white;
			SpriteLocation = spriteLocation;
			SpriteAsset = spriteAsset;
			PreserveAspect = preserveAspect;
		}
		public Image(Color color) : this(
			color,
			null,
			null,
			true) { }

		public Image(string spriteResourcePath) : this(
			Color.white,
			spriteResourcePath,
			null,
			true) { }

		public Image(Sprite spriteAsset) : this(
			Color.white,
			null,
			spriteAsset,
			true) { }

		public Image() : this(
			Color.white,
			null,
			null,
			true) { }
	}

	public class Button : Component {
		public bool Enabled = true;
		public Config.UI.ColorBlock Colors = Config.UI.Visual.DefaultColorBlock;
		public delegate void ClickEvent();
		public event ClickEvent OnClick;

		public Button(
			bool enabled,
			Config.UI.ColorBlock colors,
			ClickEvent onClick) {
			Enabled = enabled;
			Colors = colors;
			OnClick = onClick;
		}

		// less efficient full customization
		public Button(
			ClickEvent onClick,
			bool enabled = true,
			Color? normalColor = null,
			Color? highlightedColor = null,
			Color? pressedColor = null,
			Color? disabledColor = null) {

			OnClick = onClick;
			Enabled = enabled;
			Colors = new() {
				NormalColor		= normalColor		?? Config.UI.Visual.DefaultColorBlock.NormalColor,
				HoverColor		= highlightedColor	?? Config.UI.Visual.DefaultColorBlock.HoverColor,
				PressedColor	= pressedColor		?? Config.UI.Visual.DefaultColorBlock.PressedColor,
				DisabledColor	= disabledColor		?? Config.UI.Visual.DefaultColorBlock.DisabledColor
			};
		}

		public Button(
			ClickEvent onClick,
			Config.UI.ColorBlock colors) {
			OnClick = onClick;
			Colors = colors;
		}

		public Button(ClickEvent onClick) {
			OnClick = onClick;
		}

		public Button() : this(
			true,
			Config.UI.Visual.DefaultColorBlock,
			null) { }

		public void TriggerClick() {
			OnClick?.Invoke();
		}
	}

	public class Text : Component {
		public string				Content;
		public TMP_FontAsset		Font		= Config.UI.Visual.DefaultFont;
		public FontStyles			Style		= FontStyles.Normal;
		public FontWeight			Weight		= Config.UI.Visual.DefaultWeight;
		public float				FontSize	= Config.UI.Visual.FontSize;
		public Color				Color		= Config.UI.Visual.TextColor;
		public TextAlignmentOptions	Alignment	= TextAlignmentOptions.TopLeft;

		public Text(
				string					content,
				TMP_FontAsset			font		= null,
				FontStyles?				style		= null,
				FontWeight?				weight		= null,
				float?					fontSize	= null,
				Color?					color		= null,
				TextAlignmentOptions?	alignment	= null) {
			Content = content;

			Font		= font != null ? font : Config.UI.Visual.DefaultFont;
			Style		= style		?? FontStyles.Normal;
			Weight		= weight	?? Config.UI.Visual.DefaultWeight;
			FontSize	= fontSize	?? Config.UI.Visual.FontSize;
			Color		= color		?? Config.UI.Visual.TextColor;
			Alignment	= alignment	?? TextAlignmentOptions.TopLeft;
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
			float? spacing,
			TextAnchor? itemAlignment,
			bool fixedSize,
			bool fillDimension,
			bool matchOtherDimension) {

			LayoutType				= layoutType;
			Spacing					= spacing ?? Config.UI.Visual.DefaultLayoutSpacing;
			ItemAlignment			= itemAlignment ?? TextAnchor.UpperLeft;
			FixedSize				= fixedSize;
			FillDimension			= fillDimension;
			MatchOtherDimension		= matchOtherDimension;
		}

		public static readonly HorizontalDirection Horizontal = new();
		public static readonly VerticalDirection Vertical = new();

		public abstract class Direction {
			// i would REALLY want this to be 
			public abstract Type Type { get; }
			public Layout Layout(
				bool fixedSize,
				bool fillHorizontally,
				bool matchOtherDimension,
				float? spacing				= null,
				TextAnchor? itemAlignment	= null) => 
				new(
					Type,
					spacing,
					itemAlignment,
					fixedSize,
					fillHorizontally,
					matchOtherDimension);

			public Layout Fixed(
				bool fillHorizontally,
				bool matchOtherDimension,
				float? spacing				= null,
				TextAnchor? itemAlignment	= null) => 
				new(
					Type,
					spacing,
					itemAlignment,
					true,
					fillHorizontally,
					matchOtherDimension);

			public Layout Dynamic(
				float? spacing				= null,
				TextAnchor? itemAlignment	= null) => 
				new(
					Type,
					spacing,
					itemAlignment,
					false,
					false,
					false);
		}

		public class HorizontalDirection : Direction {
			public override Type Type => Type.Horizontal;
		}

		public class VerticalDirection : Direction {
			public override Type Type => Type.Vertical;
		}

		public static Layout DynamicAll(
			float spacing) => 
			new(
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
		public Config.UI.ColorBlock Colors = Config.UI.Visual.DefaultColorBlock;

		public HoverTarget(
			Color? normalColor	= null, 
			Color? hoverColor	= null, 
			float? fadeDuration	= null) {
			
			Colors = new() { 
				NormalColor		= normalColor	?? Config.UI.Visual.DefaultColorBlock.NormalColor,
				HoverColor		= hoverColor	?? Config.UI.Visual.DefaultColorBlock.HoverColor,
				FadeDuration	= fadeDuration	?? Config.UI.Visual.DefaultColorBlock.FadeDuration,
			};
		}

		public HoverTarget(Config.UI.ColorBlock colors) {
			Colors = colors;
		}

		public HoverTarget() { }
	}

	public class FlyoutTrigger : Component {
		public CWindow TargetFlyout;
		public WindowItem IndicatorImage;
		public bool OpenHorizontally;
		public bool OpenPrioritizingRight;
		public bool OpenPrioritizingUp;
		public string OpenSpriteLocation = Config.UI.Locations.FlyoutTriggerOpenSprite;
		public string ClosedSpriteLocation = Config.UI.Locations.FlyoutTriggerClosedSprite;

		public FlyoutTrigger(
			CWindow targetFlyout,
			WindowItem indicatorImage = null,
			bool openHorizontally = true,
			bool openPrioritizingRight = true,
			bool openPrioritizingUp = false,
			string openSpriteLocation = null,
			string closedSpriteLocation = null) {

			TargetFlyout = targetFlyout;
			IndicatorImage = indicatorImage;

			OpenHorizontally = openHorizontally;
			OpenPrioritizingUp = openPrioritizingUp;
			OpenPrioritizingRight = openPrioritizingRight;
			
			OpenSpriteLocation = openSpriteLocation ?? Config.UI.Locations.FlyoutTriggerOpenSprite;
			ClosedSpriteLocation = closedSpriteLocation ?? Config.UI.Locations.FlyoutTriggerClosedSprite;
		}
	}

	public class Description : Component {
		public string Text;

		public Description(string text) {
			Text = text;
		}
	}

	/// <summary>
	/// Hides sibling flyouts when hovered, requires HoverTarget
	/// </summary>
	public class FlyoutHider : Component {
		// this literally just exists to exist
		public FlyoutHider() {

		}
	}
}