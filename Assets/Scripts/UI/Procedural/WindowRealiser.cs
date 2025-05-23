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
		LiveWindow component = newWindow.AddComponent<LiveWindow>();
		component.Config = window.Config;
		component.backgroundImage = bgRT;
		component.cornerNodes = nodes;

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

		// add components
		if (item.Construction != null)
			foreach (var comp in item.Construction)
				AddComponent(comp, newObj, item);

		// position properly
		rt.anchorMin = new(item.Layout.Position.Left, item.Layout.Position.Up);
		rt.anchorMax = new(1 - item.Layout.Position.Right, 1 - item.Layout.Position.Down);

		if (item.Layout.Margins != null) {
			rt.offsetMin = new(item.Layout.Margins.Left, item.Layout.Margins.Down);
			rt.offsetMax = new(-item.Layout.Margins.Right, -item.Layout.Margins.Up);
		} else {
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
		}

		if (item.SubItems != null && item.SubItems.Count > 0) {
			// padding
			RectTransform contentsRT = rt;

			// layouts have their own padding
			// and items have to be directly inside so no padding object
			bool isLayout = item.Construction.Any(c => c is WindowItem.Components.Layout);
			if (item.Layout.Padding != null && !isLayout) {
				var (_, padRT) =
					MakeNewRT("Contents", rt);

				padRT.offsetMin = new(item.Layout.Padding.Left, item.Layout.Padding.Down);
				padRT.offsetMax = new(-item.Layout.Padding.Right, -item.Layout.Padding.Up);

				contentsRT = padRT;
			}
			foreach (var subItem in item.SubItems) {
				RealiseItem(subItem, contentsRT);
			}
		}

		return rt;
	}

	void AddComponent(WindowItem.Component comp, GameObject newObj, WindowItem originalItem) {
		switch (comp) {
			case WindowItem.Components.Image im:
				Image image = newObj.AddComponent<Image>();
				image.color = im.Color;
				image.preserveAspect = im.PreserveAspect;

				if (im.TextureResource != null && im.TextureResource != "") {
					Object load = Resources.Load(im.TextureResource);
					Texture2D tex = load as Texture2D;
					if (load == null)
						Debug.LogError($"Item at {im.TextureResource} doesn't exist");
					if (tex == null)
						Debug.LogError($"Item at {im.TextureResource} is not a Texture2D, is {load.GetType()}");

					Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
					sprite.name = tex.name;
					image.sprite = sprite;
				}
				break;

			case WindowItem.Components.Button bt:
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

				Button.ButtonClickedEvent buttonClicked = new();
				foreach (var action in bt.OnClick)
					buttonClicked.AddListener(action);
				button.onClick = buttonClicked;

				break;

			case WindowItem.Components.Text tx:
				var text = newObj.AddComponent<TextMeshProUGUI>();
				text.text = tx.Content;
				text.font = tx.Font;
				text.fontStyle = tx.Style;
				text.fontSize = tx.FontSize;
				text.color = tx.Color;
				text.alignment = tx.Alignment;
				break;

			case WindowItem.Components.Layout lt:
				HorizontalOrVerticalLayoutGroup layout;

				bool horizontal = lt.LayoutType == WindowItem.Components.Layout.Type.Horizontal;
				if (horizontal)
					layout = newObj.AddComponent<HorizontalLayoutGroup>();
				else
					layout = newObj.AddComponent<VerticalLayoutGroup>();

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
						if (horizontal) {
							layout.childControlHeight = true;
							layout.childForceExpandHeight = true;
						} else {
							layout.childControlWidth = true;
							layout.childForceExpandWidth = true;
						}
					}

					if (lt.FillDimension) {
						if (horizontal) {
							layout.childControlWidth = true;
							layout.childForceExpandWidth = true;
						} else {
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

			case WindowItem.Components.LayoutElement le:
				var element = newObj.AddComponent<LayoutElement>();
				element.flexibleWidth = le.SizeMultiplier;
				element.flexibleHeight = le.SizeMultiplier;
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