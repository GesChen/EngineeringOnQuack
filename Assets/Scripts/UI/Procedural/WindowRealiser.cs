using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WindowRealiser : Singleton<WindowRealiser> {
	public Canvas canvas;
	public Group root;

	protected override void Awake() {
		base.Awake();

		root = new("root") {
			Transform = canvas.GetComponent<RectTransform>()
		};
	}

	public class Group {
		public string Name;
		public List<Group> SubGroups = new();
		public List<CWindow> Windows = new();

		public RectTransform Transform;

		public Group(string name) {
			Name = name;
		}

		public Group FindSubGroup(string name) => SubGroups.Find(x => x.Name == name);
		public Group AddSubGroup(string name) {
			var ng = new Group(name);
			SubGroups.Add(ng);
			return ng;
		}
		public void AddWindow(CWindow cw) {
			Windows.Add(cw);
			cw.RealGroup = this;
		}
	}

	void GenerateGroup(CWindow window) {
		var stringpath = window.GroupPath;

		var path = 
			stringpath == null
			? new string[0]
			: stringpath.Split('/').Select(part => part.Trim()).ToArray();

		GenerateGroup(window, root, path);
	}
	void GenerateGroup(CWindow cw, Group current, string[] path) {
		for (int i = 0; i < path.Length; i++) {
			var name = path[i];
			var sub = current.FindSubGroup(name);
			sub ??= current.AddSubGroup(name);
			current = sub;
		}

		current.AddWindow(cw);
	}
	void RealiseGroups() {
		RealiseGroups(root, null);
	}
	void RealiseGroups(Group current, RectTransform parent, int i = 0) {
		if (i > 100) { // safety
			throw new("Some kind of circular group reference, it should not go on this long");
		}

		if (current.Transform == null) {
			// generate new
			var newT = new GameObject(current.Name).AddComponent<RectTransform>();
			newT.SetParent(parent);
			
			newT.anchorMin = Vector2.zero;
			newT.anchorMax = Vector2.one;
			newT.offsetMin = Vector2.zero;
			newT.offsetMax = Vector2.zero;

			current.Transform = newT;
		}

		foreach (var sub in current.SubGroups) {
			RealiseGroups(sub, current.Transform, i + 1);
		}
	}
	public void DestroyAllGroupObjects() {
		// destroying this top layer is enough
		foreach (var group in root.SubGroups) {
			Destroy(group.Transform.gameObject);
		}
	}

	/*recursive version, just a tad worse, cg's refactored above
	 * public void GenerateGroup(CWindow cw, Group current, string[] path, int i = 0) {
		if (i == path.Length) {
			current.AddWindow(cw);
			return;
		}

		var name = path[i];
		var sub = current.FindSubGroup(name);
		sub ??= current.AddSubGroup(name);

		GenerateGroup(cw, sub, path, i + 1);
	}*/

	public LiveWindow Realise(CWindow window) {
		GenerateGroup(window);

		// make new live window
		var (newWindow, windowRT) =
			MakeNewRT(window.Name, canvas.transform);
		windowRT.anchorMin			= window.Config.Position.AnchorMin;
		windowRT.anchorMax			= window.Config.Position.AnchorMax;
		windowRT.pivot				= window.Config.Position.Pivot;
		windowRT.anchoredPosition	= window.Config.Position.Position;
		windowRT.sizeDelta			= window.Config.Size.Default;

		// make background obj
		var (bgRT, bgIM) = MakeNewImageObj("Background", windowRT, window.Config.Color);
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

		// have to manually set the awake variables bc the new item is
		// not active so awake will not be called
		//component.rt = windowRT;
		//component.manager = newWindow.GetComponentInParent<WindowManager>();
		//component.canvas = canvas;
		// nevermind i just forgot to save when i changed to awake lmao

		component.Config = window.Config;
		component.backgroundImage = bgIM;
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

		RealiseGroups();
		windowRT.SetParent(window.RealGroup.Transform);

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
		int i = 0;
		foreach (var pos in positions) {
			var (nodeRT, _) =
				MakeNewImageObj($"node {i++}", cornerParent, Config.UI.Window.CornerNode.Color);

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

		// give flyout triggers their own indicator as the last subitem
		/*
		if (item.Construction.Find(c => c is PComponents.FlyoutTrigger) is PComponents.FlyoutTrigger trigger) {
			// allow null, and just dont add one
			if (trigger.IndicatorImage != null) {
				item.SubItems ??= new();
				item.SubItems.Add(trigger.IndicatorImage);
			}
		}*/ // warning for future: 
		// dont do this adding to the lists shit because
		// the item might be static and thus subitems list persists and 
		// end up adding a whole buncha shit over multiple 
		// iterations to the same static list
		// codes fixed now

		// make sure scrollviews dont parent themselves
		bool isScrollView = item.Construction.Any(c => c is PComponents.ScrollView);
		if (isScrollView) {
			if (item.SubItems.Count == 0) {
				item.SubItems.Add(WindowItem.NewEmpty(WindowItem.LayoutConfig.FillLayout));

				Debug.LogWarning($"No SubItems in ScrollView {item.Name}. A temporary empty has been made in its place, but subitems must be added.");
			}
		}

		if (item.SubItems != null && item.SubItems.Count > 0) {
			// padding

			// layouts have their own padding
			// and items have to be directly inside so no padding object
			bool isLayout = item.Construction.Any(c => c is PComponents.Layout);
			if (isScrollView || item.Layout.Padding != FourSides.Zero && !isLayout) {
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

			// goddamn waste of time of a bug
			if (item.Construction.Find(c => c is PComponents.FlyoutTrigger) is PComponents.FlyoutTrigger trigger) {
				if (trigger.IndicatorImage != null) {
					RealiseItem(trigger.IndicatorImage, contentsRT);
				}
			}
		}
		item.ContentsObject = contentsRT;

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

		} else if (item.Layout.IsCustom) { // do everything that isnt zero
			rt.anchorMin = new(item.Layout.Position.Left, item.Layout.Position.Up);
			rt.anchorMax = new(1 - item.Layout.Position.Right, 1 - item.Layout.Position.Down);

			item.Layout.Margins.SetTransformOffsets(rt);

			if (item.Layout.FixedPosition != null) {
				if (item.Layout.FixedPosition.Pivot != Vector2.zero)
					rt.pivot = item.Layout.FixedPosition.Pivot;
				if (item.Layout.FixedPosition.Position != Vector2.zero)
					rt.anchoredPosition = item.Layout.FixedPosition.Position;
			}

			if (item.Layout.SizeDelta != Vector2.zero)
				rt.sizeDelta = item.Layout.SizeDelta;
		} else {
			// dynamic positioning
			rt.anchorMin = new(item.Layout.Position.Left, item.Layout.Position.Up);
			rt.anchorMax = new(1 - item.Layout.Position.Right, 1 - item.Layout.Position.Down);

			item.Layout.Margins.SetTransformOffsets(rt);
		}

		item.BecomeRealised(rt, item);

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