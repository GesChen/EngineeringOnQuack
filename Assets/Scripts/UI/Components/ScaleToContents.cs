using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// TODO: OPTIMIZE THIS!!! if it starts causing lag
// testing seems to show very short frame times but you never know once theres a million ui things
// im not dealing with this for now good luck

public class ScaleToContents : MonoBehaviour {
	public FourSides padding;

	public bool IgnoreHorizontal;
	public bool IgnoreVertical;

	private RectTransform rt;
	void Start() {
		rt = GetComponent<RectTransform>();
	}
	/*
	void Update() {
		FastScaleWithoutOffset();
	}*/

	void LateUpdate() {
		if (rt.childCount == 0) return;

		var pad = new Vector2(padding.Left + padding.Right, padding.Up + padding.Down);
		
		//rt.sizeDelta = new(100, 100);

		Bounds bounds = ModifiedRRTB(rt);
		//rt.sizeDelta = bounds.size;
		//rt.sizeDelta += pad;

		if (!IgnoreHorizontal) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.size.x);
		if (!IgnoreVertical) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bounds.size.y);

		/*Vector2 padOffset = new Vector2((padding.Right - padding.Left) * 0.5f,
								(padding.Up - padding.Down) * 0.5f);*/
		//Vector2 padOffset = pad / 2;

		//Vector2 offset = bounds.center - (Vector3)(rt.worldToLocalMatrix * rt.rect.center);
		//Vector2 offset = (Vector2)bounds.center - rt.rect.center;
		//rt.localPosition += (Vector3)offset;

		//rt.SetCenter(bounds.center);
	}

	/// <summary>
	/// modified version of RectTransformUtility.CalculateRelativeRectTransformBounds
	/// that actually ignores the self rt and assumes child 
	/// empty check already done
	/// </summary>
	Bounds ModifiedRRTB(RectTransform rt) {
		Vector3[] s_Corners = new Vector3[4];
		Matrix4x4 worldToLocalMatrix = rt.worldToLocalMatrix;

		Vector3 min = Vector3.positiveInfinity;
		Vector3 max = Vector3.negativeInfinity;

		void Traverse(RectTransform current) {
			if (!current.gameObject.activeInHierarchy) return;
			if (current != rt) {
				current.GetWorldCorners(s_Corners);
				for (int j = 0; j < 4; j++) {
					Vector3 localPoint = worldToLocalMatrix.MultiplyPoint3x4(s_Corners[j]);
					min = Vector3.Min(min, localPoint);
					max = Vector3.Max(max, localPoint);
				}
			}
			if (current.GetComponent<Mask>() != null) return;
			foreach (Transform child in current) {
				Traverse(child as RectTransform);
			}
		}

		Traverse(rt);

		Bounds result = new(min, Vector3.zero);
		result.Encapsulate(max);
		return result;
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
			bool active = child.gameObject.activeInHierarchy;
			if (!active) continue;
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

	// why are you so frustrating
	void FastScaleWithoutOffset() {
		Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rt, rt);
		Vector2 size = bounds.size + new Vector3(padding.Left + padding.Right, padding.Up + padding.Down);
		rt.sizeDelta = size;

		Vector2 offset = (Vector2)bounds.center - rt.rect.center;
		rt.localPosition += (Vector3)offset;
	}
}