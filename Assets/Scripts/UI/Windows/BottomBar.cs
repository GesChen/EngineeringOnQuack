using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottomBar {
	static readonly float size = 30;

	static CWindow temp = new();

	static WindowItem BarItem(string label, float width, CWindow target) =>
		WindowItem.NewFlyoutTrigger(
			label,
			new PComponents.FlyoutTrigger(
				target,
				openHorizontally: false
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
					BarItem("File", 100, temp)
				})
		}
	};

	public static CWindow[] Windows = {
		Bar,
		temp
	};
}