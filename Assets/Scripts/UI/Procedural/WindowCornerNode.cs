using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cfg = Config.UI.Window.CornerNode;

public class WindowCornerNode : MonoBehaviour {
	public enum Corner {
		TopLeft,
		TopRight,
		BottomLeft,
		BottomRight
	};
	public Corner Position;

	bool hovered = false;
	bool dragging = false;
	float curSize;

	RectTransform rt;
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
		if (!hovered) return;

	}
}