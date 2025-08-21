using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformToolsMenu {
	public static event Action onTranslatePressed;
	public static event Action onRotatePressed;
	public static event Action onScalePressed;

	public static void ClearEvents() {
		onTranslatePressed	= null;
		onRotatePressed		= null;
		onScalePressed		= null;
	}

	public static CWindow MainWindow = new() {
		Name = "Transform Tools",
		Config = new() {
			Movable = true,
			Resizable = true,
			Size = CWindow.Configuration.FreeSize(new Vector2(300, 100) * .75f),
			Position = UIPosition.AnchoredOffset(UIPosition.BottomCenter, new(0, 100)),
			Closable = true,
			HideOnStart = false
		},
		Items = new WindowItem[] {
			WindowItem.NewLayout(
				"Transform controls",
				PComponents.Layout.DynamicAll(5f),
				WindowItem.LayoutConfig.DynamicLayout(
					padding: FourSides.Even(5)
				),
				new() {
					WindowItem.NewButtonCustomImageOverlay(
						"Translate",
						new(() => onTranslatePressed?.Invoke()),
						new PComponents.Image(Config.Locations.IconsFolder + "move"),
						new() {
							Position = new(0, 2/3f, 0, 0),
							Margins = new(5)
						}
					),
					WindowItem.NewButtonCustomImageOverlay(
						"Rotate",
						new(() => onRotatePressed?.Invoke()),
						new PComponents.Image(Config.Locations.IconsFolder + "rotate"),
						new() {
							Position = new(0, 1/3f, 0, 1/3f),
							Margins = new(5)
						}
					),
					WindowItem.NewButtonCustomImageOverlay(
						"Scale",
						new(() => onScalePressed?.Invoke()),
						new PComponents.Image(Config.Locations.IconsFolder + "scale2"),
						new() {
							Position = new(0, 0, 0, 2/3f),
							Margins = new(5)
						}
					)
				})
		}
	};

	public static CWindow[] Windows => new[] {
		MainWindow.SetGroup("tools") 
	};
}