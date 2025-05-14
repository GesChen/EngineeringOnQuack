using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg = Config.UI.Window.CornerNode;

public class WindowCornerNode : MonoBehaviour {
	public enum Corner {
		BottomLeft,
		TopLeft,
		TopRight,
		BottomRight
	};
	public Corner position;
	public Window main;

	bool hovered = false;
	bool dragging = false;
	float curSize;
	Vector2 dragStartCenter;
	Corner dragStartPos;
	bool oppositeVert;
	bool oppositeHori;

	[HideInInspector] public RectTransform rt;
	void Start() {
		rt = GetComponent<RectTransform>();
	}

	void Update() {
		CheckHover();
		UpdateSize();
		HandleMouse();
	}

	void CheckHover() {
		hovered = UIHovers.CheckStrictlyFirst(transform);
	}

	void UpdateSize() {
		float mouseDist = Vector2.Distance(transform.position, Conatrols.Mouse.Position);
		float t = Mathf.InverseLerp(cfg.ExpansionStartDist, cfg.ExpansionEndDist, mouseDist);

		float size = cfg.EasingFunction(t) * cfg.NormalSize;

		size += hovered ? cfg.HoverAddedSize : 0;

		curSize = Mathf.Lerp(curSize, size, Config.UI.Visual.Smoothness * Time.deltaTime);
		rt.sizeDelta = curSize * Vector2.one;
	}

	void HandleMouse() {
		if (!hovered && !dragging) return;

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
				(position == Corner.TopLeft || position == Corner.TopRight)
				? Conatrols.Mouse.Position.y < dragStartCenter.y
				: Conatrols.Mouse.Position.y > dragStartCenter.y;

			oppositeHori =
				(position == Corner.TopRight || position == Corner.BottomRight)
				? Conatrols.Mouse.Position.x < dragStartCenter.x
				: Conatrols.Mouse.Position.x > dragStartCenter.x;

			if (oppositeVert)
				main.FlipNodesVertically();

			if (oppositeHori)
				main.FlipNodesHorizontally();
		}
	}

	Vector2 otherCornerPos;
	void GetOtherCorner() {
		Corner opposing = position switch {
			Corner.BottomLeft => Corner.TopRight,
			Corner.TopLeft => Corner.BottomRight,
			Corner.TopRight => Corner.BottomLeft,
			Corner.BottomRight => Corner.TopLeft,
		};

		otherCornerPos = main.cornerNodes.Find(n => n.position == opposing).transform.position;
	}

	public void SetCornerPosition(Vector2 pos) {
		// recalculate center and size
		Vector2 newCenter = (otherCornerPos + pos) / 2;
		Vector2 newSize = HF.Vector2Abs(otherCornerPos - pos);

		main.rt.position = newCenter;
		main.rt.sizeDelta = newSize;
	}
}