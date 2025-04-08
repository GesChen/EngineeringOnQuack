using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollBar : MonoBehaviour {
	public enum Directions { Horizontal, Vertical }
	public Directions Direction;
	public RectTransform Background;
	public RectTransform BarObject;
	Image barImage;
	Color barOriginalColor;
	RectTransform thisRect;

	[Header("also super temporary colors to move somewhere eventually")]
	public Color hoverTint;
	public Color pressedTint;

	void Awake() {
		thisRect = GetComponent<RectTransform>();
		barImage = BarObject.GetComponent<Image>();
		barOriginalColor = barImage.color;
	}

	public void UpdateBar(float percent, float scale, bool invert) {
		bool tooBig = scale > 1;
		Background.gameObject.SetActive(!tooBig);
		BarObject.gameObject.SetActive(!tooBig);
		if (tooBig) return;

		float totalLength =
			Direction == Directions.Horizontal ?
				thisRect.rect.width :
				thisRect.rect.height;

		if (invert) percent = (1 - percent);
		float barLength = totalLength * scale;
		float t = percent * (totalLength - barLength);

		float t1 = totalLength - t - barLength;
		float t2 = -t;

		//yBarParent.sizeDelta = new(barSize, yBarParent.sizeDelta.y); // for script width control
		if (Direction == Directions.Horizontal) {
			BarObject.offsetMin = new(t1, BarObject.offsetMin.y);
			BarObject.offsetMax = new(t2, BarObject.offsetMax.y);
		} else {
			BarObject.offsetMin = new(BarObject.offsetMin.x, t1);
			BarObject.offsetMax = new(BarObject.offsetMax.x, t2);
		}
	}

	bool lastHovered;
	bool lastPressed;
	bool dragging;
	Vector2 dragStartPos;
	void Update() {
		bool hovered = UIHovers.hovers.Contains(transform);
		bool pressed = hovered && Controls.inputMaster.Mouse.Left.IsPressed();

		Color tint = new(0, 0, 0, 0);
		if (hovered) tint = hoverTint;
		if (pressed) tint = pressedTint; // overrides hovertint

		barImage.color = Color.Lerp(barOriginalColor, tint, tint.a);

		if (hovered &&
			(pressed != lastPressed && pressed)) { // mouse down while hovering
			dragging = true;
			dragStartPos = Controls.inputMaster.Mouse.Position.ReadValue<Vector2>();
		}else
		if (!pressed) {
			dragging = false;
		}
	
		lastHovered = hovered;
		lastPressed = pressed;
	}


}