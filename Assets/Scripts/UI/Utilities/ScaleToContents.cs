using System.Collections.Generic;
using UnityEngine;

public class ScaleToContents : MonoBehaviour {
	public FourSides padding;
	private RectTransform rt;
	void Start() {
		rt = GetComponent<RectTransform>();
	}
	void Update() {
		Vector2 minPos = Vector2.positiveInfinity;
		Vector2 maxPos = Vector2.negativeInfinity;

		foreach (Transform child in transform) {
			var rt = child.GetComponent<RectTransform>();

			Vector3[] corners = new Vector3[4];
			rt.GetWorldCorners(corners);

			foreach(Vector3 c in corners) {
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
}