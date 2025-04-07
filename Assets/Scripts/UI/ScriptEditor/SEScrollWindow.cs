using UnityEngine;

public class SEScrollWindow : ScrollWindow {
	public RectTransform lineNumbersParent;

	public override void UpdateContentsPosition() {
		contentsRect.localPosition = new(-xScrollAmount, yScrollAmount);
		lineNumbersParent.localPosition = new(0, yScrollAmount);
	}
}