using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TestWindows {
	public static CWindow[] Windows = new CWindow[] {
		new() {
			Name = "test",
			Config = new() {
				Movable = true,
				Resizable = true,
				Size = CWindow.Configuration.FreeSize(new Vector2(300, 100) * .75f),
				Position = UIPosition.AnchoredOffset(UIPosition.BottomCenter, new(0, 100))
			},
			Items = new WindowItem[] {
				WindowItem.NewLayout(
					"Transform controls",
					WindowItem.Components.Layout.HorizontalFixed(
						5f,
						TextAnchor.UpperLeft,
						true,
						true
					),
					WindowItem.LayoutConfig.DynamicLayout(
						FourSides.Zero,
						FourSides.Even(5),
						FourSides.Zero),
					new() {
						WindowItem.NewButton(
							"Translate",
							new(),
							new() {
								Position = new(0, 2/3f, 0, 0),
								Margins = new(5)
							},
							new WindowItem.Components.Image("Icons/move")
						),
						WindowItem.NewButton(
							"Rotate",
							new(),
							new() {
								Position = new(0, 1/3f, 0, 1/3f),
								Margins = new(5)
							},
							new WindowItem.Components.Image("Icons/rotate")
						),
						WindowItem.NewButton(
							"Scale",
							new(),
							new() {
								Position = new(0, 0, 0, 2/3f),
								Margins = new(5)
							},
							new WindowItem.Components.Image("Icons/scale")
						)
					})
			}
		}
	};
}