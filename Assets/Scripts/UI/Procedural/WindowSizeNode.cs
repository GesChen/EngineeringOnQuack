using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

	bool isClose;

	[HideInInspector] public RectTransform rt;
	Image im;

	void OnEnable() {
		rt = GetComponent<RectTransform>();
		rt.sizeDelta = Vector2.zero;
	}

	void Start() {
		main = GetComponentInParent<LiveWindow>();
		im = GetComponent<Image>();
	}

	void Update() {
		isClose = position == Positions.TopRight;
		UpdateCloseSprite();

		rt.anchoredPosition = Vector2.zero;
		if (main.Config.Resizable || main.Config.Closable) {
			CheckHover();
			UpdateSize();
		}

		if (main.Config.Resizable)
			HandleMouse();

		if (main.Config.Closable && isClose)
			HandleClose();
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

	RectTransform closeIcon;
	bool lastWasClose = false;
	void UpdateCloseSprite() {
		if (isClose != lastWasClose) {
			if (isClose)
				ShowClose();
			else
				HideClose();
		}
		lastWasClose = isClose;
	}

	void ShowClose() {
		GameObject obj = new("Close Icon");
		closeIcon = obj.AddComponent<RectTransform>();
		closeIcon.SetParent(transform);
		closeIcon.anchorMin = Vector2.zero;
		closeIcon.anchorMax = Vector2.one;
		closeIcon.offsetMin = Vector2.zero;
		closeIcon.offsetMax = Vector2.zero;

		var image = obj.AddComponent<Image>();
		image.sprite = cfg.CloseSprite;
		image.color = cfg.CloseButtonColor;
		image.raycastTarget = false;
	}

	void HideClose() {
		Destroy(closeIcon.gameObject);
	}

	void HandleClose() {
		if (isClose && hovered) {
			bool criteria = cfg.DoubleClickToClose
				? Conatrols.Mouse.Left.DoubleClicked
				: Conatrols.Mouse.Left.SingleClicked;

			if (criteria) Close();
		}
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

			if (oppositeVert)
				main.FlipNodesVertically();

			if (oppositeHori)
				main.FlipNodesHorizontally();
		}
	}

	void Close() {
		main.gameObject.SetActive(false);
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

		main.rt.position = newCenter;
		main.rt.sizeDelta = newSize;
	}
}