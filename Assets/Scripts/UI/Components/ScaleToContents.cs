using System.Collections.Generic;
using UnityEngine;

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
		Vector2 minPos = Vector2.positiveInfinity;
		Vector2 maxPos = Vector2.negativeInfinity;
		Vector3[] corners = new Vector3[4];
		var children = rt.GetComponentsInChildren<RectTransform>();

		foreach (var child in children) {
			if (child == rt) continue;

			child.GetWorldCorners(corners);

			foreach (Vector3 c in corners) {
				minPos = Vector2.Min(minPos, c);
				maxPos = Vector2.Max(maxPos, c);
			}
		}

		Vector2 size = maxPos - minPos +
			new Vector2(padding.Left + padding.Right, padding.Up + padding.Down);

		rt.sizeDelta = size;

		// fit items into box
		Vector2 bottomLeftWorld = rt.localToWorldMatrix.MultiplyPoint(
			new Vector2(rt.rect.x, rt.rect.y));

		Vector2 offset = minPos - bottomLeftWorld - new Vector2(padding.Left, padding.Down);
		foreach (Transform child in transform) {
			child.position -= (Vector3)offset;
		}
	}

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