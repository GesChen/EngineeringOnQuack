using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Overriding class to fix horizontal scrolling  and other things in the future
/// </summary>
public class BetterScrollRect : ScrollRect {
	public override void OnScroll(PointerEventData data) {
		var delta = data.scrollDelta;

		// Mouse tilt or touchpad horizontal scroll
		float x = -delta.x;
		float y = delta.y;

		// Shift + vertical wheel ? horizontal scroll
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
			x += y;
			y = 0;
		}

		// Replace the delta with adjusted values
		var adjusted = new PointerEventData(EventSystem.current)
		{
			scrollDelta = new Vector2(x, y),
			position = data.position,
			button = data.button,
			useDragThreshold = data.useDragThreshold
		};

		base.OnScroll(adjusted);
	}
}