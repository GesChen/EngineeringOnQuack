using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using W = PMenu.Window;
using TMPro;

// static for now, can figure out howt o make it into object form later
public static class SEProcedural {
	public static ScriptEditor ScriptEditor;

	static float menuHeight = 30;
	static float viewportMargins = 50;
	static int menuButtonRelWidth = 1;
	static int menuNameRelWidth = 7;

	public static void Show() {
		SEWindow.RealisedWindow.PlaceAtCenter();
		SEWindow.RealisedWindow.Show();
	}
	public static void Hide() {
		SEWindow.RealisedWindow.Hide();
	}

	public static Action OnSetup;
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

		UnityEngine.Object.Destroy(content.GetComponent<ScaleToContents>());
		content.gameObject.AddComponent<ScaleToTarget>().target = contentparent;

		var contentrt = content.GetComponent<RectTransform>();
		contentrt.anchorMin = new(0, 1);
		contentrt.anchorMax = new(0, 1);
		contentrt.pivot = new(0, 1);
		contentrt.localPosition = Vector2.zero;

		// destroy the temporary empty object
		UnityEngine.Object.Destroy(content.transform.GetChild(0).gameObject);

		setupvlg(contentparent);

		// setup main object
		GameObject g = iv.gameObject;

		ScriptEditor = g.AddComponent<ScriptEditor>();
		var sh = g.AddComponent<SyntaxHighlighter>();
		var history = g.AddComponent<LazyHistory>(); // interchangable with history if fix it
		history.SE = ScriptEditor;

		UnityEngine.Object.Destroy(ScrollView.GetComponent<Image>());
		ScriptEditor.scroll = ScrollView.GetComponent<ScrollRect>(); // returns betterscrollrect hopefully
		ScriptEditor.contentParent = contentparent;
		ScriptEditor.contentMask = contentmask;
		ScriptEditor.lineNumbersVerticalLayout = lnvlg;
		ScriptEditor.syntaxHighlighter = sh;
		ScriptEditor.history = history;

		ScriptEditor.OnDragStateChanged += DragStateChanged;

		// subscribe to the close event to update cpu script
		//SEWindow.RealisedWindow.

		OnSetup?.Invoke();
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
	static TMP_InputField FileNameField;

	public static void SetFileName(string name) {
		if (FileNameField != null)
			FileNameField.text = name;
	}

	public static void ClearEvents() {
		OnNewPressed = null;
		OnOpenPressed = null;
		OnSavePressed = null;
		OnSaveAsPressed = null;

		OnUndoPressed = null;
		OnRedoPressed = null;
		OnCutPressed = null;
		OnCopyPressed = null;
		OnPastePressed = null;
		OnDuplicatePressed = null;
	}

	public static Action<string> OnFileNameChanged;

	public static event Action OnNewPressed;
	public static event Action OnOpenPressed;
	public static event Action OnSavePressed;
	public static event Action OnSaveAsPressed;

	static W FileMenu;
	static void SetFileMenu() {
		FileMenu = new(
			"File",
			150,
			true,
			new() {
				new W.Button(
					() => OnNewPressed?.Invoke(),
					"New"
				),
				new W.Button(
					() => OnOpenPressed?.Invoke(),
					"Open"
				),
				new W.Button(
					() => OnSavePressed?.Invoke(),
					"Save"
				),
				new W.Button(
					() => OnSaveAsPressed?.Invoke(),
					"Save As"
				),
			},
			showTitle: false
		);
	}

	public static event Action OnUndoPressed;
	public static event Action OnRedoPressed;
	public static event Action OnCutPressed;
	public static event Action OnCopyPressed;
	public static event Action OnPastePressed;
	public static event Action OnDuplicatePressed;

	static W EditMenu;
	static void SetEditMenu() {
		EditMenu = new(
			"Edit",
			150,
			true,
			new() {
				new W.Button(
					() => OnUndoPressed?.Invoke(),
					"Undo"
				),
				new W.Button(
					() => OnRedoPressed?.Invoke(),
					"Redo"
				),
				new W.Button(
					() => OnCutPressed?.Invoke(),
					"Cut"
				),
				new W.Button(
					() => OnCopyPressed?.Invoke(),
					"Copy"
				),
				new W.Button(
					() => OnPastePressed?.Invoke(),
					"Paste"
				),
				new W.Button(
					() => OnDuplicatePressed?.Invoke(),
					"Duplicate"
				),
			},
			showTitle: false
		);
	}
	

	static CWindow SEWindow;
	static void SetSEWindow() {
		SEWindow = new() {
			Name = "ScriptEditor",
			Config = new() {
				Resizable = true,
				Movable = true,
				Size = CWindow.Configuration.FreeSize(new(1000, 500)),
				HideOnStart = true,
			},
			Items = new WindowItem[] {
				WindowItem.NewImage(
					"Background",
					new PComponents.Image(Config.UI.Visual.BackgroundColor),
					WindowItem.LayoutConfig.FillLayout
				),
				WindowItem.NewLayout(
					PComponents.Layout.Horizontal.Fixed(true, true),
					WindowItem.LayoutConfig.Custom(
						position: new(1, 0, 0, 0),
						sizeDelta: new(0, menuHeight),
						fixedPosition: new() {
							Pivot = UIPosition.TopCenter
						}
					),
					new() {
						WindowItem.NewFlyoutTriggerWithLabel(
							"File",
							new PComponents.FlyoutTrigger(
								FileMenu.CWindow,
								openTargetEdge: 2,
								openAlignment: true
							),
							new PComponents.Text(
								"File",
								alignment: TextAlignmentOptions.Center
							),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						).AddComponents(
							new PComponents.LayoutElement(menuButtonRelWidth)
						),
						WindowItem.NewFlyoutTriggerWithLabel(
							"Edit",
							new PComponents.FlyoutTrigger(
								EditMenu.CWindow,
								openTargetEdge: 2,
								openAlignment: true
							),
							new PComponents.Text(
								"Edit",
								alignment: TextAlignmentOptions.Center
							),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						).AddComponents(
							new PComponents.LayoutElement(menuButtonRelWidth)
						),
						WindowItem.NewInputField(
							new PComponents.InputField(
								null,
								n => OnFileNameChanged?.Invoke(n),
								placeholderText: "File Name",
								alignment: TextAlignmentOptions.Center
							).OnRealised<PComponents.InputField>(c =>
								FileNameField = (TMP_InputField)c
							),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						).AddComponents(
							new PComponents.LayoutElement(menuNameRelWidth)
						)
					}
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
		SetFileMenu();
		SetEditMenu();

		SetSEWindow();
	}
	public static CWindow[] Windows => new[] {
		SEWindow.SetGroup("script editor"),
		FileMenu.CWindow.SetGroup("script editor"),
		EditMenu.CWindow.SetGroup("script editor")
	};
	public static W[] Menus => new[] {
		FileMenu,
		EditMenu
	};
}