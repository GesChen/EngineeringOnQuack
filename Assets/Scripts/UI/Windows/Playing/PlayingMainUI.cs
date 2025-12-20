using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class PlayingMainUI {

	// might turn ts generic but for now we will have ts
	public static CWindow SitIndicator;

	static void SetSI() {
		SitIndicator = new() {
			Name = "Sit Indicator",
			Config = new() {
				Resizable = false,
				Movable = false,
				Size = CWindow.Configuration.FixedSize(new(50, 50)),
				Closable = false
			},
			Items = new WindowItem[] {
				WindowItem.NewText(
					new PComponents.Text(
						"E",
						alignment: TextAlignmentOptions.Center
						),
					WindowItem.LayoutConfig.FillLayout
					)
			}
		};
	}

	public static CWindow[] Windows => new CWindow[] {
		SitIndicator
	};

	public static void Set() {
		SetSI();
	}
}