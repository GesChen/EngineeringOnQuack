using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Window : MonoBehaviour {
	public List<WindowCornerNode> cornerNodes;
	[HideInInspector] public RectTransform rt;
	WindowCornerNode TL;
	WindowCornerNode TR;
	WindowCornerNode BL;
	WindowCornerNode BR;

	void Start() {
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
		TL = cornerNodes.Find(n => n.position == WindowCornerNode.Corner.TopLeft);
		TR = cornerNodes.Find(n => n.position == WindowCornerNode.Corner.TopRight);
		BL = cornerNodes.Find(n => n.position == WindowCornerNode.Corner.BottomLeft);
		BR = cornerNodes.Find(n => n.position == WindowCornerNode.Corner.BottomRight);

		TL.rt.anchorMin = new(0, 1);
		TL.rt.anchorMax = new(0, 1);
		TR.rt.anchorMin = new(1, 1);
		TR.rt.anchorMax = new(1, 1);
		BL.rt.anchorMin = new(0, 0);
		BL.rt.anchorMax = new(0, 0);
		BR.rt.anchorMin = new(1, 0);
		BR.rt.anchorMax = new(1, 0);
	}
}