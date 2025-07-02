using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WindowRealiser : MonoBehaviour {
	public Canvas canvas;

	public LiveWindow Realise(CWindow window) {


		// make new live window
		var (newWindow, windowRT) =
			MakeNewRT(window.Name, canvas.transform);
		windowRT.anchorMin			= window.Config.Position.AnchorMin;
		windowRT.anchorMax			= window.Config.Position.AnchorMax;
		windowRT.pivot				= window.Config.Position.Pivot;
		windowRT.anchoredPosition	= window.Config.Position.Position;
		windowRT.sizeDelta			= window.Config.Size.Default;

		// make background obj
		var (bgRT, _) = MakeNewImageObj("Background", windowRT, window.Config.Color);
		SetFull(bgRT);

		// add outline
		var outline = bgRT.gameObject.AddComponent<Outline>();
		outline.effectDistance = window.Config.Outline.size * Vector2.one;
		outline.effectColor = window.Config.Outline.color;

		// content parent
		var (_, contentParent) =
			MakeNewRT("Content", windowRT);
		SetFull(contentParent);

		// items
		foreach (var item in window.Items) {
			RealiseItem(item, contentParent);
		}

		// 4 corner nodes
		List<WindowSizeNode> nodes = MakeCornerNodes(windowRT);

		// set up live window component
		var component = newWindow.AddComponent<LiveWindow>();
		component.Config = window.Config;
		component.backgroundImage = bgRT;
		component.cornerNodes = nodes;
		component.contentsContainer = contentParent;

		// flyout if its there
		if (window.Config.IsFlyout)
			newWindow.AddComponent<Flyout>();

		// set up dynamic window
		if (window.Config.ContentDynamic) {
			var scaler = contentParent.gameObject.AddComponent<ScaleToContents>();
			scaler.padding = window.Config.DynamicPadding;

			var mainScale = newWindow.AddComponent<ScaleToTarget>();
			mainScale.target = contentParent;

			contentParent.anchorMin = new(.5f, .5f);
			contentParent.anchorMax = new(.5f, .5f);
		}

		window.SetRealised(component);
		component.Source = window;

		// set up timed events once everything has been set up
		if (window.CustomEvents != null &&  window.CustomEvents.Count > 0) {
			var invoker = newWindow.AddComponent<TimedEventInvoker>();
			invoker.CustomEvents = window.CustomEvents; // calls customawake anyway
		}

		return component;
	}

	private List<WindowSizeNode> MakeCornerNodes(RectTransform windowRT) {
		var (_, cornerParent) =
			MakeNewRT("Corner Nodes", windowRT);
		SetFull(cornerParent);

		WindowSizeNode.Positions[] positions = new[] {
			WindowSizeNode.Positions.TopLeft,
			WindowSizeNode.Positions.TopRight,
			WindowSizeNode.Positions.BottomLeft,
			WindowSizeNode.Positions.BottomRight
		};

		List<WindowSizeNode> nodes = new();
		foreach (var pos in positions) {
			var (nodeRT, _) =
				MakeNewImageObj("node", cornerParent, Config.UI.Window.CornerNode.Color);

			var nodeComp = nodeRT.gameObject.AddComponent<WindowSizeNode>();
			nodeComp.position = pos;

			nodes.Add(nodeComp);
		}

		return nodes;
	}

	void SetFull(RectTransform rt) {
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
	}

	RectTransform RealiseItem(WindowItem item, RectTransform container) {
		var (newObj, rt) =
			MakeNewRT(item.Name, container);

		// some comps need to be made before others
		PutTypeFirst<PComponents.HoverTarget>(ref item.Construction);
		PutTypeFirst<PComponents.Image>(ref item.Construction);

		RectTransform contentsRT = rt;
		if (item.SubItems != null && item.SubItems.Count > 0) {
			// padding

			// layouts have their own padding
			// and items have to be directly inside so no padding object
			bool isLayout = item.Construction.Any(c => c is PComponents.Layout);
			if (item.Layout.Padding != FourSides.Zero && !isLayout) {
				var (_, padRT) =
					MakeNewRT("Contents", rt);

				padRT.anchorMin = Vector2.zero;
				padRT.anchorMax = Vector2.one;
				item.Layout.Padding.SetTransformOffsets(padRT);

				contentsRT = padRT;
			}
			foreach (var subItem in item.SubItems) {
				RealiseItem(subItem, contentsRT);
			}
		}

		// add components
		if (item.Construction != null)
			foreach (var comp in item.Construction)
				AddComponent(comp, newObj, item, contentsRT);

		// position properly
		if (item.Layout.IsFixed) {
			// fixed positioning
			rt.anchorMin			= item.Layout.FixedPosition.AnchorMin;
			rt.anchorMax			= item.Layout.FixedPosition.AnchorMax;
			rt.pivot				= item.Layout.FixedPosition.Pivot;
			rt.anchoredPosition		= item.Layout.FixedPosition.Position;
			rt.sizeDelta			= item.Layout.SizeDelta;

		} else {
			// dynamic positioning
			rt.anchorMin = new(item.Layout.Position.Left, item.Layout.Position.Up);
			rt.anchorMax = new(1 - item.Layout.Position.Right, 1 - item.Layout.Position.Down);

			item.Layout.Margins.SetTransformOffsets(rt);
		}

		item.BecomeRealised(rt);

		// set up customevents for items too 
		if (item.CustomEvents != null && item.CustomEvents.Count > 0) {
			var invoker = newObj.AddComponent<TimedEventInvoker>();
			invoker.CustomEvents = item.CustomEvents; // calls customawake anyway
		}

		return rt;
	}

	void PutTypeFirst<T>(ref List<PComponents.Component> components) where T : PComponents.Component {
		for (int i = 0; i < components.Count; i++) {
			if (components[i] is T ht) {
				// hopefully this shit code works 
				components.RemoveAt(i);
				components.Insert(0, ht);
			}
		}
	}

	void AddComponent(
		PComponents.Component comp, 
		GameObject newObj, 
		WindowItem originalItem, 
		RectTransform contentsRT) {
		switch (comp) {
			case PComponents.Image im:
				Image image = newObj.AddComponent<Image>();
				image.color = im.Color;
				image.preserveAspect = im.PreserveAspect;
				
				if (im.SpriteAsset != null) {
					image.sprite = im.SpriteAsset;
				}
				else if (im.SpriteLocation != null && im.SpriteLocation != "") {
					Sprite sprite = Resources.Load<Sprite>(im.SpriteLocation);
					image.sprite = sprite;
				}

				im.RealComponent = image;
				break;

			case PComponents.Button bt:
				Button button = newObj.AddComponent<Button>();

				button.interactable = bt.Enabled;
				button.colors = (ColorBlock)bt.Colors;

				var bnav = button.navigation;
				bnav.mode = Navigation.Mode.None;

				button.onClick.AddListener(bt.TriggerClick);

				bt.RealComponent = button;
				break;

			case PComponents.Text tx:
				var text = newObj.AddComponent<TextMeshProUGUI>();
				text.text		= tx.Content;
				text.font		= tx.Font;
				text.fontStyle	= tx.Style;
				text.fontWeight	= tx.Weight;
				text.fontSize	= tx.FontSize;
				text.color		= tx.Color;
				text.alignment	= tx.Alignment;

				if (!originalItem.Layout.IsFixed)
					text.margin = originalItem.Layout.Padding.ToTMProType();

				tx.RealComponent = text;
				break;

			case PComponents.InputField fe:
				var field = newObj.AddComponent<TMP_InputField>();
				field.colors = (ColorBlock)fe.Colors;

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
				fe.ContentPadding.SetTransformOffsets(taRT);

				var mask = taRT.gameObject.AddComponent<RectMask2D>();
				mask.padding = (fe.MaskPadding - fe.ContentPadding).ToRectMask2DType();

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
				phtext.fontStyle = fe.Style | FontStyles.Italic;
				phtext.color = fe.PlaceholderColor;
				phtext.fontWeight = fe.Weight;
				phtext.alignment = fe.Alignment;

				phtext.text = fe.PlaceholderText;

				GameObject to = new("Text");
				var trt = to.AddComponent<RectTransform>();
				trt.SetParent(taRT.transform);
				trt.anchorMin = Vector2.zero;
				trt.anchorMax = Vector2.one;
				trt.offsetMin = Vector2.zero;
				trt.offsetMax = Vector2.zero;

				var ttext = to.AddComponent<TextMeshProUGUI>();
				ttext.fontStyle = fe.Style;
				ttext.color = fe.TextColor;
				ttext.fontWeight = fe.Weight;
				ttext.alignment = fe.Alignment;

				field.textViewport = taRT;
				field.textComponent = ttext;
				field.placeholder = phtext;

				field.fontAsset = fe.Font;
				field.pointSize = fe.FontSize;
				field.onValueChanged.AddListener(fe.ValueChanged);

				fe.RealComponent = field;
				break;

			case PComponents.Layout lt:
				HorizontalOrVerticalLayoutGroup layout = null;

				int type = lt.LayoutType switch {
					PComponents.Layout.Type.Horizontal => 0,
					PComponents.Layout.Type.Vertical => 1,
					PComponents.Layout.Type.Dynamic => 2,
					_ => 0
				};

				switch (type) {
					case 0: layout = newObj.AddComponent<HorizontalLayoutGroup>(); break;
					case 1: layout = newObj.AddComponent<VerticalLayoutGroup>(); break;
					case 2: layout = newObj.AddComponent<DynamicLayoutGroup>(); break;
				}

				// basic settings
				layout.spacing = lt.Spacing;
				layout.childAlignment = lt.ItemAlignment;
				layout.padding = (RectOffset)originalItem.Layout.Padding;

				// reset in case it initialized with any trues
				layout.childControlWidth = false;
				layout.childControlHeight = false;
				layout.childScaleWidth = false;
				layout.childScaleHeight = false;
				layout.childForceExpandWidth = false;
				layout.childForceExpandHeight = false;

				// fixed vs dynamic sizing
				if (lt.FixedSize) { // fixed
					
					// match dimension
					if (lt.MatchOtherDimension) {
						if (type == 0 || type == 2) {
							layout.childControlHeight = true;
							layout.childForceExpandHeight = true;
						} else
						if (type == 1) {
							layout.childControlWidth = true;
							layout.childForceExpandWidth = true;
						}
					}

					if (lt.FillDimension) {
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

				lt.RealComponent = layout;
				break;

			case PComponents.LayoutElement le:
				var element = newObj.AddComponent<LayoutElement>();
				element.flexibleWidth = le.SizeMultiplier;
				element.flexibleHeight = le.SizeMultiplier;

				le.RealComponent = element;
				break;

			case PComponents.HoverTarget ht:
				var htComp = newObj.AddComponent<HoverTarget>();

				htComp.Colors = ht.Colors;

				ht.RealComponent = htComp;
				break;

			case PComponents.FlyoutTrigger ft:
				var ftComp = newObj.AddComponent<FlyoutTrigger>();

				ftComp.openHorizontally = ft.OpenHorizontally;
				ftComp.openPrioritizingUp = ft.OpenPrioritizingUp;
				ftComp.openPrioritizingRight = ft.OpenPrioritizingRight;

				// find hovertarget component
				var htInstance = newObj.GetComponent<HoverTarget>();
				if (htInstance == null) {
					Debug.LogError("Missing HoverTarget Component on FlyoutTrigger");
					return;
				}

				ftComp.selfHoverTarget = htInstance;
				ftComp.targetCWindow = ft.TargetFlyout;

				// allow null, and just dont use it
				if (ft.IndicatorImage != null) {
					// check for image component
					if (!ft.IndicatorImage.Construction.Any(c => c is PComponents.Image)) {
						Debug.LogError("Flyout trigger Indicator image subitem has no image component!");
						break;
					}

					// make and set indicator image
					var indicatorImage = RealiseItem(ft.IndicatorImage, contentsRT);
					ftComp.openIndicator = indicatorImage.GetComponent<Image>();

					// get the open and closed sprites
					if (ft.OpenSpriteLocation != null && ft.OpenSpriteLocation != "")
						ftComp.openSprite = Resources.Load<Sprite>(ft.OpenSpriteLocation);
					else
						Debug.LogError("Flyout trigger missing open sprite location");

					if (ft.ClosedSpriteLocation != null && ft.ClosedSpriteLocation != "")
						ftComp.closedSprite = Resources.Load<Sprite>(ft.ClosedSpriteLocation);
					else
						Debug.LogError("Flyout trigger missing closed sprite location");
				}

				ft.RealComponent = ftComp;
				break;

			case PComponents.Description ds:
				var dsComp = newObj.AddComponent<Description>();
				dsComp.Text = ds.Text;

				ds.RealComponent = dsComp;
				break;

			case PComponents.FlyoutHider fh:
				var fhComp = newObj.AddComponent<FlyoutHider>();

				fh.RealComponent = fhComp;
				break;
		}
	}

	(GameObject, RectTransform) MakeNewRT(string name, Transform parent) {
		GameObject newObj = new(name);
		RectTransform rt = newObj.AddComponent<RectTransform>();
		rt.SetParent(parent);
		return (newObj, rt);
	}

	(RectTransform, Image) MakeNewImageObj(string name, Transform parent, Color color) {
		var (newObj, rt) = MakeNewRT(name, parent);
		Image im = newObj.AddComponent<Image>();
		im.color = color;
		return (rt, im);
	}
}