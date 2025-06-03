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
		windowRT.anchorMin = window.Config.Position.AnchorMin;
		windowRT.anchorMax = window.Config.Position.AnchorMax;
		windowRT.anchoredPosition = window.Config.Position.Position;
		windowRT.sizeDelta = window.Config.Size.Default;

		// make background obj
		var (bgRT, _) = MakeNewImageObj("Background", windowRT, window.Config.Color);
		SetFull(bgRT);

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

		window.RealisedWindow = component;

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
			if (item.Layout.Padding != null && !isLayout) {
				var (_, padRT) =
					MakeNewRT("Contents", rt);

				padRT.anchorMin = Vector2.zero;
				padRT.anchorMax = Vector2.one;
				padRT.offsetMin = new(item.Layout.Padding.Left, item.Layout.Padding.Down);
				padRT.offsetMax = new(-item.Layout.Padding.Right, -item.Layout.Padding.Up);

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

			if (item.Layout.Margins != null) {
				rt.offsetMin = new(item.Layout.Margins.Left, item.Layout.Margins.Down);
				rt.offsetMax = new(-item.Layout.Margins.Right, -item.Layout.Margins.Up);
			} else {
				rt.offsetMin = Vector2.zero;
				rt.offsetMax = Vector2.zero;
			}
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
				
				if (im.SpriteResource != null && im.SpriteResource != "") {
					Sprite sprite = Resources.Load<Sprite>(im.SpriteResource);
					image.sprite = sprite;
				}
				break;

			case PComponents.Button bt:
				Button button = newObj.AddComponent<Button>();

				button.interactable = bt.Enabled;
				button.colors = new() {
					normalColor = bt.NormalColor,
					highlightedColor = bt.HighlightedColor,
					selectedColor = bt.NormalColor,
					pressedColor = bt.PressedColor,
					disabledColor = bt.DisabledColor,
					colorMultiplier = 1,
					fadeDuration = Config.UI.Button.FadeDuration
				};

				Navigation navigation = new() {
					mode = Navigation.Mode.None
				};
				button.navigation = navigation;

				button.onClick.AddListener(bt.TriggerClick);

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
				layout.padding = originalItem.Layout.Padding.ToUnityType();

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
				break;

			case PComponents.LayoutElement le:
				var element = newObj.AddComponent<LayoutElement>();
				element.flexibleWidth = le.SizeMultiplier;
				element.flexibleHeight = le.SizeMultiplier;
				break;

			case PComponents.HoverTarget ht:
				var htComp = newObj.AddComponent<HoverTarget>();

				htComp.NormalColor = ht.NormalColor;
				htComp.HoverColor = ht.HoverColor;
				htComp.FadeDuration = ht.FadeDuration;

				break;

			case PComponents.FlyoutTrigger ft:
				var ftComp = newObj.AddComponent<FlyoutTrigger>();

				// find hovertarget component
				var htInstance = newObj.GetComponent<HoverTarget>();
				if (htInstance == null) {
					Debug.LogError("Missing HoverTarget Component on FlyoutTrigger");
					return;
				}

				ftComp.selfHoverTarget = htInstance;
				ftComp.targetCWindow = ft.TargetFlyout;

				// check for image component
				if (!ft.IndicatorImage.Construction.Any(c=>c is PComponents.Image)) {
					Debug.LogError("Flyout trigger Indicator image subitem has no image component!");
					break;
				}

				// make and set indicator image
				var indicatorImage = RealiseItem(ft.IndicatorImage, contentsRT);
				ftComp.openIndicator = indicatorImage.GetComponent<Image>();

				// get the open and closed sprites
				if (ft.openSpriteLocation != null && ft.openSpriteLocation != "") 
					ftComp.openSprite = Resources.Load<Sprite>(ft.openSpriteLocation);
				else 
					Debug.LogError("Flyout trigger missing open sprite location");
				
				if (ft.closedSpriteLocation != null && ft.closedSpriteLocation != "") 
					ftComp.closedSprite = Resources.Load<Sprite>(ft.closedSpriteLocation);
				else
					Debug.LogError("Flyout trigger missing closed sprite location");

				break;

			case PComponents.Description ds:
				var dsComp = newObj.AddComponent<Description>();
				dsComp.Text = ds.Text;
				break;

			case PComponents.FlyoutHider:
				newObj.AddComponent<FlyoutHider>();
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