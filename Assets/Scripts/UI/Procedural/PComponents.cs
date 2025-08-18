using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PComponents {
	public abstract class Component {
		public UnityEngine.Component RealComponent;

		/// <summary>
		/// Generalized method for how components add themselves to the
		/// new gameobject upon realization
		/// </summary>
		public abstract void RealiseComponent(
			GameObject newObj,
			WindowItem originalItem);
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

		/// <summary>
		/// blank white image constructor
		/// </summary>
		public Image() : this(
			Color.white,
			null,
			null,
			true) { }

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var image = newObj.AddComponent<UnityEngine.UI.Image>();
			image.color = Color;
			image.preserveAspect = PreserveAspect;

			if (SpriteAsset != null) {
				image.sprite = SpriteAsset;
			} else if (SpriteLocation != null && SpriteLocation != "") {
				Sprite sprite = Resources.Load<Sprite>(SpriteLocation);
				image.sprite = sprite;

				if (sprite == null)
					Debug.LogWarning($"Sprite \"{SpriteLocation}\" was not loaded/found! Image will be null. On Item \"{originalItem.Name}\" RT {newObj.transform.GetPath()}");
			}

			RealComponent = image;
		}
	}

	public class Button : Component {
		public bool Enabled = true;
		public Config.UI.ColorBlock Colors = Config.UI.ColorBlock.DefaultBlock;
		public event Action OnClick;

		public Button(
			bool enabled,
			Config.UI.ColorBlock colors,
			Action onClick) {
			Enabled = enabled;
			Colors = colors;
			OnClick = onClick;
		}

		// less efficient full customization
		public Button(
			Action onClick,
			bool enabled = true,
			Color? normalColor = null,
			Color? highlightedColor = null,
			Color? pressedColor = null,
			Color? disabledColor = null) {

			OnClick = onClick;
			Enabled = enabled;
			Colors = new() {
				NormalColor		= normalColor		?? Config.UI.ColorBlock.DefaultBlock.NormalColor,
				HoverColor		= highlightedColor	?? Config.UI.ColorBlock.DefaultBlock.HoverColor,
				PressedColor	= pressedColor		?? Config.UI.ColorBlock.DefaultBlock.PressedColor,
				DisabledColor	= disabledColor		?? Config.UI.ColorBlock.DefaultBlock.DisabledColor
			};
		}

		public Button(
			Action onClick,
			Config.UI.ColorBlock colors) {
			OnClick = onClick;
			Colors = colors;
		}

		public Button(Action onClick) {
			OnClick = onClick;
		}

		public void TriggerClick() {
			OnClick?.Invoke();
		}

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var button = newObj.AddComponent<UnityEngine.UI.Button>();

			button.interactable = Enabled;
			button.colors = (ColorBlock)Colors;

			var bnav = button.navigation;
			bnav.mode = Navigation.Mode.None;

			button.onClick.AddListener(TriggerClick);

			RealComponent = button;
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

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var text = newObj.AddComponent<TextMeshProUGUI>();

			text.text		= Content;
			text.font		= Font;
			text.fontStyle	= Style;
			text.fontWeight	= Weight;
			text.fontSize	= FontSize;
			text.color		= Color;
			text.alignment	= Alignment;

			if (!originalItem.Layout.IsFixed)
				text.margin = originalItem.Layout.Padding.ToTMProType();

			RealComponent = text;
		}
	}

	public class InputField : Component {
		public Config.UI.ColorBlock Colors		= Config.UI.ColorBlock.DefaultBlock;

		public string			PlaceholderText	= Config.UI.InputField.PlaceholderDefaultText;

		public TMP_FontAsset		Font		= Config.UI.Visual.DefaultFont;
		public FontStyles			Style		= FontStyles.Normal;
		public FontWeight			Weight		= Config.UI.Visual.DefaultWeight;
		public float				FontSize	= Config.UI.Visual.FontSize;
		public TextAlignmentOptions	Alignment	= TextAlignmentOptions.TopLeft;

		public Color TextColor					= Config.UI.Visual.TextColor;
		public Color PlaceholderColor			= Config.UI.Visual.PlaceholderColor;

		public FourSides ContentPadding			= new(10, 5);
		// value independent from content dont worry about their relation
		public FourSides MaskPadding			= new(2);

		public event Action<string> OnValueChanged;

		/// <summary>
		/// its probably best you just look at the source for 
		/// this constructor tbh.
		/// </summary>
		public InputField(
			Action<string>			onValueChanged,
			string					placeholderText = null,
			Color?					textColor = null,
			Color?					placeholderColor = null,
			FourSides?				contentPadding = null,
			FourSides?				maskPadding = null,
			Config.UI.ColorBlock?	colors = null,
			TMP_FontAsset			font = null,
			FontStyles?				style = null,
			FontWeight?				weight = null,
			float?					fontSize = null,
			TextAlignmentOptions?	alignment = null
			) {

			OnValueChanged = onValueChanged;

			PlaceholderText = placeholderText ?? Config.UI.InputField.PlaceholderDefaultText;

			Font				= font != null ? font : Config.UI.Visual.DefaultFont;
			Style				= style				?? FontStyles.Normal;
			Weight				= weight			?? Config.UI.Visual.DefaultWeight;
			FontSize			= fontSize			?? Config.UI.Visual.FontSize;
			Alignment			= alignment			?? TextAlignmentOptions.TopLeft;

			Colors				= colors			?? Config.UI.ColorBlock.DefaultBlock;

			TextColor			= textColor			?? Config.UI.Visual.TextColor;
			PlaceholderColor	= placeholderColor	?? Config.UI.Visual.PlaceholderColor;

			ContentPadding = contentPadding ?? new(10, 5);
			MaskPadding = maskPadding ?? new(2);
		}

		public void ValueChanged(string newValue) {
			OnValueChanged?.Invoke(newValue);
		}

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var field = newObj.AddComponent<TMP_InputField>();
			field.colors = (ColorBlock)Colors;

			var fnav = field.navigation;
			fnav.mode = Navigation.Mode.None;

			// all this can be dryed but iiabdfi
			// make and setup text area
			GameObject textArea = new("Text Area");
			var taRT = textArea.AddComponent<RectTransform>();
			taRT.SetParent(newObj.transform);

			taRT.anchorMin = Vector2.zero;
			taRT.anchorMax = Vector2.one;
			taRT.anchoredPosition = Vector2.zero;
			ContentPadding.SetTransformOffsets(taRT);

			var mask = taRT.gameObject.AddComponent<RectMask2D>();
			mask.padding = (MaskPadding - ContentPadding).ToRectMask2DType();

			// set up texts
			GameObject pho = new("Placeholder");
			var phrt = pho.AddComponent<RectTransform>();
			phrt.SetParent(taRT.transform);
			phrt.anchorMin = Vector2.zero;
			phrt.anchorMax = Vector2.one;
			phrt.offsetMin = Vector2.zero;
			phrt.offsetMax = Vector2.zero;

			phrt.anchoredPosition = Vector2.zero;
			var phtext = pho.AddComponent<TextMeshProUGUI>();
			phtext.fontStyle = Style | FontStyles.Italic;
			phtext.color = PlaceholderColor;
			phtext.fontWeight = Weight;
			phtext.alignment = Alignment;

			phtext.text = PlaceholderText;

			GameObject to = new("Text");
			var trt = to.AddComponent<RectTransform>();
			trt.SetParent(taRT.transform);
			trt.anchorMin = Vector2.zero;
			trt.anchorMax = Vector2.one;
			trt.offsetMin = Vector2.zero;
			trt.offsetMax = Vector2.zero;

			var ttext = to.AddComponent<TextMeshProUGUI>();
			ttext.fontStyle = Style;
			ttext.color = TextColor;
			ttext.fontWeight = Weight;
			ttext.alignment = Alignment;

			field.textViewport = taRT;
			field.textComponent = ttext;
			field.placeholder = phtext;

			field.fontAsset = Font;
			field.pointSize = FontSize;
			field.onValueChanged.AddListener(ValueChanged);

			// caret and small fix
			field.customCaretColor = true;
			field.caretColor = Config.UI.Visual.TextColor;

			field.enabled = false;
			field.enabled = true;

			RealComponent = field;
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

		/// <summary>
		/// <b><u>You probably shouldn't be using this</u></b> constructor if you want 
		/// to make a layout. Use Pcomp.layout.[direction].whatever
		/// </summary>
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
				bool fillOwnAxis,
				bool matchOtherDimension,
				float? spacing				= null,
				TextAnchor? itemAlignment	= null) => 
				new(
					Type,
					spacing,
					itemAlignment,
					fixedSize,
					fillOwnAxis,
					matchOtherDimension);

			public Layout Fixed(
				bool fillOwnAxis,
				bool matchOtherDimension,
				float? spacing				= null,
				TextAnchor? itemAlignment	= null) => 
				new(
					Type,
					spacing,
					itemAlignment,
					true,
					fillOwnAxis,
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
			new(Type.Dynamic,
				spacing,
				TextAnchor.MiddleCenter, // this makes no sense either, no differences
				true, // always fill all dimensions, this is really not compatible with dynamic
				true,
				true);

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			HorizontalOrVerticalLayoutGroup layout = null;

			int type = LayoutType switch {
				Type.Horizontal => 0,
				Type.Vertical => 1,
				Type.Dynamic => 2,
				_ => 0
			};

			switch (type) {
				case 0: layout = newObj.AddComponent<HorizontalLayoutGroup>(); break;
				case 1: layout = newObj.AddComponent<VerticalLayoutGroup>(); break;
				case 2: layout = newObj.AddComponent<DynamicLayoutGroup>(); break;
			}

			// basic settings
			layout.spacing = Spacing;
			layout.childAlignment = ItemAlignment;
			layout.padding = (RectOffset)originalItem.Layout.Padding;

			// reset in case it initialized with any trues
			layout.childControlWidth = false;
			layout.childControlHeight = false;
			layout.childScaleWidth = false;
			layout.childScaleHeight = false;
			layout.childForceExpandWidth = false;
			layout.childForceExpandHeight = false;

			// fixed vs dynamic sizing
			if (FixedSize) { // fixed
					
				// match dimension
				if (MatchOtherDimension) {
					if (type == 0 || type == 2) {
						layout.childControlHeight = true;
						layout.childForceExpandHeight = true;
					} else
					if (type == 1) {
						layout.childControlWidth = true;
						layout.childForceExpandWidth = true;
					}
				}

				if (FillDimension) {
					if (type == 0 || type == 2) {
						layout.childControlWidth = true;
						layout.childForceExpandWidth = true;
					} else
					if (type == 1) {
						layout.childControlHeight = true;
						layout.childForceExpandHeight = true;
					}
				}
			} else { // dynamic

				// keep everything false
				var fitter = newObj.AddComponent<ContentSizeFitter>();
				fitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
				fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
			}

			RealComponent = layout;
		}
	}

	public class LayoutElement : Component {
		public float SizeMultiplier;

		public LayoutElement(float sizeMultiplier) {
			SizeMultiplier = sizeMultiplier;
		}

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var element = newObj.AddComponent<UnityEngine.UI.LayoutElement>();
			element.flexibleWidth = SizeMultiplier;
			element.flexibleHeight = SizeMultiplier;

			RealComponent = element;
		}
	}

	public class HoverTarget : Component {
		public Config.UI.ColorBlock Colors = Config.UI.ColorBlock.DefaultBlock;

		public HoverTarget(
			Color? normalColor	= null, 
			Color? hoverColor	= null, 
			float? fadeDuration	= null) {
			
			Colors = new() { 
				NormalColor		= normalColor	?? Config.UI.ColorBlock.DefaultBlock.NormalColor,
				HoverColor		= hoverColor	?? Config.UI.ColorBlock.DefaultBlock.HoverColor,
				FadeDuration	= fadeDuration	?? Config.UI.ColorBlock.DefaultBlock.FadeDuration,
			};
		}

		public HoverTarget(Config.UI.ColorBlock colors) {
			Colors = colors;
		}

		public HoverTarget() { }

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var htComp = newObj.AddComponent<global::HoverTarget>();

			htComp.Colors = Colors;

			RealComponent = htComp;
		}
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

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var ftComp = newObj.AddComponent<global::FlyoutTrigger>();

			ftComp.openHorizontally			= OpenHorizontally;
			ftComp.openPrioritizingUp		= OpenPrioritizingUp;
			ftComp.openPrioritizingRight	= OpenPrioritizingRight;

			// find hovertarget component
			if (!newObj.TryGetComponent<global::HoverTarget>(out var htInstance)) {
				Debug.LogError("Missing HoverTarget Component on FlyoutTrigger");
				return;
			}

			ftComp.selfHoverTarget = htInstance;
			ftComp.targetCWindow = TargetFlyout;

			// allow null, and just dont use it
			if (IndicatorImage != null) {
				// check for image component
				if (!IndicatorImage.Construction.Any(c => c is Image)) {
					Debug.LogError("Flyout trigger Indicator image subitem has no image component!");
					return;
				}

				// make and set indicator image <- changed
				var indicatorImage = IndicatorImage.RealObject();
				ftComp.openIndicator = indicatorImage.GetComponent<UnityEngine.UI.Image>();

				// get the open and closed sprites
				if (OpenSpriteLocation != null && OpenSpriteLocation != "")
					ftComp.openSprite = Resources.Load<Sprite>(OpenSpriteLocation);
				else
					Debug.LogError("Flyout trigger missing open sprite location");

				if (ClosedSpriteLocation != null && ClosedSpriteLocation != "")
					ftComp.closedSprite = Resources.Load<Sprite>(ClosedSpriteLocation);
				else
					Debug.LogError("Flyout trigger missing closed sprite location");
			}

			RealComponent = ftComp;
		}
	}

	public class Description : Component {
		public string Text;

		public Description(string text) {
			Text = text;
		}

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var dsComp = newObj.AddComponent<global::Description>();
			dsComp.Text = Text;

			RealComponent = dsComp;
		}
	}

	/// <summary>
	/// Hides sibling flyouts when hovered, requires HoverTarget
	/// </summary>
	public class FlyoutHider : Component {
		// this literally just exists to exist
		public FlyoutHider() {

		}

		public override void RealiseComponent(GameObject newObj, WindowItem _) {
			var fhComp = newObj.AddComponent<global::FlyoutHider>();

			RealComponent = fhComp;
		}
	}

	public class ScaleToContents : Component {
		public FourSides Padding;

		public ScaleToContents(FourSides padding) {
			Padding = padding;
		}

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var comp = newObj.AddComponent<global::ScaleToContents>();
			comp.padding = Padding;
			RealComponent = comp;
		}
	}

	public class ScrollView : Component {
		public bool HorizontalScrolling = true;
		public bool VerticalScrolling = true;

		// might change
		public Color Background;
		public float BarSize;
		public Color BarBackgroundColor;
		public Config.UI.ColorBlock BarHandleColorBlock;

		public ScrollView(
			bool horizontalScrolling = true,
			bool verticalScrolling = true,
			Color? background = null,
			float barSize = 20,
			Color? barBackgroundColor = null,
			Config.UI.ColorBlock? barHandleColorBlock = null
			) {
			HorizontalScrolling	= horizontalScrolling;
			VerticalScrolling	= verticalScrolling;
			Background			= background ?? Config.UI.Visual.BackgroundColor;
			BarSize				= barSize;
			BarBackgroundColor	= barBackgroundColor ?? Config.UI.Visual.SecondaryBackgroundColor;
			BarHandleColorBlock	= barHandleColorBlock ?? Config.UI.ColorBlock.DefaultBlock;
		}

		public override void RealiseComponent(GameObject newObj, WindowItem originalItem) {
			var comp = newObj.AddComponent<ScrollRect>();
			var rt = newObj.GetComponent<RectTransform>();

			// this is gonna be a little fuckin complicated :(
			// "little" is an understatement too :(
			// prolly gonna have to redo how the container works entirely too
			// deal with the contents container last i guess

			comp.horizontal = HorizontalScrolling;
			comp.vertical = VerticalScrolling;

			// make both scrollbars
			if (VerticalScrolling) {
				var vertScrollbar = CreateScrollbar(
					rt,
					true,
					1,
					BarSize,
					Vector2.one,
					BarBackgroundColor,
					BarHandleColorBlock);

				comp.verticalScrollbar = vertScrollbar;
				comp.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
				comp.verticalScrollbarSpacing = 0; // may chnage later but best 0
			}

			if (HorizontalScrolling) {
				var horiScrollbar = CreateScrollbar(
					rt,
					false,
					0,
					BarSize,
					Vector2.zero,
					BarBackgroundColor,
					BarHandleColorBlock);
			
				comp.horizontalScrollbar = horiScrollbar;
				comp.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
				comp.horizontalScrollbarSpacing = 0; // may chnage later but best 0
			}

			comp.movementType = ScrollRect.MovementType.Clamped;
			comp.inertia = true;
			comp.decelerationRate = .135f; // defaults
			comp.scrollSensitivity = Config.Input.ScrollSensitivity;
			
			// yeah idk how we're gonna do this ikiab
			// probably just parent the container to a new viewport object i guess 
			GameObject viewportObj = new("Viewport");
			var viewportRT = viewportObj.AddComponent<RectTransform>();
			// most values are set automatically
			viewportRT.SetParent(rt);
			viewportRT.pivot = new(0, 1);
			viewportRT.SetAsFirstSibling();
			viewportObj.AddComponent<RectMask2D>(); // no setup needed

			// best solution i could come up with, hope children follow 
			originalItem.ContentsObject.SetParent(viewportRT);

			comp.viewport = viewportRT;
			comp.content = originalItem.ContentsObject;

			// i hate you so much
			originalItem.ContentsObject.anchorMin = new(0, 1);
			originalItem.ContentsObject.anchorMax = new(1, 1);
			originalItem.ContentsObject.pivot = new(0, 1);
			originalItem.ContentsObject.offsetMin = new(0, 0);
			originalItem.ContentsObject.offsetMax = new(0, 0);

			var scaler = originalItem.ContentsObject.gameObject.AddComponent<global::ScaleToContents>();
			scaler.IgnoreHorizontal = !HorizontalScrolling;
			scaler.IgnoreVertical = !VerticalScrolling;

			RealComponent = comp;
		}

		// unity's scrollbar
		// fucking hell that took a while to write
		private Scrollbar CreateScrollbar(
			RectTransform parent,
			bool vertical,
			float otherAxisAnchor,
			float size,
			Vector2 pivot,
			Color mainColor,
			Config.UI.ColorBlock colors) {

			// make the main object
			GameObject mainObj = new($"Scrollbar {(vertical ? "Vertical" : "Horizontal")}");
			var mainRT = mainObj.AddComponent<RectTransform>();
			mainRT.SetParent(parent);

			// the component will figure out the other axis so just set both
			// apparently not anymore wtf
			int axis = vertical ? 0 : 1;
			Vector2 MAmin = Vector2.zero;
			MAmin[axis] = otherAxisAnchor;
			Vector2 MAmax = Vector2.one;
			MAmax[axis] = otherAxisAnchor;

			mainRT.anchorMin = MAmin;
			mainRT.anchorMax = MAmax;

			mainRT.sizeDelta = size * Vector2.one;
			mainRT.pivot = pivot;

			mainRT.anchoredPosition = Vector2.zero; // for good measure

			var mainImage = mainObj.AddComponent<UnityEngine.UI.Image>();
			mainImage.color = mainColor;

			// make the sliding area
			GameObject slidingAreaObj = new("Sliding Area");
			var slidingRT = slidingAreaObj.AddComponent<RectTransform>();
			slidingRT.SetParent(mainRT);
			slidingRT.anchorMin = Vector2.zero;
			slidingRT.anchorMax = Vector2.one;
			slidingRT.offsetMin = Vector2.zero;
			slidingRT.offsetMax = Vector2.zero;

			// make the handle
			GameObject handleObj = new("Handle");
			var handleRT = handleObj.AddComponent<RectTransform>();
			handleRT.SetParent(slidingRT);
			// anchors are set by the component
			// lets set them to be safe and offsets
			handleRT.anchorMin = Vector2.zero;
			handleRT.anchorMax = Vector2.one;
			handleRT.offsetMin = Vector2.zero;
			handleRT.offsetMax = Vector2.zero;

			var handleImage = handleObj.AddComponent<UnityEngine.UI.Image>();
			handleImage.color = Color.white; // controlled by the colorblock so no need

			// set up the component
			var comp = mainObj.AddComponent<Scrollbar>();
			comp.targetGraphic = handleImage;
			comp.colors = (ColorBlock)colors;
			comp.handleRect = handleRT;
			comp.direction =
				vertical
				? Scrollbar.Direction.BottomToTop
				: Scrollbar.Direction.LeftToRight;
			// rest of the values should be set by the scrollrect

			// return
			return comp;
		}
	}
}