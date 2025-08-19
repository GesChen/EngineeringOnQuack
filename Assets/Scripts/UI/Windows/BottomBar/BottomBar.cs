using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using W = PMenu.Window;

public static class BottomBar {
	static readonly float size = 50;

	static readonly float innerpadding = 10;

	static readonly float splitspacing = 5;
	static readonly float splittextspace = 10;

	public static WindowItem OutputButton;
	public static void ClearOutputs() { OnOutputsOpened = null; }
	public static event Action OnOutputsOpened;
	public static void ClearTransform() { OnTransformOpened = null; }
	public static event Action OnTransformOpened;
	public static void ClearMaterial() { OnMaterialOpened = null; }
	public static event Action OnMaterialOpened;

	public static void ClearAssemble() { OnAssemble = null; }
	public static event Action OnAssemble;
	public static void Assemble() { OnAssemble?.Invoke(); }

	static readonly W FileMenu = new(
		"File", 200, new(){
			new W.Button(null,	"New"),
			new W.Button(null,	"Rename"),
			new W.CustomItem(
				WindowItem.NewEmpty( // use empty iinstead of layout for perf
					PMenu.WindowItemLayout(200),
					new() {
						WindowItem.NewButton(
							new PComponents.Button(SaveLoadMenus.Save),
							WindowItem.LayoutConfig.DynamicLayout(
								margin: new(0, splitspacing / 2, 0, 0),
								padding: new(splittextspace, 0),
								position: new FourSides(0,.5f,0,0))
						).SetSubItems(
							WindowItem.NewText(
								new PComponents.Text(
									"Save",
									alignment: TextAlignmentOptions.Right),
								WindowItem.LayoutConfig.FillLayout
							)),
						WindowItem.NewButton(
							new PComponents.Button(SaveLoadMenus.SaveAs),
							WindowItem.LayoutConfig.DynamicLayout(
								margin: new(0, 0, 0, splitspacing / 2),
								padding: new(splittextspace, 0),
								position: new FourSides(0,0,0,.5f))
						).SetSubItems(
							WindowItem.NewText(
								new PComponents.Text(
									"As",
									alignment: TextAlignmentOptions.Left),
								WindowItem.LayoutConfig.FillLayout
							))
					}
			)),
			new W.CustomItem(
				WindowItem.NewEmpty( // use empty iinstead of layout for perf
					PMenu.WindowItemLayout(200),
					new() {
						WindowItem.NewButton(
							new PComponents.Button(SaveLoadMenus.ShowLoadMenu),
							WindowItem.LayoutConfig.DynamicLayout(
								margin: new(0, splitspacing / 2, 0, 0),
								padding: new(splittextspace, 0),
								position: new FourSides(0,.5f,0,0))
						).SetSubItems(
							WindowItem.NewText(
								new PComponents.Text(
									"Load",
									alignment: TextAlignmentOptions.Right),
								WindowItem.LayoutConfig.FillLayout
							)),
						WindowItem.NewButton(
							new PComponents.Button(null),
							WindowItem.LayoutConfig.DynamicLayout(
								margin: new(0, 0, 0, splitspacing / 2),
								padding: new(splittextspace, 0),
								position: new FourSides(0,0,0,.5f))
						).SetSubItems(
							WindowItem.NewText(
								new PComponents.Text(
									"Recent",
									alignment: TextAlignmentOptions.Left),
								WindowItem.LayoutConfig.FillLayout
							))
					}
			)),
			new W.Button(null, "Insert Assembly"), // ? might keep 
		},
		showTitle: false);

	static readonly W ToolsMenu = new(
		"Tools", 200, new(){
			new W.Button(() => OnOutputsOpened?.Invoke(), "Outputs")
				.OnRealItemMade((wi) => OutputButton = wi), // todo: descriptions? and icons
			new W.Button(() => OnTransformOpened?.Invoke(), "Transform"), // todo: descriptions? and icons
			new W.Button(() => OnMaterialOpened?.Invoke(), "Material"), // todo: descriptions? and icons
		},
		showTitle: false);

	static WindowItem DynamicBarFlyout(string label, CWindow target, float width) =>
		WindowItem.NewFlyoutTrigger(
			label,
			new PComponents.FlyoutTrigger(
				target,
				openHorizontally: false,
				openPrioritizingRight: true,
				openPrioritizingUp: true
				),
			WindowItem.LayoutConfig.LayoutElementDynamic()
		).SetSubItems(
		WindowItem.NewText(
			new PComponents.Text(
				label,
				alignment: TextAlignmentOptions.Center),
			WindowItem.LayoutConfig.FillLayout
			)
		).AddComponents(
			new PComponents.LayoutElement(width)
		);
	static WindowItem DynamicBarSpace(float width) =>
		WindowItem.NewEmpty(
			WindowItem.LayoutConfig.LayoutElementDynamic()
		).AddComponents(
			new PComponents.LayoutElement(width)
		);

	static WindowItem DynamicBarText(string text, float width, float bgopacity) =>
		WindowItem.NewImage(
				new PComponents.Image(
					Config.UI.Visual.BackgroundColor *
						new Color(1, 1, 1, bgopacity)
				),
				WindowItem.LayoutConfig.LayoutElementDynamic()
		).SetSubItems(
			WindowItem.NewText(
				new PComponents.Text(
					text,
					alignment: TextAlignmentOptions.Center),
				WindowItem.LayoutConfig.FillLayout
			)
		).AddComponents(
			new PComponents.LayoutElement(width)
		);

	static WindowItem DynamicBarButton(Action target, string label, float width) =>
		WindowItem.NewButton(
			new PComponents.Button(target),
			WindowItem.LayoutConfig.LayoutElementDynamic()
		).SetSubItems(
		WindowItem.NewText(
			new PComponents.Text(
				label,
				alignment: TextAlignmentOptions.Center),
			WindowItem.LayoutConfig.FillLayout
			)
		).AddComponents(
			new PComponents.LayoutElement(width)
		);

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
					true,
					true,
					10
					),
				WindowItem.LayoutConfig.DynamicLayout(
					padding: new(innerpadding)
				),
				new(){
					DynamicBarFlyout("File", FileMenu.CWindow, 1),
					DynamicBarFlyout("Tools", ToolsMenu.CWindow, 1),
					DynamicBarSpace(2),
					DynamicBarText("name", 5, .5f),
					DynamicBarSpace(2),
					DynamicBarButton(Assemble, "Assemble", 2)
				})
		},
		CustomEvents = new() {
			new TimedEventInvoker.TimedEvent(
				TimedEventInvoker.Timing.Awake,
				(_) => {
Bar.RealisedWindow.backgroundImage.enabled = false;
				})
		}
	};
	
	public static CWindow[] Windows => new CWindow[] {
		Bar.SetGroup("bar"),
		FileMenu.CWindow.SetGroup("bar"),
		ToolsMenu.CWindow.SetGroup("bar")
	};
}