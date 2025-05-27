using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RightClickTest {
	static CWindow testNew = new() {
		Name = "flyoutetest",
		Config = new() {
			Movable = true,
			Resizable = true,
			Size = CWindow.Configuration.FreeSize(new Vector2(400, 100) * .75f),
			Position = UIPosition.AnchoredOffset(UIPosition.BottomCenter, new(0, 100))
		},
		Items = new WindowItem[] {
			WindowItem.NewLayout(
				"Transform controls",
				WindowItem.Components.Layout.Dynamic(5f),
				WindowItem.LayoutConfig.DynamicLayout(
					FourSides.Zero,
					FourSides.Even(5),
					FourSides.Zero
				),
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
	};


	static CWindow mainTest = new() {
		Name = "Right Click Test",
		Config = new() {
			Resizable = false,
			Movable = true,
			ContentDynamic = true,
			DynamicPadding = FourSides.Even(2)
		},
		Items = new WindowItem[] {
			WindowItem.NewLayout(
				WindowItem.Components.Layout.VerticalDynamic(
					0,
					TextAnchor.UpperLeft
				),
				WindowItem.LayoutConfig.FillLayout,
				new() {
					WindowItem.NewText(
						new(
							"Title",
							TextAlignmentOptions.Center
							),
						WindowItem.LayoutConfig.FixedLayout(
							UIPosition.AnchoredAt(UIPosition.TopLeft),
							new(150, 30),
							new(5)
							)
						),
					WindowItem.NewFlyoutTrigger(
						"menu Item",
						new(
							testNew,
							WindowItem.NewImage(new(),
								WindowItem.LayoutConfig.FixedLayout(
									UIPosition.AnchoredAt(UIPosition.MiddleRight),
									new(20,20)
									)
								)
							),
						WindowItem.LayoutConfig.FixedLayout(
							UIPosition.AnchoredAt(UIPosition.TopLeft),
							new(150, 40),
							new(5)
							)
					).WithSubItems(
						WindowItem.NewImage(
							"Icon",
							new("Icons/structural"),
							WindowItem.LayoutConfig.FixedLayout(
								UIPosition.AnchoredAt(UIPosition.MiddleLeft),
								new(30,30)
							)
						),
						WindowItem.NewText(
							"Label",
							new(
								"Label",
								TextAlignmentOptions.MidlineLeft
							),
							WindowItem.LayoutConfig.DynamicLayout(
								new FourSides(0,0,0,40),
								FourSides.Zero,
								FourSides.Zero
							)
						)
					)
				}
			)
		}
	};

	public static CWindow[] Windows = {
		mainTest,
		testNew
	};
}