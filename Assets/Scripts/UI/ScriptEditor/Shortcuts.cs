using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Shortcuts : MonoBehaviour
{
	public ScriptEditor scriptEditor;

	public Shortcut[] AllShortcuts;

	void Start() {
		AllShortcuts = new Shortcut[] {
			new(false, scriptEditor.C_Copy, Key.LeftCtrl, Key.C)
		};
	}

	void Update() {
		foreach (var shortcut in AllShortcuts) {
			bool triggered = shortcut.Test();
			if (triggered) break;
		}
	}

	public class Shortcut {
		public (
			bool Ctrl,
			bool Shift,
			bool Alt)
			Modifiers;

		public Key[] Keys;
		public Action TriggeredAction;

		public bool AllowOtherKeys;

		Key[] RemoveModifiers(List<Key> keys) =>
			keys.Where(k => !Conatrols.Keyboard.All.Modifiers.Contains(k)).ToArray();

		public Shortcut(
			bool allowOthers,
			Action action,
			params Key[] neededKeys) {

			Modifiers.Ctrl = neededKeys.Contains(Key.LeftCtrl) || neededKeys.Contains(Key.RightCtrl);
			Modifiers.Shift = neededKeys.Contains(Key.LeftShift) || neededKeys.Contains(Key.RightShift);
			Modifiers.Alt = neededKeys.Contains(Key.LeftAlt) || neededKeys.Contains(Key.RightAlt);

			AllowOtherKeys = allowOthers;

			TriggeredAction = action;

			Keys = RemoveModifiers(neededKeys.ToList());
		}

		public bool Test() {

			int matches = Keys.Count(k => Conatrols.Keyboard.Presses.Contains(k));

			bool allPressed = matches == Keys.Length;
			bool anyOthersPressed = Conatrols.Keyboard.Presses.Count == Keys.Length;

			if (allPressed &&
				(anyOthersPressed || AllowOtherKeys)) {
				TriggeredAction?.Invoke();
				return true;
			}

			return false;
		}
	}
}
