using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// this file will probably not be used in final but here for temporary 
// this is being used alot more than im expecting man im ngl
// alr i think we keeping this tbh lmao 
public class WindowManager : Singleton<WindowManager> {
	public List<CWindow> Windows = new();
	public List<PMenu.Window> Menus = new();
	public WindowRealiser Realiser;
	[HideInInspector] public Canvas Canvas;
	[HideInInspector] public RectTransform CanvasRect;

	private RectTransform preview;
	bool previewVisible;
	Vector2 pvPos;
	Vector2 pvSize;

	public bool anyDragging = false;

	[HideInNormalInspector] public string currentlyLoadedCollection;

	CWindow[] pauseWindows;

	// only good way i can think of for now to ensure that the 
	// other awakes are called before init is to just delay this script's 
	// execution order cuz every other method doesn't make sense or this class
	// cant access it
	protected override void Awake() {
		base.Awake();

		// weird domain clearing bug somehow these persist????????
		Windows.Clear();
		Menus.Clear();

		GameManager.Instance.WM_LoadCollection =
			name => {
				currentlyLoadedCollection = name;
				RealiseCollection(name);
			};

		GameManager.Instance.WM_Pause = LoadPauseCollection;
		GameManager.Instance.WM_UnPause = DestroyPauseCollection;

	}

	public void RealiseCollection(string name) => RealiseCollection(ContextWindows.GetCollection(name));

	public void RealiseCollection(ContextWindows.WindowCollection collection) {
		ResetAllMenus();
		Menus.AddRange(collection.Menus);

		DestroyAllWindows();
		RealiseWindows(collection.Windows);
	}

	void LoadPauseCollection() {
		var paused = ContextWindows.GetCollection("paused");

		RealiseWindows(paused.Windows);

		pauseWindows = paused.Windows;
	}

	void DestroyPauseCollection() {
		foreach (var win in pauseWindows)
			DestroyWindow(win);

		pauseWindows = null;
	}

	public void RealiseWindows(params CWindow[] torealise) {
		Windows ??= new();
		foreach (var window in torealise) {
			Realiser.Realise(window);
			Windows.Add(window);
		}
	}
	public void AddMenus(params PMenu.Window[] menus) {
		Menus.AddRange(menus);
	}

	public void DestroyAllWindows() {
		// destroy these for good measure and non grouped
		foreach (var window in Windows) {
			Destroy(window.RealisedWindow.gameObject);
		}
		Windows.Clear();

		// destroy the groups too
		WindowRealiser.Instance.DestroyAllGroupObjects();
	}

	internal void DestroyMenu(PMenu.Window menu) {
		DestroyWindow(menu.CWindow);
	}

	internal void DestroyWindow(CWindow window) {
		int i = Windows.IndexOf(window);

		if (i == -1) {
			Debug.LogWarning($"deletion attempt of cw {window.Name} failed, not registered in windows list");
			return;
		}

		var associatedmenu = Menus.FirstOrDefault(m => m.CWindow == window);
		if (associatedmenu != null) {
			Menus.Remove(associatedmenu);
		}

		Windows.RemoveAt(i);
		Destroy(window.RealisedWindow.gameObject);
	}

	void ResetAllMenus() {
		foreach (var menu in Menus) {
			menu.Reset();
		}
		Menus.Clear();
	}

	void ReSetAllValues(Action[] Sets) {
		foreach (var setter in Sets) {
			setter();
		}
	}

	void Start() {
		Canvas = GetComponent<Canvas>();
		CanvasRect = Canvas.GetComponent<RectTransform>();
		CreatePreviewWindow();
	}

	void CreatePreviewWindow() {
		GameObject newObj = new("Window Preview");
		preview = newObj.AddComponent<RectTransform>();
		Image image = newObj.AddComponent<Image>();
		image.color = Config.UI.Visual.PreviewWindowColor;
		preview.SetParent(Canvas.transform);
		newObj.SetActive(false);
	}

	void Update() {
		anyDragging = Windows.Any(w => w.RealisedWindow.dragging || w.RealisedWindow.anyNodesDragging);

		if (Conatrols.IM.UI.WindowSnap.IsPressed()) {
			if (Conatrols.Mouse.Left.ReleasedThisFrame && beingDragged != null && lowestWindow != null) {
				PerformSnap(beingDragged, lowestWindow, quadrant, center);
			}

			previewVisible = HandleWindowToWindowSnapping();
		} else {
			previewVisible = false;
		}

		UpdatePreview();
	}

	LiveWindow lowestWindow;
	LiveWindow beingDragged;
	int quadrant;
	bool center;
	bool HandleWindowToWindowSnapping() {
		if (!anyDragging) return false;

		lowestWindow = null;
		int lowestHoverIndex = int.MaxValue;
		beingDragged = null;
		foreach (var cw in Windows) {
			var window = cw.RealisedWindow;
			if (!window.dragging) {
				int index = UIHovers.hovers.IndexOf(window.backgroundImage.transform);
				if (index >= 0 && index < lowestHoverIndex) {
					lowestHoverIndex = index;
					lowestWindow = window;
				}
			} else {
				beingDragged = window;
			}
		}
		if (beingDragged == null || lowestWindow == null) return false;

		Vector2 otherSize = lowestWindow.rt.sizeDelta;
		Vector2 otherCenter = lowestWindow.rt.position;

		Vector2 relativePos = Conatrols.Mouse.Position - otherCenter;
		Vector2 UV = relativePos / otherSize;

		// diagonal quadrants
		bool dUL = UV.x < UV.y;
		bool dUR = -UV.x < UV.y;
		quadrant = (dUL, dUR) switch {
			(true, true) => 0, // up
			(false, true) => 1, // right
			(false, false) => 2, // down
			(true, false) => 3 // left
		};

		float centerMargin = .5f *Config.UI.Window.CenterSnapRange;
		center = Mathf.Abs(UV.x) < centerMargin && Mathf.Abs(UV.y) < centerMargin;

		DisplayPreview(beingDragged, lowestWindow, quadrant, center);

		return true;
	}

	void DisplayPreview(LiveWindow target, LiveWindow snapTo, int quadrant, bool center) {
		(Vector2 pos, Vector2 size) loc;
		if (snapTo.Config.ContentDynamic)
			loc = SnapOutside(target, snapTo, quadrant);
		else
			loc = center 
				? SnapInside(snapTo, quadrant)
				: SnapOutside(target, snapTo, quadrant);

		pvPos = loc.pos;
		pvSize = loc.size;
	}

	(Vector2 pos, Vector2 size) SnapInside(LiveWindow snapTo, int quadrant) {
		bool matchHeight = quadrant == 1 || quadrant == 3;

		float oX = snapTo.rt.sizeDelta.x;
		float oY = snapTo.rt.sizeDelta.y;

		Vector2 newSize =
			matchHeight
			? new(oX / 2, oY)
			: new(oX, oY / 2);

		Vector2 newPos =
			snapTo.rt.GetCenter() +
			quadrant switch {
				0 => new(0, oY / 4),
				1 => new(oX / 4, 0),
				2 => new(0, -oY / 4),
				3 => new(-oX / 4, 0),
				_ => new(0, 0)
			};

		return (newPos, newSize);
	}

	(Vector2 pos, Vector2 size) SnapOutside(LiveWindow target, LiveWindow snapTo, int quadrant) {
		bool matchHeight = quadrant == 1 || quadrant == 3;

		float oX = snapTo.rt.sizeDelta.x;
		float oY = snapTo.rt.sizeDelta.y;
		float tX = target.rt.sizeDelta.x;
		float tY = target.rt.sizeDelta.y;

		Vector2 newSize =
			matchHeight
			? new(tX, oY)
			: new(oX, tY);

		Vector2 newPos =
			snapTo.rt.GetCenter() +
			quadrant switch {
				0 => new(0, (oY + tY) / 2),
				1 => new((oX + tX) / 2, 0),
				2 => new(0, -(oY + tY) / 2),
				3 => new(-(oX + tX) / 2, 0),
				_ => new(0, 0)
			};

		return (newPos, newSize);
	}

	void PerformSnap(LiveWindow target, LiveWindow snapTo, int quadrant, bool center) {
		if (center) {
			int opposideQuad = quadrant switch {
				0 => 2,
				1 => 3,
				2 => 0,
				3 => 1,
				_ => -1
			};
			var (pos, size) = SnapInside(snapTo, opposideQuad);

			snapTo.rt.position = pos;
			snapTo.rt.sizeDelta = size;
		}

		target.rt.SetCenter(preview.position);
		target.rt.sizeDelta = preview.sizeDelta;
	}

	void UpdatePreview() {
		preview.gameObject.SetActive(previewVisible);
		if (!previewVisible) return;

		preview.SetAsLastSibling(); // under everything

		preview.position = pvPos;
		preview.sizeDelta = pvSize;
	}
}