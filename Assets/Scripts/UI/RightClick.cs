using System;
using System.Linq;
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

	protected override void Awake() {
		base.Awake();
	}
	void Update() {
		if (Conatrols.Mouse.Right.PressedThisFrame) {
			Click();
		} else 
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

		var window = WindowLookupFunc(ContextManager.Current, out int[] indices);

		if (window != null) {
			// don't optimize this if not needed 
			currentOpen = window.CWindow.RealisedWindow.GetComponent<Flyout>();

			currentOpen.Show(Conatrols.Mouse.Position, true, true);

			if (window.Switchable) {
				window.SwitchingComponent.UpdateActiveState(indices);
			}
		} else {
			Debug.LogWarning($"No right click defined for {ContextManager.Current.Name}");
		}
	}
	public void Hide() {
		currentOpen.Hide();
	}

	PMenu.Window WindowLookupFunc(IContext context, out int[] menuMask) { 
		PMenu.Window window =
			context switch {
				C.InWorld or 
				C.NoSelection or 
				C.SingleSelection or 
				C.MultiSelection or
				C.GroupSelection => RCM.inWorldUniversalMenu,
				_ => null,
			};

		if (window == null) {
			menuMask = null;
			return null;
		}

		menuMask = context switch {
			C.InWorld or C.NoSelection => RCM.UniversalIndices.Default,
			C.SingleSelection => RCM.UniversalIndices.SingleSelection,
			C.MultiSelection => RCM.UniversalIndices.MultiSelection,
			C.GroupSelection gc => GetGroupIndices(gc),
			_ => Enumerable.Range(0, window.Items.Count).ToArray() // default just select everything
		};

		return window;
	}

	/// <summary>
	/// Gets the universal indices for the group contexts
	/// which needs special processing
	/// </summary>
	int[] GetGroupIndices(C.GroupSelection context) {
		switch ((context.AllGroupedParts, context.AllPartsOfOneGroup)) {
			case (false, false):	return RCM.UniversalIndices.AGPF_APOOGF;
			case (true, false):		return RCM.UniversalIndices.AGPT_APOOGF;
			case (false, true):		return RCM.UniversalIndices.AGPF_APOOGT;
			case (true, true):
				if (context.AllGroupPartsSelected)
					return RCM.UniversalIndices.AGPT_APOOGT_AGPST;
				else return RCM.UniversalIndices.AGPT_APOOGT_AGPSF;
		}
	}
}