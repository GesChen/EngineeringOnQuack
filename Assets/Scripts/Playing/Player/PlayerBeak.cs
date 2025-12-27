using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg = Config.Player.Beak;

public class PlayerBeak : MonoBehaviour {
	public Transform BeakTop;
	public Transform BeakBottom;
	public Transform ItemHoldingArea;

	bool open = false;

	float topAngle;
	float bottomAngle;

	Transform currentlyHeld;
	Transform itemOriginalParent;

	void Update() {
		topAngle = Mathf.Lerp(topAngle, open ? cfg.TopOpenAngle : 0, cfg.OpenSmoothness * Time.deltaTime);
		bottomAngle = Mathf.Lerp(bottomAngle, open ? cfg.BottomOpenAngle : 0, cfg.OpenSmoothness * Time.deltaTime);

		BeakTop.localRotation = Quaternion.Euler(topAngle, 0, 0);
		BeakBottom.localRotation = Quaternion.Euler(bottomAngle, 0, 0);
	}

	internal void Grab(Transform item) {
		currentlyHeld = item;
		itemOriginalParent = item.parent;

		
		item.SetParent(ItemHoldingArea);
		item.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

		open = true;
	}

	internal void Release() {
		currentlyHeld.SetParent(itemOriginalParent);

		open = false;
	}
}