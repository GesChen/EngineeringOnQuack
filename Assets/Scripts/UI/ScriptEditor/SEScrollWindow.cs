using UnityEngine;

public class SEScrollWindow : ScrollWindow {
	[Header("SE specific")]
	public ScriptEditor ScriptEditor;

	public override void UpdateContentsPosition() {
		contentsRect.localPosition = new(-xScrollAmount, yScrollAmount);
		ScriptEditor.lineNumbersRect.localPosition = new(0, yScrollAmount);
	}

	public override void Recalculate() {
		windowWidth = thisRect.rect.width;
		windowHeight = thisRect.rect.height;

		xScrollableDist = contents.maxWidth - windowWidth 
			+ ScriptEditor.lineNumbersVerticalLayout.maxWidth 
			+ ScriptEditor.numberToContentSpace;
		yScrollableDist = contents.totalHeight - windowHeight; // may be negative
	}
}