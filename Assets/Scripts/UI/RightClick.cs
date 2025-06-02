using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C = Contexts;
using RCM = RightClickMenus;

// will defo come up with a better system for ts later
// temporary solution for now tho
public class RightClick : MonoBehaviour {
	public WindowManager windowManager;

	void Awake() {
		windowManager.RealiseWindows(RCM.GetWindows());
	}

	void Update() {
		if (Conatrols.Mouse.Right.PressedThisFrame) {
			Click();
		}
	}

	void Click() {
		var window = WindowLookupFunc(ContextManager.Current);

		if (window != null) {
			// don't optimize this if not needed 
			var live = window.CWindow.RealisedWindow.GetComponent<Flyout>();

			live.Show(Conatrols.Mouse.Position);
		}
	}

	Dictionary<Type, MenuUtil.Window> ContextWindowLookup = new(){
		{ typeof(C.InWorld), RightClickMenus.inworldDefaultPanel },
	};

	MenuUtil.Window WindowLookupFunc(IContext context)
		=> context switch {
			C.InWorld or C.NoSelection => RCM.inworldDefaultPanel,
			C.SingleSelection => RCM.inworldSinglePanel,
			_ => null
		};
}