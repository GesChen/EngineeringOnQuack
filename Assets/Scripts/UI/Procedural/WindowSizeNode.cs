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

	void LateUpdate() {
		isClose = position == Positions.TopRight;

		if (main.Config.Closable)
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
			//GetOtherCorner();

			var pad = Config.UI.Behaviour.CanvasInnerWindowsPadding;
			Vector2 pos = Conatrols.Mouse.Position;

			// sizemin
			Vector2 minSize = Vector2.Max(
				Config.UI.Behaviour.WindowUniversalMinSize, 
				main.Config.Size.Minimum);
			Vector2 minC1 = dragStartCenter + minSize;
			Vector2 minC2 = dragStartCenter - minSize;
			pos = new(
				ClosestOutside(pos.x, minC1.x, minC2.x),
				ClosestOutside(pos.y, minC1.y, minC2.y));

			// sizemax
			Vector2 min = Vector2.Min(dragStartCenter + main.Config.Size.Maximum, dragStartCenter - main.Config.Size.Maximum);
			Vector2 max = Vector2.Max(dragStartCenter + main.Config.Size.Maximum, dragStartCenter - main.Config.Size.Maximum);

			pos = HF.Vector2Clamp(pos, min, max);
			
			// canvas padding
			pos = HF.Vector2Clamp(
				pos,
				new Vector2(pad.Left, pad.Down),
				main.manager.CanvasRect.sizeDelta - new Vector2(pad.Right, pad.Up));
			
			SetCornerPosition(pos);

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

	public static float ClosestOutside(float p, float b1, float b2) {
		float min = Mathf.Min(b1, b2);
		float max = Mathf.Max(b1, b2);

		if (p < min || p > max) return p;

		return 
			Mathf.Abs(p - b1) < Mathf.Abs(p - b2)
			? b1
			: b2;
	}

	public static Vector2 ClosestOutside(Vector2 point, Vector2 p1, Vector2 p2) {
		float xmin = Mathf.Min(p1.x, p2.x);
		float xmax = Mathf.Max(p1.x, p2.x);
		float ymin = Mathf.Min(p1.y, p2.y);
		float ymax = Mathf.Max(p1.y, p2.y);

		// already outside
		if (point.x < xmin || point.x > xmax || point.y < ymin || point.y > ymax)
			return point;

		// distances to edges
		float dLeft  = point.x - xmin;
		float dRight = xmax - point.x;
		float dDown  = point.y - ymin;
		float dUp    = ymax - point.y;

		float min = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dDown, dUp));

		if (min == dLeft) return new Vector2(xmin, point.y);
		if (min == dRight) return new Vector2(xmax, point.y);
		if (min == dDown) return new Vector2(point.x, ymin);
		else return new Vector2(point.x, ymax);
	}

	void Close() {
		main.dragging = false;
		dragging = false;
		main.anyNodesDragging = false;

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

		main.rt.SetCenter(newCenter);
		main.rt.sizeDelta = newSize;
	}
}