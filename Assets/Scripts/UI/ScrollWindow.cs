using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollWindow : MonoBehaviour
{
	public CustomVerticalLayout contents;
	public float sensitivity;

	[Header("Bar")]
	public RectTransform scrollbar;
	public RectTransform barParent;
	public float barwidth;

	RectTransform thisRect;
	RectTransform contentsRect;
	float scrollAmount;
	float totalBarHeight;
	float windowHeight;
	float scrollableDist;

	void Start() {
		scrollAmount = 0;
		thisRect = GetComponent<RectTransform>();
		contentsRect = contents.GetComponent<RectTransform>();
	}

	void Update() {
		Recalculate();
		HandleInput();
		UpdateContentsPosition();
		UpdateBar();
	}

	void Recalculate() {
		totalBarHeight = barParent.rect.height;

		windowHeight = thisRect.rect.height;
		
		scrollableDist = contents.totalHeight - windowHeight; // may be negative
	}

	void HandleInput() {
		scrollAmount -= Controls.inputMaster.UI.ScrollWheel.ReadValue<Vector2>().y * sensitivity * Time.deltaTime;
		scrollAmount = Mathf.Clamp(scrollAmount, 0, Mathf.Max(0, scrollableDist));
	}

	void UpdateContentsPosition() {
		contentsRect.localPosition = new(0, scrollAmount);
	}

	void UpdateBar() {
		barParent.gameObject.SetActive(contents.totalHeight > totalBarHeight);

		if (contents.totalHeight == 0) return;

		float percent = scrollAmount / scrollableDist;
		float barHeight = totalBarHeight * (windowHeight / contents.totalHeight);
		float y = percent * (totalBarHeight - barHeight);

		float y1 = totalBarHeight - y - barHeight;
		float y2 = -y;

		barParent.sizeDelta = new(barwidth, barParent.sizeDelta.y);
		scrollbar.offsetMin = new(scrollbar.offsetMin.x, y1);
		scrollbar.offsetMax = new(scrollbar.offsetMax.x, y2);
	}
}