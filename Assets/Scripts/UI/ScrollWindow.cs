using UnityEngine;

public class ScrollWindow : MonoBehaviour {
	public CustomVerticalLayout contents;
	public float sensitivity;

	[Header("Bar")]
	public ScrollBar xBar;
	public ScrollBar yBar;

	protected RectTransform thisRect;
	protected RectTransform contentsRect;
	protected float xScrollAmount;
	protected float yScrollAmount;
	protected float windowWidth;
	protected float windowHeight;
	protected float xScrollableDist;
	protected float yScrollableDist;

	void Start() {
		xScrollAmount = 0;
		yScrollAmount = 0;

		thisRect = GetComponent<RectTransform>();
		contentsRect = contents.GetComponent<RectTransform>();
	}

	void Update() {
		Recalculate();

		if (!xBar.dragging && !yBar.dragging) {
			HandleInput();

			xBar.UpdateBar(xScrollAmount / xScrollableDist, windowWidth / contents.maxWidth, true);
			yBar.UpdateBar(yScrollAmount / yScrollableDist, windowHeight / contents.totalHeight, false);
		}
		else {
			if (xBar.dragging)
				xScrollAmount = xBar.currentPercent * xScrollableDist;
			if (yBar.dragging)
				yScrollAmount = yBar.currentPercent * yScrollableDist;
		}

		UpdateContentsPosition();
	}

	public virtual void Recalculate() {
		windowWidth = thisRect.rect.width;
		windowHeight = thisRect.rect.height;

		xScrollableDist = contents.maxWidth - windowWidth;
		yScrollableDist = contents.totalHeight - windowHeight; // may be negative
	}

	bool CheckMouse() {
		return UIHovers.CheckStrictlyFirst(transform);
	}

	void HandleInput() {
		if (!CheckMouse()) return;

		xScrollAmount += Conatrols.IM.TextEditor.Scroll.ReadValue<Vector2>().x * sensitivity * Time.deltaTime;

		if (Conatrols.IM.TextEditor.Shift.IsPressed())
			xScrollAmount -= Conatrols.IM.TextEditor.Scroll.ReadValue<Vector2>().y * sensitivity * Time.deltaTime;
		else
			yScrollAmount -= Conatrols.IM.TextEditor.Scroll.ReadValue<Vector2>().y * sensitivity * Time.deltaTime;

		xScrollAmount = Mathf.Clamp(xScrollAmount, 0, Mathf.Max(0, xScrollableDist));
		yScrollAmount = Mathf.Clamp(yScrollAmount, 0, Mathf.Max(0, yScrollableDist));
	}

	public virtual void UpdateContentsPosition() {
		contentsRect.localPosition = new(-xScrollAmount, yScrollAmount);
	}
}