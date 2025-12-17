using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlayingMainUI {
	public static CWindow testwindow;

	public static void Set() {
		testwindow = new() {
			Name = "test",
			Config = new() {
				HideOnStart = false
			},
		};
	}

	public static CWindow[] Windows => new CWindow[] {
		testwindow
	};
}