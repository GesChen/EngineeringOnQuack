using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Linq;

public class Controls : MonoBehaviour {
	public static float clickMaxDist = 5;
	public static float clickMaxTime = .1f;

	public static InputMaster IM;
	public static Keyboard CurrentKeyboard;

	void Awake() {
		IM = new();

		CurrentKeyboard = new();
	}
	void OnEnable() {
		IM ??= new InputMaster();
		IM.Enable();
	}
	void OnDisable() {
		IM.Disable();
	}

	public delegate void MouseMove(Vector2 position);
	public static event MouseMove OnMouseMove;

	public static Vector2 mousePos;
	public static Vector2 lastMousePos;
	void Update() {
		UpdateMouse();

		CurrentKeyboard.Update();
	}
	private void LateUpdate() {
		lastMousePos = mousePos;
	}

	void UpdateMouse() {
		mousePos = Mouse.current.position.value;
		if (lastMousePos != mousePos) {
			//Debug.Log($"moved to {mousePos}");
			OnMouseMove?.Invoke(mousePos);
		}
	}

	#region Shortcut Methods
	public static bool IsUsed(Key key) => Keyboard.Presses.Contains(key);
	public static bool IsPressed(Key key) => Keyboard.Pressed.Contains(key);
	#endregion

	public class Keyboard {

		// different than allpressed, this one is the repeating one
		public static List<Key> Presses;
		public void Update() {
			UpdateKeyboard();
			UpdateKeyHeldTimes();
			UpdateModifiers();
			Presses = GetRepeats().Union(PressedThisFrame).ToList();
		}

		public static List<Key> LastPressed = new();
		public static List<Key> Pressed; // held ones stay held, presses repeats held
		public static List<Key> PressedThisFrame;
		public static List<Key> ReleasedThisFrame;
		public void UpdateKeyboard() {
			Pressed = GetAllPressedKeys();

			// might be kinda slow but idk
			PressedThisFrame = Pressed.Except(LastPressed).ToList();
			ReleasedThisFrame = LastPressed.Except(Pressed).ToList();

			LastPressed = Pressed;
		}

		// do this later if list contains ends up being too slow
		void UpdateHashSets() {

		}

		// dw about speed, its doing like .05ms so 20k fps \_("/)_/
		List<Key> GetAllPressedKeys() {
			List<Key> pressed = new();

			var kb = UnityEngine.InputSystem.Keyboard.current;
			foreach (KeyControl kc in kb.allKeys) {
				if (kc.isPressed) pressed.Add(kc.keyCode);
			}

			return pressed;
		}

		// these two can be optimized away into one timer sort of thing if you want
		Dictionary<Key, float> KeyHeldTimes = new();
		Dictionary<Key, float> KeyLastRepeatTime = new();
		void UpdateKeyHeldTimes() {
			foreach (Key k in PressedThisFrame) {
				KeyHeldTimes.Add(k, Time.time);
				KeyLastRepeatTime.Add(k, Time.time);
			}
			foreach (Key k in ReleasedThisFrame) {
				KeyHeldTimes.Remove(k);
				KeyLastRepeatTime.Remove(k);
			}
		}

		List<Key> GetRepeats() {
			List<Key> keys = new();
			foreach (KeyValuePair<Key, float> keytime in KeyHeldTimes) {
				if (Time.time - keytime.Value > Config.ScriptEditor.RepeatDelayMs / 1000 && // long enough held
					Time.time - KeyLastRepeatTime[keytime.Key] > 1f / Config.ScriptEditor.RepeatRateCPS) { // long enough since last repeat
					keys.Add(keytime.Key);
					KeyLastRepeatTime[keytime.Key] = Time.time;
				}
			}

			return keys;
		}
	
		void UpdateModifiers() {
			// controls.keyboard.modifiers.alt is shorter still
			Modifiers.Ctrl = Pressed.Contains(Key.LeftCtrl) || Pressed.Contains(Key.RightCtrl);
			Modifiers.Shift = Pressed.Contains(Key.LeftShift) || Pressed.Contains(Key.RightShift);
			Modifiers.Alt = Pressed.Contains(Key.LeftAlt) || Pressed.Contains(Key.RightAlt);
		}

		public static class Modifiers {
			public static bool Ctrl { get; internal set; }
			public static bool Shift { get; internal set; }
			public static bool Alt { get; internal set; }
		}
	}
}