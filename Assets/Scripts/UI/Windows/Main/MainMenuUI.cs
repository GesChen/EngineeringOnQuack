using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MainMenuUI {
	public static Action OnNewWorldPressed;
	public static Action OnLoadWorldPressed;
	public static Action OnManageConstructsPressed;
	public static Action OnSettingsPressed;
	public static Action OnExitPressed;

	public static CWindow MenuWindow;
	public static void SetMW() {
		MenuWindow = new() {
			Name = "Main Menu",
			Config = new() {
				Resizable = true,
				Movable = true,
				Color = Config.UI.Visual.PauseTint,
				Size = CWindow.Configuration.FreeSizeMinimum(new(0, 0)),
				Closable = false,
				HideOnStart = false
			},
			Items = new WindowItem[] {
				WindowItem.NewLayout(
					PComponents.Layout.Vertical.Fixed(
						true,
						true,
						Config.UI.Visual.MainMenu.ItemSpacing
					),
					WindowItem.LayoutConfig.DynamicLayout(
						Config.UI.Visual.MainMenu.MainMargins * FourSides.One
					),
					new() {
WindowItem.NewText(
	"Title",
	new PComponents.Text(
		"Engineering On Quack",
		TMPro.TextAlignmentOptions.Center,
		fontSize: Config.UI.Visual.MainMenu.TitleFontSize,
		wrap: true,
		autoSize: true
	),
	WindowItem.LayoutConfig.LayoutElementDynamic()
).AddComponents(new PComponents.LayoutElement(2)),

WindowItem.NewButtonCustomText(
	"New World",
	new PComponents.Button(() => OnNewWorldPressed?.Invoke()),
	new PComponents.Text(
		"New World",
		TMPro.TextAlignmentOptions.Center,
		fontSize: Config.UI.Visual.MainMenu.ButtonsFontSize,
		autoSize: true
	),
	WindowItem.LayoutConfig.LayoutElementDynamic()
).AddComponents(new PComponents.Outline()),

WindowItem.NewButtonCustomText(
	"Load World",
	new PComponents.Button(() => OnLoadWorldPressed?.Invoke()),
	new PComponents.Text(
		"Load World",
		TMPro.TextAlignmentOptions.Center,
		fontSize: Config.UI.Visual.MainMenu.ButtonsFontSize,
		autoSize: true
	),
	WindowItem.LayoutConfig.LayoutElementDynamic()
).AddComponents(new PComponents.Outline()),

WindowItem.NewButtonCustomText(
	"Constructs",
	new PComponents.Button(() => OnManageConstructsPressed?.Invoke()),
	new PComponents.Text(
		"Constructs",
		TMPro.TextAlignmentOptions.Center,
		fontSize: Config.UI.Visual.MainMenu.ButtonsFontSize,
		autoSize: true
	),
	WindowItem.LayoutConfig.LayoutElementDynamic()
).AddComponents(new PComponents.Outline()),

WindowItem.NewButtonCustomText(
	"Settings",
	new PComponents.Button(() => OnSettingsPressed?.Invoke()),
	new PComponents.Text(
		"Settings",
		TMPro.TextAlignmentOptions.Center,
		fontSize: Config.UI.Visual.MainMenu.ButtonsFontSize,
		autoSize: true
	),
	WindowItem.LayoutConfig.LayoutElementDynamic()
).AddComponents(new PComponents.Outline()),

WindowItem.NewButtonCustomText(
	"Exit",
	new PComponents.Button(() => OnExitPressed?.Invoke()),
	new PComponents.Text(
		"Exit",
		TMPro.TextAlignmentOptions.Center,
		fontSize: Config.UI.Visual.MainMenu.ButtonsFontSize,
		autoSize: true
	),
	WindowItem.LayoutConfig.LayoutElementDynamic()
).AddComponents(new PComponents.Outline()),
					}
				)
			},
			CustomEvents = new() {
				new(
					TimedEventInvoker.Timing.Awake,
					source => {
						var lw = source.GetComponent<LiveWindow>();

						// place it on the side
						// scale it to the proper size

						Vector2 size = new(
							Screen.width * Config.UI.Visual.MainMenu.ScreenWidthPercent,
							Screen.height - Config.UI.Visual.MainMenu.PlaceDistanceFromSides * 2
						);

						MenuWindow.Config.Size = CWindow.Configuration.FreeSizeMinimum(size);
						// hopefully this updates the lw and the lw doesnt just have a copy of the cw

						lw.rt.sizeDelta = size;

						// place the corner
						lw.SetWorldCorner(Config.UI.Visual.MainMenu.PlaceDistanceFromSides * Vector2.one, 0);
					}
				)
			}
		};
	}

	public static CWindow[] Windows => new[] {
		MenuWindow
	};

	public static void Set() {
		SetMW();
	}
}