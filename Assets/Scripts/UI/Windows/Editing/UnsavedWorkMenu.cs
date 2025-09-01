using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = PMenu.Window;

public static class UnsavedWorkMenu {
	public enum Choice {
		Save,
		Discard,
		Cancel
	}

	static event Action<Choice> OnChoicePressed;
	static void Press(Choice c) {
		OnChoicePressed?.Invoke(c);

		Menu.CWindow.RealisedWindow.Hide();
	}

	public static W Menu;
	public static void SetMenu() {
		Menu = new(
			"Unsaved Changes",
			300, false, 
			new() {
				new W.Text("You have unsaved changes!"),
				new W.Button(
					() => Press(Choice.Save),
					"Save"
				),
				new W.Button(
					() => Press(Choice.Discard),
					"Discard Changes"
				),
				new W.Button(
					() => Press(Choice.Cancel),
					"Cancel"
				)
			},
			showTitle: false,
			movable: true
		);
	}

	public static void Notify(Action<Choice> onChoiceCallback) {
		Menu.CWindow.RealisedWindow.PlaceAtCenter();
		Menu.CWindow.RealisedWindow.Show();

		OnChoicePressed = null;
		OnChoicePressed += onChoiceCallback;
	}

	public static void Set() {
		SetMenu();
	}
	public static CWindow[] Windows => new[] {
		Menu.CWindow.SetGroup("saveload")
	};
	public static W[] Menus => new[] {
		Menu
	};
}