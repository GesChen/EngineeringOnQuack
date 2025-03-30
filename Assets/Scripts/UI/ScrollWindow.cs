using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollWindow : MonoBehaviour
{
	public CustomVerticalLayout contents;
	public float sensitivity;

	[Header("Bar")]
	public RectTransform yScrollbar;
	public RectTransform yBarParent;
	public RectTransform xScrollbar;
	public RectTransform xBarParent;
	public float barSize;

	RectTransform thisRect;
	RectTransform contentsRect;
	float xScrollAmount;
	float yScrollAmount;
	float xTotalBarLength;
	float yTotalBarLength;
	float windowWidth;
	float windowHeight;
	float xScrollableDist;
	float yScrollableDist;

	void Start() {
		xScrollAmount = 0;
		yScrollAmount = 0;

		thisRect = GetComponent<RectTransform>();
		contentsRect = contents.GetComponent<RectTransform>();
	}

	void Update() {
		Recalculate();
		HandleInput();
		UpdateContentsPosition();
		UpdateVertBar();
		UpdateHorizBar();
	}

	void Recalculate() {
		xTotalBarLength = xBarParent.rect.width;
		yTotalBarLength = yBarParent.rect.height;

		windowWidth = thisRect.rect.width;
		windowHeight = thisRect.rect.height;

		xScrollableDist = contents.maxWidth - windowWidth; 
		yScrollableDist = contents.totalHeight - windowHeight; // may be negative
	}

	void HandleInput() {
		xScrollAmount += Controls.inputMaster.TextEditor.Scroll.ReadValue<Vector2>().x * sensitivity * Time.deltaTime;
		
		if (Controls.inputMaster.TextEditor.Shift.IsPressed())
			xScrollAmount -= Controls.inputMaster.TextEditor.Scroll.ReadValue<Vector2>().y * sensitivity * Time.deltaTime;
		else
			yScrollAmount -= Controls.inputMaster.TextEditor.Scroll.ReadValue<Vector2>().y * sensitivity * Time.deltaTime;
		
		xScrollAmount = Mathf.Clamp(xScrollAmount, 0, Mathf.Max(0, xScrollableDist));
		yScrollAmount = Mathf.Clamp(yScrollAmount, 0, Mathf.Max(0, yScrollableDist));
	}

	void UpdateContentsPosition() {
		contentsRect.localPosition = new(-xScrollAmount, yScrollAmount);
	}

	void UpdateHorizBar() {
		xBarParent.gameObject.SetActive(contents.maxWidth > windowWidth);
		if (yBarParent.gameObject.activeSelf)
			xBarParent.offsetMax = new(-barSize, xBarParent.offsetMax.y);

		if (contents.maxWidth == 0) return;

		float percent = xScrollAmount / xScrollableDist;
		float barWidth = xTotalBarLength * (windowWidth / contents.maxWidth);
		float x = (1 - percent) * (xTotalBarLength - barWidth);

		float x1 = xTotalBarLength - x - barWidth;
		float x2 = -x;

		xBarParent.sizeDelta = new(yBarParent.sizeDelta.x, barSize);
		xScrollbar.offsetMin = new(x1, xScrollbar.offsetMin.y);
		xScrollbar.offsetMax = new(x2, xScrollbar.offsetMax.y);
	}

	void UpdateVertBar() {
		yBarParent.gameObject.SetActive(contents.totalHeight > windowHeight);

		if (contents.totalHeight == 0) return;

		float percent = yScrollAmount / yScrollableDist;
		float barHeight = yTotalBarLength * (windowHeight / contents.totalHeight);
		float y = percent * (yTotalBarLength - barHeight);

		float y1 = yTotalBarLength - y - barHeight;
		float y2 = -y;

		yBarParent.sizeDelta = new(barSize, yBarParent.sizeDelta.y);
		yScrollbar.offsetMin = new(yScrollbar.offsetMin.x, y1);
		yScrollbar.offsetMax = new(yScrollbar.offsetMax.x, y2);
	}
}