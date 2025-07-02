using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using W = PMenu.Window;

public static class BottomBar {
	static readonly float size = 30;

	public static event Action OnTransformOpened;
	public static event Action OnMaterialOpened;

	public static void ClearTransform() { OnTransformOpened = null; }
	public static void ClearMaterial() { OnMaterialOpened = null; }

	public static event Action OnSave;
	public static void ClearSave() { OnSave = null; }
	public static event Action OnSaveAs;
	public static void ClearSaveAs() { OnSaveAs = null; }
	

	public static void ShowNamePrompt(Action<string> nameCallback) {
		NamePrompt.CWindow.RealisedWindow.Show();

		var canvas = NamePrompt.CWindow.RealisedWindow.canvas;
		Vector2 center = canvas.renderingDisplaySize / 2;
		NamePrompt.CWindow.RealisedWindow.SetWorldCorner(center, 4);

		SaveStatusText.text = "";

		OnNameEnterPressed = null;	
		OnNameEnterPressed += () => nameCallback?.Invoke(PromptedName);
	}
	public static void HideNamePrompt() {
		NamePrompt.CWindow.RealisedWindow.Hide();
	}

	public static string PromptedName;
	public static TextMeshProUGUI SaveStatusText;
	public static event Action OnNameEnterPressed;

	static readonly W NamePrompt = new(
		"Name Your Creation!", 220, new(){
			new W.InputField(
				(value) => PromptedName = value,
				"Enter name here..."),
			new W.CustomItem(
				WindowItem.NewButton(
					new PComponents.Button(() => OnNameEnterPressed?.Invoke()),
					PMenu.WindowItemLayout(220)
					).SetSubItems(
					WindowItem.NewText(
						new PComponents.Text(
							"Save!",
							fontSize: Config.UI.Menu.FontSize,
							alignment: TextAlignmentOptions.Center
							),
						WindowItem.LayoutConfig.FillLayout)
					)
				),
			new W.CustomItem(
				WindowItem.NewText(
					"Status text",
					new PComponents.Text(
						"",
						alignment: TextAlignmentOptions.Center),
					PMenu.WindowItemLayout(220))
				).OnRealItemMade(
					(item) => { item.OnRealized(
						(rt) => {
							SaveStatusText = rt.GetComponent<TextMeshProUGUI>();
						});
					})
		},
		showTitle: true,
		isFlyout: false,
		extraSpacing: 5
		);

	static readonly W FileMenu = new(
		"File", 200, new(){
			new W.Button(() => OnSave?.Invoke(),	"Save"), // todo: descriptions? and icons
			new W.Button(() => OnSaveAs?.Invoke(),	"Save As"),
			new W.Button(null, "Load"),
			new W.Button(null, "Load Recent"),
			new W.Button(null, "Insert Assembly"), // ? might keep 
			new W.Button(null, "Reset")
		},
		showTitle: false);

	static readonly W ToolsMenu = new(
		"Tools", 200, new(){
			new W.Button(() => OnTransformOpened?.Invoke(), "Transform"), // todo: descriptions? and icons
			new W.Button(() => OnMaterialOpened?.Invoke(), "Material"), // todo: descriptions? and icons
		},
		showTitle: false);

	static WindowItem BarItem(string label, float width, CWindow target) =>
		WindowItem.NewFlyoutTrigger(
			label,
			new PComponents.FlyoutTrigger(
				target,
				openHorizontally: false,
				openPrioritizingRight: true,
				openPrioritizingUp: true
				),
			WindowItem.LayoutConfig.LayoutElement(new(width, size))
			).SetSubItems(
			WindowItem.NewText(
				new PComponents.Text(
					label,
					alignment: TMPro.TextAlignmentOptions.Center),
				WindowItem.LayoutConfig.FillLayout
				)
			);
	/*.AddEvent(TimedEventInvoker.Timing.Start, // bold on hover
		(source) => { // holy nesting
			var hover = source.gameObject.GetComponent<HoverTarget>();
			var text = source.gameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
			hover.OnHoverStateChange += (state) => {
				text.fontStyle = 
					state 
					? TMPro.FontStyles.Bold 
					: TMPro.FontStyles.Normal;
			};
		});*/

	public static CWindow Bar = new(){
		Name = "Bottom Bar",
		Config = new(){
			Resizable = false,
			Movable = false,
			Size = CWindow.Configuration.FixedSize(new(0, size)),
			Position = new(
				new(0, 0),
				new(1, 0),
				new(.5f, 0),
				new(0, 0)
				),
			Closable = false,
			HideOnStart = false
		},
		Items = new WindowItem[] {
			WindowItem.NewLayout(
				PComponents.Layout.Horizontal.Fixed(
					false,
					true,
					10 // todo
					),
				WindowItem.LayoutConfig.FillLayout,
				new(){
					BarItem("File", 100, FileMenu.CWindow),
					BarItem("Tools", 100, ToolsMenu.CWindow),
				})
		}
	};
	
	public static CWindow[] Windows => new CWindow[] {
		Bar,
		NamePrompt.CWindow,
		FileMenu.CWindow,
		ToolsMenu.CWindow
	};
}