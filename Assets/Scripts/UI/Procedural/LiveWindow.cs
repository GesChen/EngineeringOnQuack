using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiveWindow : MonoBehaviour {
	public List<WindowSizeNode> cornerNodes = new();
	public Transform backgroundImage;
	[HideInInspector] public WindowManager manager;
	[HideInInspector] public RectTransform rt;
	[HideInNormalInspector] public bool dragging = false;
	[HideInNormalInspector] public bool anyNodesDragging = false;
	public ClassWindow.Config Config = new() { Movable = true, Resizable = true };
	WindowSizeNode TL;
	WindowSizeNode TR;
	WindowSizeNode BL;
	WindowSizeNode BR;

	void Start() {
		manager = GetComponentInParent<WindowManager>();
		rt = GetComponent<RectTransform>();
	}

	public void FlipNodesVertically() {
		TL.position = WindowSizeNode.Positions.BottomLeft;
		TR.position = WindowSizeNode.Positions.BottomRight;
		BL.position = WindowSizeNode.Positions.TopLeft;
		BR.position = WindowSizeNode.Positions.TopRight;
	}

	public void FlipNodesHorizontally() {
		TL.position = WindowSizeNode.Positions.TopRight;
		TR.position = WindowSizeNode.Positions.TopLeft;
		BL.position = WindowSizeNode.Positions.BottomRight;
		BR.position = WindowSizeNode.Positions.BottomLeft;
	}

	void Update() {
		SetNodesActive(Config.Resizable);

		if (Config.Resizable) {
			Find();
			SetAnchors();
			CheckNodes();
		}
		if (Config.Movable) {
			HandleDrag();
		}
	}

	void SetNodesActive(bool state) {
		cornerNodes.ForEach(n => n.gameObject.SetActive(state));
	}

	void Find() {
		TL = cornerNodes.Find(n => n.position == WindowSizeNode.Positions.TopLeft);
		TR = cornerNodes.Find(n => n.position == WindowSizeNode.Positions.TopRight);
		BL = cornerNodes.Find(n => n.position == WindowSizeNode.Positions.BottomLeft);
		BR = cornerNodes.Find(n => n.position == WindowSizeNode.Positions.BottomRight);
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