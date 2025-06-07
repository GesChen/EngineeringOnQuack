using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformToolsMenu {
	public static PComponents.Button.ClickEvent onTranslatePressed;
	public static PComponents.Button.ClickEvent onRotatePressed;
	public static PComponents.Button.ClickEvent onScalePressed;

	public static CWindow[] Windows = new CWindow[] {
		new() {
			Name = "Transform Tools",
			Config = new() {
				Movable = true,
				Resizable = true,
				Size = CWindow.Configuration.FreeSize(new Vector2(300, 100) * .75f),
				Position = UIPosition.AnchoredOffset(UIPosition.BottomCenter, new(0, 100))
			},
			Items = new WindowItem[] {
				WindowItem.NewLayout(
					"Transform controls",
					PComponents.Layout.Dynamic(5f),
					WindowItem.LayoutConfig.DynamicLayout(
						FourSides.Zero,
						FourSides.Even(5),
						FourSides.Zero
					),
					new() {
						WindowItem.NewButtonCustomImage(
							"Translate",
							new(() => onTranslatePressed?.Invoke()),
							new PComponents.Image(Config.UI.Locations.IconsFolder + "move"),
							new() {
								Position = new(0, 2/3f, 0, 0),
								Margins = new(5)
							}
						),
						WindowItem.NewButtonCustomImage(
							"Rotate",
							new(() => onRotatePressed?.Invoke()),
							new PComponents.Image(Config.UI.Locations.IconsFolder + "rotate"),
							new() {
								Position = new(0, 1/3f, 0, 1/3f),
								Margins = new(5)
							}
						),
						WindowItem.NewButtonCustomImage(
							"Scale",
							new(() => onScalePressed?.Invoke()),
							new PComponents.Image(Config.UI.Locations.IconsFolder + "scale2"),
							new() {
								Position = new(0, 0, 0, 2/3f),
								Margins = new(5)
							}
						)
					})
			}
		}
	};
}