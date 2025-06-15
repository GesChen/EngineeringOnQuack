using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiveWindow : MonoBehaviour {
	public List<WindowSizeNode> cornerNodes = new();
	public Transform backgroundImage;
	public Transform contentsContainer;
	[HideInInspector] public WindowManager manager;
	[HideInInspector] public RectTransform rt;
	[HideInNormalInspector] public bool dragging = false;
	[HideInNormalInspector] public bool anyNodesDragging = false;
	public CWindow.Configuration Config = new() { Movable = true, Resizable = true };
	WindowSizeNode TL;
	WindowSizeNode TR;
	WindowSizeNode BL;
	WindowSizeNode BR;
	Canvas canvas;

	public CWindow Source;


	void Awake() {
		Config.CallEvents(CWindow.Configuration.Timings.Awake, Source);
	}

	void Start() {
		Config.CallEvents(CWindow.Configuration.Timings.Start, Source);
		manager = GetComponentInParent<WindowManager>();
		rt = GetComponent<RectTransform>();
		canvas = GetComponentInParent<Canvas>();
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
		if (Config.HideOnStart) {
			if (Time.frameCount == global::Config.UI.Behaviour.MaxFramesForRealization)
				gameObject.SetActive(false);
			if (Time.frameCount <= global::Config.UI.Behaviour.MaxFramesForRealization) {
				transform.position = new Vector2(-1000, -1000); // somewhere offscreen to load
				return;
			}
		}

		Config.CallEvents(CWindow.Configuration.Timings.Update, Source);

		SetNodesActive(Config.Resizable, Config.Closable);

		if (Config.Resizable || Config.Closable) {
			Find();
			SetAnchors();
		}
		if (Config.Resizable)
			CheckNodes();

		CheckSize();
		if (Config.Movable) {
			HandleDrag();
		}
	}

	void SetNodesActive(bool state, bool closeState) {
		foreach (var n in cornerNodes) {
			if (n.position == WindowSizeNode.Positions.TopRight)
				n.gameObject.SetActive(state || closeState);
			else
				n.gameObject.SetActive(state);
		}
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

	void CheckSize() {
		rt.sizeDelta = Vector2.Min(Vector2.Max(rt.sizeDelta, Config.Size.Minimum), Config.Size.Maximum);
	}

	Vector2 dragOffset;
	Vector2 dragStartPos;
	bool goodToStartDragging = false;
	void HandleDrag() {
		bool hovered = UIHovers.CheckFirstIgnoringChildrenOfOther(backgroundImage, contentsContainer);
		if (!hovered && !dragging) return;

		if (!dragging && Conatrols.Mouse.Left.PressedThisFrame) {
			dragging = true;
			dragOffset = (Vector2)rt.position - Conatrols.Mouse.Position;
			dragStartPos = Conatrols.Mouse.Position;
			transform.SetAsLastSibling();

			goodToStartDragging = false;
		}
		if (Conatrols.Mouse.Left.ReleasedThisFrame) {
			dragging = false;
		}

		if (dragging) {
			if (Vector2.Distance(dragStartPos, Conatrols.Mouse.Position) >
				global::Config.UI.Behaviour.WindowMinDragDist)
				goodToStartDragging = true;

			if (!goodToStartDragging) return;

			transform.position = Conatrols.Mouse.Position + dragOffset;

			// prevent going off the sides
			Vector2 padding = global::Config.UI.Behaviour.CanvasInnerWindowsPadding * Vector2.one;
			Vector2 canvasSize = manager.canvasRect.sizeDelta;

			float halfWidth = rt.sizeDelta.x / 2;
			float halfHeight = rt.sizeDelta.y / 2;

			Vector2 clampedPos = HF.Vector2Clamp(
				transform.position,
				new Vector2(halfWidth, halfHeight) + padding,
				new Vector2(canvasSize.x - halfWidth, canvasSize.y - halfHeight) - padding);

			transform.position = clampedPos;
		}
	}

	void CheckNodes() {
		anyNodesDragging = cornerNodes.Any(n => n.dragging);
	}

	public void PlaceAt(RectTransform target) {
		Vector3[] corners = new Vector3[4];
		target.GetWorldCorners(corners);

		PlaceAt(corners);
	}

	/// <summary>
	/// Try to put this window at some corners
	/// </summary>
	/// <param name="corners">4 corner array of the possible positions</param>
	public void PlaceAt(Vector3[] corners) {
		// check fits
		float rightX = corners[2].x + global::Config.UI.Behaviour.FlyoutDistance;
		float leftX = corners[1].x - global::Config.UI.Behaviour.FlyoutDistance;
		bool fitsOnRight = rightX + rt.rect.width < canvas.renderingDisplaySize.x;

		float yOfBottomDownwards = corners[2].y - rt.rect.height;
		//float yOfTopUpwards = corners[4].y + rt.rect.height;
		bool fitsDownwards = yOfBottomDownwards > 0;

		int targetCorner;
		if (fitsOnRight) targetCorner = fitsDownwards ? 1 : 0;
		else targetCorner = fitsDownwards ? 2 : 3;

		Vector2 pos = new(
			fitsOnRight ? rightX : leftX,
			fitsDownwards ? corners[1].y : corners[0].y);

		SetWorldCorner(rt, pos, targetCorner);
	}

	// 0-BL 1-TL 2-TR 3-BR
	public void SetWorldCorner(RectTransform rect, Vector3 targetWorldPosition, int corner) {
		Vector3[] worldCorners = new Vector3[4];
		rect.GetWorldCorners(worldCorners);

		Vector3 currentCornerPos = worldCorners[corner];

		Vector3 offset = targetWorldPosition - currentCornerPos;

		rect.position += offset;
	}
}