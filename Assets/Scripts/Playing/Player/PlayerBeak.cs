using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBeak : MonoBehaviour {
	public PlayerController controller;

	public Transform BeakTop;
	public Transform BeakBottom;
	public float TopOpenAngle;
	public float BottomOpenAngle;
	public float OpenSmoothness;
	public Transform ItemHoldingArea;

	bool open = false;

	float topAngle;
	float bottomAngle;

	Transform currentlyHeld;
	Transform itemOriginalParent;

	void Update() {
		topAngle = Mathf.Lerp(topAngle, open ? TopOpenAngle : 0, OpenSmoothness * Time.deltaTime);
		bottomAngle = Mathf.Lerp(bottomAngle, open ? BottomOpenAngle : 0, OpenSmoothness * Time.deltaTime);

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