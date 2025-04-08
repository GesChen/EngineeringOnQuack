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
		HandleInput();
		UpdateContentsPosition();

		xBar.UpdateBar(xScrollAmount / xScrollableDist, windowWidth / contents.maxWidth, true);
		yBar.UpdateBar(yScrollAmount / yScrollableDist, windowHeight / contents.totalHeight, false);
	}

	void Recalculate() {
		windowWidth = thisRect.rect.width;
		windowHeight = thisRect.rect.height;

		xScrollableDist = contents.maxWidth - windowWidth;
		yScrollableDist = contents.totalHeight - windowHeight; // may be negative
	}

	bool CheckMouse() {
		return UIHovers.hovers.Contains(transform);
	}

	void HandleInput() {
		if (!CheckMouse()) return;

		xScrollAmount += Controls.inputMaster.TextEditor.Scroll.ReadValue<Vector2>().x * sensitivity * Time.deltaTime;

		if (Controls.inputMaster.TextEditor.Shift.IsPressed())
			xScrollAmount -= Controls.inputMaster.TextEditor.Scroll.ReadValue<Vector2>().y * sensitivity * Time.deltaTime;
		else
			yScrollAmount -= Controls.inputMaster.TextEditor.Scroll.ReadValue<Vector2>().y * sensitivity * Time.deltaTime;

		xScrollAmount = Mathf.Clamp(xScrollAmount, 0, Mathf.Max(0, xScrollableDist));
		yScrollAmount = Mathf.Clamp(yScrollAmount, 0, Mathf.Max(0, yScrollableDist));
	}

	public virtual void UpdateContentsPosition() {
		contentsRect.localPosition = new(-xScrollAmount, yScrollAmount);
	}
}