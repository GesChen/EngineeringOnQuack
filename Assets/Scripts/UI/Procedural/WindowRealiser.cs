using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WindowRealiser : Singleton<WindowRealiser> {
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

	public void UpdateWindow(CWindow window) {
		var oldRT = window.RealisedWindow.rt;

		var newWindow = Realise(window);

		var newRT = newWindow.rt;

		newRT.position = oldRT.position;
		newRT.rotation = oldRT.rotation;
		newRT.localScale = oldRT.localScale;
		// any other properties to copy 

		Destroy(oldRT.gameObject);
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

	internal RectTransform RealiseItem(WindowItem item, RectTransform container) {
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

			// give flyout triggers their own indicator as the last subitem
			if (item.Construction.Find(c => c is PComponents.FlyoutTrigger) is PComponents.FlyoutTrigger trigger) {

				// allow null, and just dont add one
				if (trigger.IndicatorImage != null)
					item.SubItems.Add(trigger.IndicatorImage);
			}

			foreach (var subItem in item.SubItems) {
				RealiseItem(subItem, contentsRT);
			}
		}

		// add components
		if (item.Construction != null)
			foreach (var comp in item.Construction)
				comp.RealiseComponent(newObj, item);

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