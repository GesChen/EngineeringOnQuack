using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PauseUI {

	public static Action OnSettingsPressed;

	public static Action Save;
	public static Action SaveAndExit;
	public static Action Exit;

	public static CWindow TintWindow;
	public static void SetTW() {
		TintWindow = new() {
			Name = "Tint",
			Config = new() {
				Resizable = false,
				Movable = false,
				Color = Config.UI.Visual.PauseTint,
				Size = CWindow.Configuration.FixedSize(100000 * Vector2.one), // should b big enough
				Closable = false,
				HideOnStart = false
			}
			// no items
		};
	}

	public static CWindow PauseWindow;
	public static void SetPW() {
		PauseWindow = new() {
			Name = "Pause Menu",
			Config = new() {
				Resizable = true,
				Movable = true,
				Size = CWindow.Configuration.FreeSizeMinimum(new(300, 200)),
				Closable = false,
				HideOnStart = false
			},
			Items = new WindowItem[] {
				WindowItem.NewLayout(
					PComponents.Layout.Vertical.Fixed(true, true),
					WindowItem.LayoutConfig.FillLayout,
					new() {
						WindowItem.NewButtonCustomText(
							"Settings",
							new PComponents.Button(() => OnSettingsPressed?.Invoke()),
							new PComponents.Text(
								"Settings",
								TMPro.TextAlignmentOptions.Center
							),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						).AddComponents(new PComponents.LayoutElement(3)), // need scale 3x fsr
						WindowItem.NewText(
							new PComponents.Text(
								"Paused",
								TMPro.TextAlignmentOptions.Center
							),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						),
						WindowItem.NewLayout(
							PComponents.Layout.Horizontal.Fixed(true, true),
							WindowItem.LayoutConfig.LayoutElementDynamic(),
							new() {
								WindowItem.NewButtonCustomText(
									"Save",
									new PComponents.Button(() => Save?.Invoke()),
									new PComponents.Text(
										"Save",
										TMPro.TextAlignmentOptions.Center
									),
									WindowItem.LayoutConfig.LayoutElementDynamic()
								).AddComponents(new PComponents.LayoutElement(3)),
								WindowItem.NewButtonCustomText(
									"And",
									new PComponents.Button(() => SaveAndExit?.Invoke()),
									new PComponents.Text(
										"And",
										TMPro.TextAlignmentOptions.Center
									),
									WindowItem.LayoutConfig.LayoutElementDynamic()
								).AddComponents(new PComponents.LayoutElement(2)),
								WindowItem.NewButtonCustomText(
									"Exit",
									new PComponents.Button(() => Exit?.Invoke()),
									new PComponents.Text(
										"Exit",
										TMPro.TextAlignmentOptions.Center
									),
									WindowItem.LayoutConfig.LayoutElementDynamic()
								).AddComponents(new PComponents.LayoutElement(3)),

							}
						)
					}
				)
			}
		};
	}

	public static CWindow[] Windows => new[] {
		TintWindow,
		PauseWindow
	};

	public static void Set() {
		SetTW();
		SetPW();
	}
}