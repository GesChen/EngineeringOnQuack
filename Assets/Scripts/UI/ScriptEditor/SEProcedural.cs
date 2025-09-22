using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

// static for now, can figure out howt o make it into object form later
public static class SEProcedural {
	public static ScriptEditor ScriptEditor;

	static float viewportMargins = 50;

	static void Setup(TimedEventInvoker iv) {
		// set things up from the deepest upwards

		// setup content
		var viewport = ScrollView.GetChild(0);
		var content = viewport.GetChild(0);

		var linenumbers = HF.CreateRectTransform(
			"Line Numbers",
			viewport,
			new(0, 1), new(0, 1),
			new(0, 1),
			Vector2.zero, Vector2.zero,
			new(0, 0)
		);
		var contentmask = HF.CreateRectTransform(
			"Content Mask",
			viewport,
			Vector2.zero, Vector2.one,
			new(.5f, .5f),
			Vector2.zero, Vector2.zero,
			Vector2.zero
		);
		var contentparent = HF.CreateRectTransform(
			"Content Parent",
			content,
			new(0, 1), new(0, 1),
			new(0, 1),
			Vector2.zero, Vector2.zero,
			Vector2.zero
		);

		var lnvlg = setupvlg(linenumbers);

		contentmask.anchoredPosition = Vector2.zero;
		contentmask.gameObject.AddComponent<RectMask2D>();
		content.SetParent(contentmask);

		Object.Destroy(content.GetComponent<ScaleToContents>());
		content.gameObject.AddComponent<ScaleToTarget>().target = contentparent;

		var contentrt = content.GetComponent<RectTransform>();
		contentrt.anchorMin = new(0, 1);
		contentrt.anchorMax = new(0, 1);
		contentrt.pivot = new(0, 1);
		contentrt.localPosition = Vector2.zero;

		// destroy the temporary empty object
		Object.Destroy(content.transform.GetChild(0).gameObject);

		setupvlg(contentparent);

		// setup main object
		GameObject g = iv.gameObject;

		ScriptEditor = g.AddComponent<ScriptEditor>();
		var sh = g.AddComponent<SyntaxHighlighter>();
		var history = g.AddComponent<LazyHistory>(); // interchangable with history if fix it
		history.SE = ScriptEditor;

		Object.Destroy(ScrollView.GetComponent<Image>());
		ScriptEditor.scroll = ScrollView.GetComponent<ScrollRect>(); // returns betterscrollrect hopefully
		ScriptEditor.contentParent = contentparent;
		ScriptEditor.contentMask = contentmask;
		ScriptEditor.lineNumbersVerticalLayout = lnvlg;
		ScriptEditor.syntaxHighlighter = sh;
		ScriptEditor.history = history;

		ScriptEditor.OnDragStateChanged += DragStateChanged;
	}

	static void DragStateChanged(bool state) {
		SEWindow.RealisedWindow.Config.Movable = !state;
		SEWindow.RealisedWindow.dragging = false;
	}

	private static VerticalLayoutGroup setupvlg(RectTransform rt) {
		var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();

		vlg.childControlWidth = false;
		vlg.childControlHeight = false;
		vlg.childScaleWidth = false;
		vlg.childScaleHeight = false;
		vlg.childForceExpandWidth = false;
		vlg.childForceExpandHeight = false;

		var csf = rt.gameObject.AddComponent<ContentSizeFitter>();
		csf.horizontalFit = ContentSizeFitter.FitMode.MinSize;
		csf.verticalFit = ContentSizeFitter.FitMode.MinSize;

		return vlg;
	}

	static RectTransform ScrollView;

	static CWindow SEWindow;
	static void SetSEWindow() {
		SEWindow = new() {
			Name = "ScriptEditor",
			Config = new() {
				Resizable = true,
				Movable = true,
				Size = CWindow.Configuration.FreeSize(new(1000, 500)),
				HideOnStart = false,
			},
			Items = new WindowItem[] {
				WindowItem.NewImage(
					"Background",
					new PComponents.Image(Config.UI.Visual.BackgroundColor),
					WindowItem.LayoutConfig.FillLayout
				),
				// this thing gets heavily modified in setup lol most of the work is actually done there
				WindowItem.NewScrollView(
					new PComponents.ScrollView(),
					WindowItem.LayoutConfig.DynamicLayout(
						FourSides.Even(viewportMargins)
					),
					new() { WindowItem.NewEmpty(WindowItem.LayoutConfig.FillLayout) } // just to not trigger the warning
				).OnRealized((rt, _) => ScrollView = rt)
			},
			CustomEvents = new() {
				new(TimedEventInvoker.Timing.Awake, Setup)
			}
		};
	}

	public static void Set() {
		SetSEWindow();
	}
	public static CWindow[] Windows => new[] {
		SEWindow
	};
}