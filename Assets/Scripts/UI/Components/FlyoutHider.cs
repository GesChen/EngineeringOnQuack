using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// really dumbass niche class that does one thing and that i might get rid of too
// cuz its so fuckin niche idk
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
						trigger.targetFlyout.Hide();
					}
				}
			}
		}
	}
}