using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// really dumbass niche class that does one thing and that i might get rid of too
// cuz its so fuckin niche idk

/// <summary>
/// With HoverTarget, hides all sibling flyouts when this is hovered.
/// Used for lists of buttons or flyouts that need to disable each other.
/// </summary>
[RequireComponent(typeof(HoverTarget))]
public class FlyoutHider : MonoBehaviour {
	HoverTarget hoverTarget;

	void OnEnable() {
		hoverTarget = GetComponent<HoverTarget>();
		hoverTarget.OnHoverStateChange += OnHoverChange;
	}

	void OnDestroy() {
		hoverTarget.OnHoverStateChange -= OnHoverChange;
	}

	void OnHoverChange(bool state) {
		if (state) {
			foreach (Transform child in transform.parent) {
				if (child != transform) {
					if (child.TryGetComponent<FlyoutTrigger>(out var trigger)) {
						if (trigger.targetFlyout == null)
							Debug.LogWarning($"Target flyout not created on {trigger.transform.GetPath()}");
						else
							trigger.targetFlyout.Hide();
					}
				}
			}
		}
	}
}