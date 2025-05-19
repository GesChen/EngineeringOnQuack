using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg = Config.UI.Window.CornerNode;

public class WindowSizeNode : MonoBehaviour {
	public enum Positions {
		BottomLeft,
		TopLeft,
		TopRight,
		BottomRight
	};
	public Positions position;
	private LiveWindow main;

	bool hovered = false;
	[HideInInspector] public bool dragging = false;
	float curSize;
	Vector2 dragStartCenter;
	bool oppositeVert;
	bool oppositeHori;

	[HideInInspector] public RectTransform rt;
	void Start() {
		main = GetComponentInParent<LiveWindow>();
		rt = GetComponent<RectTransform>();
	}

	void Update() {
		if (main.Config.Resizable) {
			CheckHover();
			UpdateSize();
			HandleMouse();
		}
	}

	void CheckHover() {
		hovered = UIHovers.CheckStrictlyFirst(transform);
	}

	void UpdateSize() {
		float mouseDist = Vector2.Distance(transform.position, Conatrols.Mouse.Position);
		float t = Mathf.InverseLerp(cfg.ExpansionStartDist, cfg.ExpansionEndDist, mouseDist);

		float size = cfg.EasingFunction(t) * cfg.NormalSize;

		if (hovered) size = cfg.HoverSize;
		if (dragging) size = cfg.DragSize;
		if (main.manager.anyDragging && !dragging) size = 0;

		curSize = Mathf.Lerp(curSize, size, Config.UI.Visual.Smoothness * Time.deltaTime);
		rt.sizeDelta = curSize * Vector2.one;
	}

	void HandleMouse() {
		bool notHoverOrDrag = !(hovered || dragging);
		bool anyDraggingNotThis = main.manager.anyDragging && !dragging;

		if (notHoverOrDrag || anyDraggingNotThis) return;

		if (!dragging && Conatrols.Mouse.Left.PressedThisFrame) {
			dragging = true;
			GetOtherCorner();
			dragStartCenter = otherCornerPos;
		}
		if (Conatrols.Mouse.Left.ReleasedThisFrame) {
			dragging = false;
		}

		if (dragging) {
			GetOtherCorner();

			SetCornerPosition(Conatrols.Mouse.Position);

			oppositeVert =
				(position == Positions.TopLeft || position == Positions.TopRight)
				? Conatrols.Mouse.Position.y < dragStartCenter.y
				: Conatrols.Mouse.Position.y > dragStartCenter.y;

			oppositeHori =
				(position == Positions.TopRight || position == Positions.BottomRight)
				? Conatrols.Mouse.Position.x < dragStartCenter.x
				: Conatrols.Mouse.Position.x > dragStartCenter.x;

			DebugExtra.DrawPoint(dragStartCenter);

			if (oppositeVert)
				main.FlipNodesVertically();

			if (oppositeHori)
				main.FlipNodesHorizontally();
		}
	}

	Vector2 otherCornerPos;
	void GetOtherCorner() {
		Positions opposing = position switch {
			Positions.BottomLeft => Positions.TopRight,
			Positions.TopLeft => Positions.BottomRight,
			Positions.TopRight => Positions.BottomLeft,
			Positions.BottomRight => Positions.TopLeft,
			_ => Positions.TopRight
		};

		otherCornerPos = main.cornerNodes.Find(n => n.position == opposing).transform.position;
	}

	public void SetCornerPosition(Vector2 pos) {
		// recalculate center and size
		Vector2 newCenter = (otherCornerPos + pos) / 2;
		Vector2 newSize = HF.Vector2Abs(otherCornerPos - pos);

		print($"setting");
		main.rt.position = newCenter;
		main.rt.sizeDelta = newSize;
	}
}