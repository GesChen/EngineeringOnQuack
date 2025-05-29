using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// will defo come up with a better system for ts later
// temporary solution for now tho
public class RightClick : MonoBehaviour {
	public WindowManager windowManager;

	void Awake() {
		windowManager.RealiseWindows(RightClickMenus.GetWindows());
	}

	void Update() {
		if (Conatrols.Mouse.Right.PressedThisFrame) {
			Click();
		}
	}

	void Click() {

	}
}