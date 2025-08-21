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
	
	static BottomBar() {
		OnOutputsOpened = null;
		OnTransformOpened = null;
		OnMaterialOpened = null;
		OnAssemble = null;
	}
	public static event Action OnOutputsOpened;
	public static event Action OnTransformOpened;
	public static event Action OnMaterialOpened;
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
						WindowItem.NewButtonCustomText(
							new PComponents.Button(SaveLoadMenus.Save),
							new PComponents.Text(
								"Save",
								alignment: TextAlignmentOptions.Right
							),
							WindowItem.LayoutConfig.DynamicLayout(
								margin: new(0, splitspacing / 2, 0, 0),
								padding: new(splittextspace, 0),
								position: new FourSides(0,.5f,0,0))
						),
						WindowItem.NewButtonCustomText(
							new PComponents.Button(SaveLoadMenus.SaveAs),
							new PComponents.Text(
								"As",
								alignment: TextAlignmentOptions.Left
							),
							WindowItem.LayoutConfig.DynamicLayout(
								margin: new(0, 0, 0, splitspacing / 2),
								padding: new(splittextspace, 0),
								position: new FourSides(0,0,0,.5f))
						)
					}
			)),
			new W.CustomItem(
				WindowItem.NewEmpty( // use empty iinstead of layout for perf
					PMenu.WindowItemLayout(200),
					new() {
						WindowItem.NewButtonCustomText(
							new PComponents.Button(SaveLoadMenus.ShowLoadMenu),
							new PComponents.Text(
								"Load",
								alignment: TextAlignmentOptions.Right
							),
							WindowItem.LayoutConfig.DynamicLayout(
								margin: new(0, splitspacing / 2, 0, 0),
								padding: new(splittextspace, 0),
								position: new FourSides(0,.5f,0,0))
						),
						WindowItem.NewButtonCustomText(
							new PComponents.Button(null),
							new PComponents.Text(
								"Recent",
								alignment: TextAlignmentOptions.Left
							),
							WindowItem.LayoutConfig.DynamicLayout(
								margin: new(0, 0, 0, splitspacing / 2),
								padding: new(splittextspace, 0),
								position: new FourSides(0,0,0,.5f))
						)
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
UIBarUtils.DynamicBarFlyout	(1, "File", FileMenu.CWindow, (true, true)),
UIBarUtils.DynamicBarFlyout	(1, "Tools", ToolsMenu.CWindow, (true, true)),
UIBarUtils.DynamicBarSpace	(2),
UIBarUtils.DynamicBarText	(5, "name", .5f),
UIBarUtils.DynamicBarSpace	(2),
UIBarUtils.DynamicBarButton	(2, "Assemble", Assemble)
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
	public static W[] Menus => new[] {
		FileMenu,
		ToolsMenu
	};
}