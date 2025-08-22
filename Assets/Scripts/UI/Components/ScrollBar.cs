using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollBar : MonoBehaviour {
	public enum Directions { Horizontal, Vertical }
	public Directions Direction;
	public RectTransform Background;
	public RectTransform BarObject;
	public RectTransform OptionalOtherBar;

	Image barImage;
	Color barOriginalColor;
	RectTransform thisRect;

	static Color hoverTint = new(1, 1, 1, .2f);
	static Color pressedTint = new(1, 1, 1, .5f);

	float totalLength;
	[HideInNormalInspector] public float currentPercent;
	float currentScale;
	bool currentInversion;

	void Awake() {
		thisRect = GetComponent<RectTransform>();
		barImage = BarObject.GetComponent<Image>();
		barOriginalColor = barImage.color;
	}

	bool lastHovered;
	bool lastPressed;
	[HideInNormalInspector] public bool dragging;
	
	float dragStartBackT;
	float dragStartPercent;

	void Update() {
		bool hovered;
		if (OptionalOtherBar)
			hovered = UIHovers.CheckFirstAllowing(BarObject, OptionalOtherBar);
		else
			hovered = UIHovers.CheckFirstAllowing(BarObject);

		bool pressed = Conatrols.IM.Mouse.Left.IsPressed();

		Color tint = new(0, 0, 0, 0);
		if (hovered) tint = hoverTint;
		if (dragging) tint = pressedTint; // overrides hovertint

		barImage.color = Color.Lerp(barOriginalColor, tint, tint.a);

		if (hovered &&
			pressed != lastPressed && pressed) { // mouse down while hovering
			dragging = true;

			// assuming the canvas is screen space, if this somehow changes then well have to change it too. 
			dragStartBackT = BackT();
			dragStartPercent = currentPercent;
		} else
		if (!pressed) {
			dragging = false;
		}

		if (dragging) {
			// i have no clue why this works so dont touch it. 
			float tOffset = currentInversion ?
				TtoPercentUnclamped(dragStartBackT) - dragStartPercent :
				TtoPercentUnclamped(dragStartBackT) + dragStartPercent;

			float newPercent = currentInversion ?
				TtoPercentUnclamped(BackT()) - tOffset :
				tOffset - TtoPercentUnclamped(BackT());

			UpdateBar(Mathf.Clamp01(newPercent), currentScale, currentInversion);
		}

		lastHovered = hovered;
		lastPressed = pressed;
	}

	public void UpdateBar(float percent, float scale, bool invert) {
		currentPercent = percent;
		currentScale = scale;
		currentInversion = invert;

		bool tooBig = scale > 1;
		Background.gameObject.SetActive(!tooBig);
		BarObject.gameObject.SetActive(!tooBig);
		if (tooBig) return;

		totalLength =
			Direction == Directions.Horizontal ?
				thisRect.rect.width :
				thisRect.rect.height;

		if (invert) percent = 1 - percent;
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

	float BarT() {
		Vector2? barMouseUV = HF.RectScreenSpaceMouseUV(BarObject);
		if (!barMouseUV.HasValue) return -1;

		return Direction == Directions.Horizontal ?
			barMouseUV.Value.x :
			barMouseUV.Value.y ;
	}

	float BackT() {
		Vector2? barMouseUV = HF.RectScreenSpaceMouseUV(Background);
		if (!barMouseUV.HasValue) return -1;

		return Direction == Directions.Horizontal ?
			barMouseUV.Value.x :
			barMouseUV.Value.y;
	}

	float TtoPercentUnclamped(float t) =>
		(t - currentScale / 2) /
		(1 - currentScale);
}