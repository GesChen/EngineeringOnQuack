using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C = Contexts;
using RCM = RightClickMenus;

// will defo come up with a better system for ts later
// temporary solution for now tho
public class RightClick : Singleton<RightClick> {
	public WindowManager windowManager;
	[HideInNormalInspector] public Vector2 downPos;
	Flyout currentOpen;

	void Awake() {
		windowManager.RealiseWindows(RCM.GetWindows());
	}

	void Update() {
		if (Conatrols.Mouse.Right.PressedThisFrame) {
			Click();
		}else 
		if (Conatrols.Mouse.Right.Pressed) {
			if (currentOpen != null && currentOpen.gameObject.activeInHierarchy &&
				(Conatrols.Mouse.Position - downPos).sqrMagnitude >
				Config.UI.Behaviour.MaxMouseMovementForClick * Config.UI.Behaviour.MaxMouseMovementForClick)
				Hide();
		}
	}

	void Click() {
		if (Conatrols.Mouse.SmoothDelta.sqrMagnitude >
			Config.UI.Behaviour.MaxMouseMovementForClick * Config.UI.Behaviour.MaxMouseMovementForClick)
			return;

		downPos = Conatrols.Mouse.Position;

		var window = WindowLookupFunc(ContextManager.Current);

		if (window != null) {
			// don't optimize this if not needed 
			currentOpen = window.CWindow.RealisedWindow.GetComponent<Flyout>();

			currentOpen.Show(Conatrols.Mouse.Position);
		}
	}

	MenuUtil.Window WindowLookupFunc(IContext context)
		=> context switch {
			C.InWorld or C.NoSelection => RCM.inworldDefaultPanel,
			C.SingleSelection => RCM.inworldSinglePanel,
			_ => null
		};

	public void Hide() {
		currentOpen.Hide();
	}
}