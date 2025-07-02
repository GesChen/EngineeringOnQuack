using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// TODO: OPTIMIZE THIS!!! if it starts causing lag
// testing seems to show very short frame times but you never know once theres a million ui things
// im not dealing with this for now good luck

public class ScaleToContents : MonoBehaviour {
	public FourSides padding;
	private RectTransform rt;
	void Start() {
		rt = GetComponent<RectTransform>();
	}
	void Update() {
		Scale();
	}
	
	void Scale() {
		Vector2 padOffset = new(padding.Left + padding.Right, padding.Up + padding.Down);

		if (transform.childCount == 0) {
			//Debug.LogWarning($"ScaleToContents on {transform.name} has no children. This will cause unintended side effects.");
			rt.sizeDelta = padOffset;
			return;
		}

		Vector2 min = Vector2.positiveInfinity;
		Vector2 max = Vector2.negativeInfinity;

		TestChildrenOf(rt, ref min, ref max);

		Vector2 size = max - min + padOffset;

		rt.sizeDelta = size;

		// fit items into box
		Vector2 bottomLeftWorld = rt.localToWorldMatrix.MultiplyPoint(
			new Vector2(rt.rect.x, rt.rect.y));

		Vector2 offset = min - bottomLeftWorld - new Vector2(padding.Left, padding.Down);
		foreach (Transform child in transform) {
			child.position -= (Vector3)offset;
		}
	}

	void TestChildrenOf(Transform parent,
		ref Vector2 min,
		ref Vector2 max) {
		foreach (Transform child in parent) {
			if (child == parent) continue;

			var crt = child.GetComponent<RectTransform>();
			Vector3[] corners = new Vector3[4];
			crt.GetWorldCorners(corners);

			foreach (Vector3 c in corners) {
				min = Vector2.Min(min, c);
				max = Vector2.Max(max, c);
			}

			bool isMask = child.GetComponent<RectMask2D>();
			bool hasChildren = child.childCount > 0;
			if (!isMask && hasChildren) {
				TestChildrenOf(child, ref min, ref max);
			}
		}
	}

	// doesnt work or something idk
	void FastScale() {
		Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(transform);
		Vector2 size = bounds.size + new Vector3(padding.Left + padding.Right, padding.Up + padding.Down);
		rt.sizeDelta = size;

		Vector2 bottomLeftWorld = rt.localToWorldMatrix.MultiplyPoint(
			new Vector2(rt.rect.x, rt.rect.y));

		Vector2 offset = (Vector2)bounds.center - bottomLeftWorld - new Vector2(padding.Left, padding.Down);
		foreach (Transform child in transform) {
			child.position -= (Vector3)offset;
		}
	}
}