using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TestWindows {
	public static ClassWindow[] Windows = new ClassWindow[] {
		new() {
			Name = "test",
			Configuration = new() {
				Movable = true,
				Resizable = true
			},
			Items = new WindowItem[] {
				WindowItem.Image(new(MoreColors.Maroon), new() {
					Position = new(.1f,.1f,.2f,.2f)
				})
			}
		}
	};
}