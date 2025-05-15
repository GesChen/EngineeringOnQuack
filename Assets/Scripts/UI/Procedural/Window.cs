using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Window : MonoBehaviour {
	public List<WindowCornerNode> cornerNodes;
	public Transform backgroundImage;
	[HideInInspector] public WindowManager manager;
	[HideInInspector] public RectTransform rt;
	[HideInInspector] public bool dragging = false;
	[HideInInspector] public bool anyNodesDragging = false;
	WindowCornerNode TL;
	WindowCornerNode TR;
	WindowCornerNode BL;
	WindowCornerNode BR;

	void Start() {
		manager = GetComponentInParent<WindowManager>();
		rt = GetComponent<RectTransform>();
	}

	public void FlipNodesVertically() {
		TL.position = WindowCornerNode.Corner.BottomLeft;
		TR.position = WindowCornerNode.Corner.BottomRight;
		BL.position = WindowCornerNode.Corner.TopLeft;
		BR.position = WindowCornerNode.Corner.TopRight;
	}

	public void FlipNodesHorizontally() {
		TL.position = WindowCornerNode.Corner.TopRight;
		TR.position = WindowCornerNode.Corner.TopLeft;
		BL.position = WindowCornerNode.Corner.BottomRight;
		BR.position = WindowCornerNode.Corner.BottomLeft;
	}

	void Update() {
		Find();
		SetAnchors();
		HandleDrag();
		CheckNodes();
	}

	void Find() {
		TL = cornerNodes.Find(n => n.position == WindowCornerNode.Corner.TopLeft);
		TR = cornerNodes.Find(n => n.position == WindowCornerNode.Corner.TopRight);
		BL = cornerNodes.Find(n => n.position == WindowCornerNode.Corner.BottomLeft);
		BR = cornerNodes.Find(n => n.position == WindowCornerNode.Corner.BottomRight);
	}

	void SetAnchors() {
		TL.rt.anchorMin = new(0, 1);
		TL.rt.anchorMax = new(0, 1);
		TR.rt.anchorMin = new(1, 1);
		TR.rt.anchorMax = new(1, 1);
		BL.rt.anchorMin = new(0, 0);
		BL.rt.anchorMax = new(0, 0);
		BR.rt.anchorMin = new(1, 0);
		BR.rt.anchorMax = new(1, 0);
	}

	Vector2 dragOffset;
	void HandleDrag() {
		bool hovered = UIHovers.CheckStrictlyFirst(backgroundImage);
		if (!hovered && !dragging) return;

		if (!dragging && Conatrols.Mouse.Left.PressedThisFrame) {
			dragging = true;
			dragOffset = (Vector2)rt.position - Conatrols.Mouse.Position;
		}
		if (Conatrols.Mouse.Left.ReleasedThisFrame) {
			dragging = false;
		}

		if (dragging) {
			transform.position = Conatrols.Mouse.Position + dragOffset;

			// prevent going off the sides
			Vector2 canvasSize = manager.canvasRect.sizeDelta;

			float halfWidth = rt.sizeDelta.x / 2;
			float halfHeight = rt.sizeDelta.y / 2;

			Vector2 clampedPos = HF.Vector2Clamp(
				transform.position,
				new(halfWidth, halfHeight),
				new(canvasSize.x - halfWidth, canvasSize.y - halfHeight));

			transform.position = clampedPos;
		}
	}

	void CheckNodes() {
		anyNodesDragging = cornerNodes.Any(n => n.dragging);
	}
}