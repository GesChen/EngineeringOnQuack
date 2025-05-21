using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TestWindows {
	public static ClassWindow[] Windows = new ClassWindow[] {
		new() {
			Name = "test",
			Config = new() {
				Movable = false,
				Resizable = false,
				Size = ClassWindow.Configuration.FixedSize(new Vector2(300, 100) / 2f),
				DefaultPosition = UIPosition.AnchoredOffset(UIPosition.BottomCenter, new(0, 100))
			},
			Items = new WindowItem[] {
				WindowItem.Image(
					new(MoreColors.Red), 
					new() {
						Position = new(0, 2/3f, 0, 0),
						Margins = new(5)
					}
				),
				WindowItem.Image(
					new(MoreColors.Green),
					new() {
						Position = new(0, 1/3f, 0, 1/3f),
						Margins = new(5)
					}
				),
				WindowItem.Image(
					new(MoreColors.Blue),
					new() {
						Position = new(0, 0, 0, 2/3f),
						Margins = new(5)
					}
				),
			}
		}
	};
}