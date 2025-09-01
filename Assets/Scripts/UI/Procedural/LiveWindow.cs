using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

/// <remarks>
/// Don't store references to LiveWindows themselves
/// if they are not temporary as windowrealiser.updatewindow
/// will destroy old references and you'll get null errors
/// </remarks>
public class LiveWindow : MonoBehaviour {
	public List<WindowSizeNode> cornerNodes = new();
	public Image backgroundImage;
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
	[HideInInspector] public Canvas canvas;

	public CWindow Source;

	void Awake() {
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

	public void Show() {
		gameObject.SetActive(true);
	}
	public void Hide() {
		gameObject.SetActive(false);
	}

	void Update() {
		if (Config.HideOnStart) {
			if (Time.frameCount - Source.CreationFrame == global::Config.UI.Behaviour.MaxFramesForRealization)
				gameObject.SetActive(false);
			if (Time.frameCount - Source.CreationFrame <= global::Config.UI.Behaviour.MaxFramesForRealization) {
				//transform.position = new Vector2(-1000, -1000); // somewhere offscreen to load
				return;
			}
		}

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
		bool hovered = UIHovers.CheckFirstIgnoringChildrenOfOther(backgroundImage.transform, contentsContainer);
		if (!hovered && !dragging) return;

		if (!dragging && Conatrols.Mouse.Left.PressedThisFrame && DragAllowed()) {
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
			FourSides padding = global::Config.UI.Behaviour.CanvasInnerWindowsPadding;
			Vector2 canvasSize = manager.CanvasRect.sizeDelta;

			float halfWidth = rt.sizeDelta.x / 2;
			float halfHeight = rt.sizeDelta.y / 2;

			Vector2 clampedPos = HF.Vector2Clamp(
				rt.GetCenter(),
				new Vector2(halfWidth, halfHeight) 
				+ new Vector2(padding.Left, padding.Down),
				
				new Vector2(canvasSize.x - halfWidth, canvasSize.y - halfHeight) 
				- new Vector2(padding.Right, padding.Up));

			rt.SetCenter(clampedPos);
		}
	}

	// check if anythings stopping dragging from occuring
	bool DragAllowed() {
		if (UIHovers.hovers.Any(h => h.GetComponent<Scrollbar>() != null)) return false;

		return true;
	}

	void CheckNodes() {
		anyNodesDragging = cornerNodes.Any(n => n.dragging);
	}

	#region PlaceAt helpers and variations
	// only sets the location, show it manually

	/*
	public void PlaceAt(RectTransform target, bool horizontal, bool prioritizeTopRight) {
		if (horizontal)
			PlaceAtHorizontal(target, prioritizeTopRight);
		else
			PlaceAtVertical(target, prioritizeTopRight);
	}

	public void PlaceAt(Vector3[] target, bool horizontal, bool prioritizeTopRight) {
		if (horizontal)
			PlaceAtHorizontal(target, prioritizeTopRight);
		else
			PlaceAtVertical(target, prioritizeTopRight);
	}

	public void PlaceAt(Vector3 singlePosition, bool horizontal, bool prioritizeTopRight) {
		PlaceAt(
			new[] { singlePosition, singlePosition, singlePosition, singlePosition },
			horizontal,
			prioritizeTopRight);
	}

	private void PlaceAtVertical(RectTransform target, bool prioritizeTop) {
		Vector3[] corners = new Vector3[4];
		target.GetWorldCorners(corners);

		PlaceAtVertical(corners, prioritizeTop);
	}

	private void PlaceAtHorizontal(RectTransform target, bool prioritizeRight) {
		Vector3[] corners = new Vector3[4];
		target.GetWorldCorners(corners);

		PlaceAtHorizontal(corners, prioritizeRight);
	}

	/// <summary>
	/// Try to put this window at some corners
	/// </summary>
	/// <param name="corners">4 corner array of the possible positions</param>
	private void PlaceAtVertical(Vector3[] corners, bool prioritizeTop) {
		// check fits
		float topY = corners[1].y + global::Config.UI.Behaviour.FlyoutDistance;
		float bottomY = corners[0].y - global::Config.UI.Behaviour.FlyoutDistance;
		bool fitsOnTop = topY + rt.rect.height < canvas.renderingDisplaySize.y;
		bool fitsOnBottom = bottomY - rt.rect.height > 0;

		bool putOnTop = prioritizeTop ? fitsOnTop : !fitsOnBottom;

		float xOfLeftLeftwards = corners[2].x - rt.rect.width;
		//float yOfTopUpwards = corners[4].y + rt.rect.height;
		bool fitsLeftwards = xOfLeftLeftwards > 0;

		int targetCorner;
		if (putOnTop) targetCorner = fitsLeftwards ? 3 : 0;
		else targetCorner = fitsLeftwards ? 2 : 1;

		Vector2 pos = new(
			fitsLeftwards ? corners[2].x : corners[1].x,
			putOnTop ? topY : bottomY);

		SetWorldCorner(rt, pos, targetCorner);
	}

	private void PlaceAtHorizontal(Vector3[] corners, bool prioritizeRight) {
		// check fits
		float rightX = corners[2].x + global::Config.UI.Behaviour.FlyoutDistance;
		float leftX = corners[1].x - global::Config.UI.Behaviour.FlyoutDistance;
		bool fitsOnRight = rightX + rt.rect.width < canvas.renderingDisplaySize.x;
		bool fitsOnLeft = leftX - rt.rect.width > 0;

		bool putOnRight = prioritizeRight ? fitsOnRight : !fitsOnLeft;

		float yOfBottomDownwards = corners[2].y - rt.rect.height;
		//float yOfTopUpwards = corners[4].y + rt.rect.height;
		bool fitsDownwards = yOfBottomDownwards > 0;

		int targetCorner;
		if (putOnRight) targetCorner = fitsDownwards ? 1 : 0;
		else targetCorner = fitsDownwards ? 2 : 3;

		Vector2 pos = new(
			putOnRight ? rightX : leftX,
			fitsDownwards ? corners[1].y : corners[0].y);

		SetWorldCorner(rt, pos, targetCorner);
	}*/

	/* old placeats, might need them as reference
	public void PlaceAt(Vector3 point, bool horizontal, bool prioritizeRight, bool prioritizeUp) {
		PlaceAt(
			new[] { point, point, point, point }, 
			horizontal, 
			prioritizeRight, 
			prioritizeUp);
	}

	public void PlaceAt(RectTransform target, bool horizontal, bool prioritizeRight, bool prioritizeUp) {
		Vector3[] corners = new Vector3[4];
		target.GetWorldCorners(corners);

		PlaceAt(corners, horizontal, prioritizeRight, prioritizeUp);
	}

	public void PlaceAt(Vector3[] corners, bool horizontal, bool prioritizeRight, bool prioritizeUp) {
		// check fits

		// which side of the corners array to place on
		float height = canvas.renderingDisplaySize.y;
		float width = canvas.renderingDisplaySize.x;

		float topY = corners[1].y + global::Config.UI.Behaviour.FlyoutDistance;
		float bottomY = corners[0].y - global::Config.UI.Behaviour.FlyoutDistance;
		bool fitsOnTop = topY + rt.rect.height < height;
		bool fitsOnBottom = bottomY - rt.rect.height > 0;

		float rightX = corners[2].x + global::Config.UI.Behaviour.FlyoutDistance;
		float leftX = corners[1].x - global::Config.UI.Behaviour.FlyoutDistance;
		bool fitsOnRight = rightX + rt.rect.width < width;
		bool fitsOnLeft = leftX - rt.rect.width > 0;

		bool putOnTop = prioritizeUp ? fitsOnTop : !fitsOnBottom;
		bool putOnRight = prioritizeRight ? fitsOnRight : !fitsOnLeft;

		// which way to offset the entire thing
		float xOfRightRightwards = corners[3].y + rt.rect.height;
		float xOfLeftLeftwards = corners[2].x - rt.rect.width;
		bool fitsRightwards = xOfRightRightwards < width;
		bool fitsLeftwards = xOfLeftLeftwards > 0;

		float yOfTopUpwards = corners[3].y + rt.rect.height;
		float yOfBottomDownwards = corners[2].y - rt.rect.height;
		bool fitsUpwards = yOfTopUpwards < height;
		bool fitsDownwards = yOfBottomDownwards > 0;

		bool putRightwards = prioritizeRight ? fitsRightwards : !fitsLeftwards;
		bool putUpwards = prioritizeUp ? fitsUpwards : !fitsDownwards;

		// determine actual placements
		Vector2 pos;
		int targetCorner;
		if (horizontal) {
			targetCorner = 
				putOnRight 
				? putUpwards ? 0 : 1 
				: putUpwards ? 3 : 2;

			pos = new(
				putOnRight ? rightX : leftX,
				putUpwards ? corners[0].y : corners[1].y);
		} else {
			targetCorner = 
				putOnTop 
				? putRightwards ? 0 : 3 
				: putRightwards ? 1 : 2;

			pos = new(
				putRightwards ? corners[1].x : corners[2].x,
				putOnTop ? topY : bottomY);
		}

		SetWorldCorner(pos, targetCorner);
	}
	*/

	/// <summary>
	/// Place this LW at a point given edge and priorities, 
	/// without going out of bounds. Edge will modify which side of the point
	/// it is placed on in this case
	/// </summary>
	/// <param name="targetCorners">Corners of the target object to align to</param>
	/// <param name="targetEdge">0-Top 1-Right 2-Bottom 3-Left</param>
	/// <param name="alignment">How this LW is  aligned against the edge. <br></br><b>T</b> - up or right, <b>F</b> - left or down</param>
	public void PlaceAt(Vector3 target, int targetEdge, bool alignment) {
		PlaceAt(new[] { target, target, target, target }, targetEdge, alignment);
	}

	/// <summary>
	/// Place this LW aligned to a RT given edge and priorities, 
	/// without going out of bounds
	/// </summary>
	/// <param name="targetCorners">Corners of the target object to align to</param>
	/// <param name="targetEdge">0-Top 1-Right 2-Bottom 3-Left</param>
	/// <param name="alignment">How this LW is  aligned against the edge. <br></br><b>T</b> - up or right, <b>F</b> - left or down</param>
	public void PlaceAt(RectTransform target, int targetEdge, bool alignment) {
		Vector3[] corners = new Vector3[4];
		target.GetWorldCorners(corners);

		PlaceAt(corners, targetEdge, alignment);
	}

	/// <summary>
	/// Place this LW at some corners given edge and priorities, 
	/// without going out of bounds
	/// </summary>
	/// <param name="targetCorners">Corners of the target object to align to</param>
	/// <param name="targetEdge">0-Top 1-Right 2-Bottom 3-Left</param>
	/// <param name="alignment">How this LW is  aligned against the edge. <br></br><b>T</b> - up or right, <b>F</b> - left or down</param>
	public void PlaceAt(Vector3[] targetCorners, int targetEdge, bool alignment) {
		(bool horizontal, bool placement) =
			targetEdge switch {
				0 => (false, true),
				1 => (true, true),
				2 => (false, false),
				3 => (true, false),
				_ => throw new System.ArgumentException($"invalid targetEdge: {targetEdge}")
			};

		(int thisC, int testTargetC) = PlacementCorners(horizontal, placement, alignment);

		static int opposite(int corner) =>
			corner switch {
				0 => 2,
				1 => 3,
				2 => 0,
				3 => 1,
				_ => -1
			};

		int testCorner = opposite(thisC);

		var targetPos = targetCorners[testTargetC];
		SetWorldCorner(targetPos, thisC);

		Vector2 test = GetWorldCorner(testCorner);
		(bool testX, bool testY) = InBounds(test);

		if (!testX) {
			if (horizontal) placement = !placement;
			else alignment = !alignment;
		}
		if (!testY) {
			if (horizontal) alignment = !alignment;
			else placement = !placement;
		}

		int targetC;
		(thisC, targetC) = PlacementCorners(horizontal, placement, alignment);

		targetPos = targetCorners[targetC];
		SetWorldCorner(targetPos, thisC);
	}

	// convert positioning to corner placements
	// it makes a bit more sense in the brainstorming
	private (int thisC, int targetC) PlacementCorners(
		bool horizontal, bool placeUR, bool alignUR) =>
		(horizontal, placeUR) switch {
			(false, true)	=> alignUR ? (0, 1) : (3, 2),
			(true, true)	=> alignUR ? (0, 3) : (1, 2),
			(false, false)	=> alignUR ? (1, 0) : (2, 3),
			(true, false)	=> alignUR ? (3, 0) : (2, 1)
		};

	public Vector2 GetWorldCorner(int corner) {
		Vector3[] corners = new Vector3[4];
		rt.GetWorldCorners(corners);
		return corners[corner];
	}

	// i really shoudltn have made this a global function
	// bools for if that axis is in bounds
	public (bool x, bool y) InBounds(Vector2 p) {
		Vector2 canvasSize = manager.CanvasRect.sizeDelta;
		FourSides padding = global::Config.UI.Behaviour.CanvasInnerWindowsPadding;

		Vector2 min = new(padding.Left, padding.Down);
		Vector2 max = canvasSize - new Vector2(padding.Right, padding.Up);

		return (
			p.x > min.x && p.x < max.x,
			p.y > min.y && p.y < max.y);
	}

	// 0-BL 1-TL 2-TR 3-BR
	// this is kinda dumb but im lazy
	// so 4 is now center
	/// <summary>
	/// Puts a selected corner 1-3 or 4 for center at a position
	/// </summary>
	public void SetWorldCorner(Vector3 targetWorldPosition, int corner) {
		Vector3[] worldCorners = new Vector3[4];
		rt.GetWorldCorners(worldCorners);

		Vector3 currentCornerPos;
		if (corner < 4) {
			currentCornerPos = worldCorners[corner];
		} else if (corner == 4) {
			// center cuz yeah
			currentCornerPos = (worldCorners[0] + worldCorners[2]) / 2;
		} else {
			throw new("whoops.. bad corner!");
		}

		Vector3 offset = targetWorldPosition - currentCornerPos;

		rt.position += offset;
	}
	#endregion
}