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
	float smoothDelta = 0f;

	public IContext ContextAtClick;

	protected override void Awake() {
		base.Awake();
	}

	void Update() {
		smoothDelta = HF.ApproxAvg(smoothDelta, Conatrols.Mouse.Delta.magnitude, 10);

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
		if (smoothDelta > Config.UI.Behaviour.MaxMouseMovementForClick)
			return;

		downPos = Conatrols.Mouse.Position;

		var window = WindowLookupFunc(ContextManager.Current, out var customization);

		if (window != null) {
			ContextAtClick = ContextManager.Current;

			// don't optimize this if not needed 
			currentOpen = window.CWindow.RealisedWindow.GetComponent<Flyout>();

			window.CustomizeIfAble(customization);

			currentOpen.Show(Conatrols.Mouse.Position, 1, false);

		} else {
			Debug.LogWarning($"No right click defined for {ContextManager.Current.GetType().Name}");
		}
	}
	public void Hide() {
		currentOpen.Hide();
	}

	PMenu.Window WindowLookupFunc(
		IContext context,
		out PMenu.Window.CustomizationData customization) { 
		PMenu.Window window =
			context switch {
				C.Editing or 
				C.Editing.NoSelection or 
				C.Editing.SingleSelection or 
				C.Editing.MultiSelection or
				C.Editing.GroupSelection => RCM.inWorldUniversalMenu,
				_ => null,
			};

		if (window == null) {
			customization = null;
			return null;
		}

		int[] indices = context switch {
			C.Editing or C.Editing.NoSelection	=> RCM.Customizations.Indices.Default,
			C.Editing.SingleSelection			=> RCM.Customizations.Indices.SingleSelection,
			C.Editing.MultiSelection			=> RCM.Customizations.Indices.MultiSelection,
			C.Editing.GroupSelection gc			=> GetGroupIndices(gc),
			_ => Enumerable.Range(0, window.Items.Count).ToArray() // default just select everything
		};

		float? width = context switch {
			C.Editing or C.Editing.NoSelection	=> RCM.Customizations.Widths.Default,
			C.Editing.SingleSelection			=> RCM.Customizations.Widths.SingleSelection,
			C.Editing.MultiSelection			=> RCM.Customizations.Widths.MultiSelection,
			C.Editing.GroupSelection gc			=> GetGroupWidths(gc),
			_ => null
		};

		// part extensions (only on ss for now?)
		if (context is C.Editing.SingleSelection or C.Editing.MultiSelection) {
			List<int> selectedBPIDs = new();
			if (context is C.Editing.SingleSelection ss)
				selectedBPIDs = new() { ss.SelectedBasePartID };
			else 
				selectedBPIDs = ((C.Editing.MultiSelection)context).SelectedBasePartIDs.ToList();

			// find part extension(s)
			var pExs = RCM_Extensions.PartExtensions
				.Where(ex =>
					selectedBPIDs.Any(ss =>
						ex.AssociatedBasePartID == ss)
				);

			foreach (var ex in pExs) {
				// add indices
				indices = indices.Concat(ex.NewItems.Select(ni => ni.RCMi)).ToArray();

				// set width
				width = Mathf.Max(ex.Width, width.Value);
			}
		}

		customization = new() {
			Indices = indices,
			Width = width
		};

		return window;
	}

	/// <summary>
	/// Gets the universal indices for the group contexts
	/// which needs special processing
	/// </summary>
	int[] GetGroupIndices(C.Editing.GroupSelection context) {
		switch ((context.AllGroupedParts, context.AllPartsOfOneGroup)) {
			case (false, false):	return RCM.Customizations.Indices.AGPF_APOOGF;
			case (true, false):		return RCM.Customizations.Indices.AGPT_APOOGF;
			case (false, true):		return RCM.Customizations.Indices.AGPF_APOOGT;
			case (true, true):
				if (context.AllGroupPartsSelected)
					return RCM.Customizations.Indices.AGPT_APOOGT_AGPST;
				else return RCM.Customizations.Indices.AGPT_APOOGT_AGPSF;
		}
	}

	// we wet cuz i cant be fucked to figure out a dryer solution atm
	float GetGroupWidths(C.Editing.GroupSelection context) {
		switch ((context.AllGroupedParts, context.AllPartsOfOneGroup)) {
			case (false, false):	return RCM.Customizations.Widths.AGPF_APOOGF;
			case (true, false):		return RCM.Customizations.Widths.AGPT_APOOGF;
			case (false, true):		return RCM.Customizations.Widths.AGPF_APOOGT;
			case (true, true):
				if (context.AllGroupPartsSelected)
					return RCM.Customizations.Widths.AGPT_APOOGT_AGPST;
				else return RCM.Customizations.Widths.AGPT_APOOGT_AGPSF;
		}
	}
}