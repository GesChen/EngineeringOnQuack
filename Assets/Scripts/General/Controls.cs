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

	void Awake() {
		IM = new InputMaster();
	}
	void OnEnable() {
		IM ??= new InputMaster();
		IM.Enable();
	}
	void OnDisable() {
		IM.Disable();
	}
	public static Controls GetControls() {
		return FindObjectOfType<Controls>();
	}

	public delegate void MouseMove(Vector2 position);
	public static event MouseMove OnMouseMove;

	public static Vector2 mousePos;
	public static Vector2 lastMousePos;
	void Update() {
		UpdateMouse();
		UpdateKeyboard();
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

	public static List<Key> AllLastPressed = new();
	public static List<Key> AllPressedKeys;
	public static List<Key> AllPressedThisFrame;
	public static List<Key> AllReleasedThisFrame;
	void UpdateKeyboard() {
		AllPressedKeys = GetAllPressedKeys();

		// might be kinda slow but idk
		AllPressedThisFrame = AllPressedKeys.Except(AllLastPressed).ToList();
		AllReleasedThisFrame = AllLastPressed.Except(AllPressedKeys).ToList();

		AllLastPressed = AllPressedKeys;
	}

	// dw about speed, its doing like .05ms so 20k fps \_("/)_/
	List<Key> GetAllPressedKeys() {
		List<Key> pressed = new();

		Keyboard k = Keyboard.current;
		foreach (KeyControl kc in k.allKeys) {
			if (kc.isPressed) pressed.Add(kc.keyCode);
		}

		return pressed;
	}
}