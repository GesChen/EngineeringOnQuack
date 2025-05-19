using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WindowRealiser : MonoBehaviour {
	public Canvas canvas;

	public LiveWindow Realise(ClassWindow window) {

		// make new live window
		var (newWindow, windowRT) = 
			MakeNewRT(window.Name, canvas.transform);

		// make background obj
		var (bgRT, _) = MakeNewImageObj("Background", windowRT, window.Configuration.Color);
		SetFull(bgRT);

		// 4 corner nodes
		var (cpObj, cornerParent) =
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

		// content parent
		var (contentObj, contentParent) =
			MakeNewRT("Content", windowRT);
		SetFull(contentParent);

		foreach(var item in window.Items) {
			RealiseItem(item, contentParent);
		}

		LiveWindow component = newWindow.AddComponent<LiveWindow>();
		component.Config = window.Configuration;
		component.backgroundImage = bgRT;
		component.cornerNodes = nodes;

		return component;
	}

	void SetFull(RectTransform rt) {
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
	}

	RectTransform RealiseItem(WindowItem item, RectTransform container) {
		var (newObj, rt) =
			MakeNewRT("Item", container);

		// add components
		if (item.Construction != null)
			foreach (var comp in item.Construction)
				AddComponent(comp, newObj);

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

		if (item.SubItems != null && item.SubItems.Length > 0) {
			// padding
			RectTransform contentsRT = rt;

			if (item.Layout.Padding != null) {
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

	void AddComponent(WindowItem.Component comp, GameObject newObj) {
		switch (comp) {
			case WindowItem.Components.Image im:
				Image image = newObj.AddComponent<Image>();
				image.color = im.Color;
				image.sprite = im.Sprite;
				break;

			case WindowItem.Components.Button bt:
				Button button = newObj.AddComponent<Button>();
				button.interactable = bt.Enabled;
				button.colors = new() {
					normalColor = bt.NormalColor,
					highlightedColor = bt.HighlightedColor,
					pressedColor = bt.PressedColor,
					disabledColor = bt.DisabledColor
				};
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