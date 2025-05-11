using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Shortcuts : MonoBehaviour {
	public static Shortcut[] AllShortcuts;
	
	void Awake() {
		AllShortcuts = new Shortcut[] {
			new("copy",			false, Key.LeftCtrl, Key.C),
			new("cut",			false, Key.LeftCtrl, Key.X),
			new("paste",		false, Key.LeftCtrl, Key.V),
			new("undo",			false, Key.LeftCtrl, Key.Z),
			new("redo",			false, Key.LeftCtrl, Key.Y),
			new("redo_shift",	false, Key.LeftCtrl, Key.LeftShift, Key.Z),
		};
	}

	void Update() {
		foreach (var shortcut in AllShortcuts) {
			bool triggered = shortcut.Test();
			if (triggered) break;
		}
	}

	static Shortcut Get(string name)
		=> AllShortcuts.First(s => s.Name == name);

	public static void SubscribeTo(string name, Shortcut.ShortcutTriggeredEvent action) {
		Get(name).Subscribe(action);
	}

	public class Shortcut {
		public string Name;

		public (
			bool Ctrl,
			bool Shift,
			bool Alt)
			Modifiers;

		public Key[] Keys;
		
		public delegate void ShortcutTriggeredEvent();
		private event ShortcutTriggeredEvent OnShortcutTrigger;

		public bool AllowOtherKeys;

		Key[] RemoveModifiers(List<Key> keys) =>
			keys.Where(k => !Conatrols.Keyboard.All.Modifiers.Contains(k)).ToArray();

		public Shortcut(
			string name,
			bool allowOthers,
			params Key[] neededKeys) {

			Name = name;

			Modifiers.Ctrl = neededKeys.Contains(Key.LeftCtrl) || neededKeys.Contains(Key.RightCtrl);
			Modifiers.Shift = neededKeys.Contains(Key.LeftShift) || neededKeys.Contains(Key.RightShift);
			Modifiers.Alt = neededKeys.Contains(Key.LeftAlt) || neededKeys.Contains(Key.RightAlt);

			AllowOtherKeys = allowOthers;

			Keys = RemoveModifiers(neededKeys.ToList());
		}

		public bool Test() {
			bool modifiersMatch =
				(Modifiers.Ctrl		== Conatrols.Keyboard.Modifiers.Ctrl) &&
				(Modifiers.Shift	== Conatrols.Keyboard.Modifiers.Shift) &&
				(Modifiers.Alt		== Conatrols.Keyboard.Modifiers.Alt);
			if (!modifiersMatch) return false;

			bool allPressed = Keys.All(k => Conatrols.Keyboard.Presses.Contains(k));
			if (!allPressed) return false;

			bool anyOthersPressed = Conatrols.Keyboard.Presses.Any(
				k => !Keys.Contains(k) && 
				!Conatrols.Keyboard.All.Modifiers.Contains(k));
			
			if (anyOthersPressed && !AllowOtherKeys) return false;

			OnShortcutTrigger?.Invoke();
			return true;
		}

		public void Subscribe(ShortcutTriggeredEvent triggeredVoid) {
			OnShortcutTrigger += triggeredVoid;
		}
	}
}
