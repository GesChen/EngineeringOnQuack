using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BottomBar {
	static float size = 20;

	public static CWindow Bar = new(){
		Name = "Bottom Bar",
		Config = new(){
			Resizable = false,
			Movable = false,
			Size = CWindow.Configuration.FreeSize(Vector2.zero),
			Position = new(
				new(0, 0),
				new(1, 0),
				new(.5f, 0),
				new(0, size)
				)
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
				})
		}
	};
}