using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScriptEditor : MonoBehaviour {
	public List<string> testStrings;

	public List<Line> lines;
	public ScrollWindow scroll;
	public CustomVerticalLayout linesContainer;

	[Header("temporary local config options, should move to global config soon")]
	public float lineNumberToLineGap;
	public TMP_FontAsset font;
	public float fontSize;

	public static Line NewLine(string str) => new() { content = str };
	public struct Line {
		public int lineNumber;
		public string content;
		public List<Component> components;
	}

	float lineNumberWidth;
	float allLinesHeight;

	void Start() {
		lines = testStrings.Select(s => new Line() { content = s }).ToList();

		Regenerate();
	}

	void Regenerate() {

		// recalculate max line number width
		TextMeshProUGUI testingText = linesContainer.gameObject.AddComponent(typeof(TextMeshProUGUI)) as TextMeshProUGUI;
		testingText.font = font;
		testingText.fontSize = fontSize;
		Vector2 numberSize = HF.TextWidthExact((lines.Count + 1).ToString(), testingText);
		lineNumberWidth = numberSize.x;
		allLinesHeight = numberSize.y;

		// generate lines
		for (int i = 0; i < lines.Count; i++) {
			Line line = lines[i];

			List<Component> generatedLineComponents = GenerateLine(line);
			line.components = generatedLineComponents;
		}
	}

	List<Component> GenerateLine(Line line) {

		// make line container object
		GameObject lineContainer = new($"Line {line.lineNumber}");
		lineContainer.transform.SetParent(linesContainer.transform); // have to use transform cuz rt doesnt exist yet
		if (!lineContainer.TryGetComponent<RectTransform>(out var LRect)) 
			LRect = lineContainer.AddComponent<RectTransform>();

		// make line number object
		(GameObject NObj, TextMeshProUGUI NText, RectTransform NRect)
			= NewText(
				"Line Number",
				line.lineNumber.ToString(),
				lineContainer.transform,
				TextAlignmentOptions.Right,
				lineNumberWidth);

		// make actual line content
		(GameObject LCObj, TextMeshProUGUI LCText, RectTransform LCRect)
			= NewText(
				"Line Content",
				line.content,
				lineContainer.transform,
				TextAlignmentOptions.Left,
				0); // temp set width to zero, recalculate later

		Vector2 LCSize = HF.TextWidthExact(line.content, LCText);
		LCRect.sizeDelta = new(LCSize.x, allLinesHeight);

		// setup line container rect
		LRect.anchorMin = new(0, 1);
		LRect.anchorMax = new(0, 1);
		LRect.pivot = new(0, 1);
		LRect.sizeDelta = new(lineNumberWidth + lineNumberToLineGap + LCSize.x, allLinesHeight);

		NRect.localPosition = Vector2.zero;
		LCRect.localPosition = new(lineNumberWidth + lineNumberToLineGap, 0);

		// return components
		return new() {
			lineContainer.transform,
			LCText
		};
	}

	void UpdateLineContents(Line line, string newContents) {
		TextMeshProUGUI text = line.components[0] as TextMeshProUGUI;
		text.text = newContents;
	}

	(GameObject, TextMeshProUGUI, RectTransform) NewText(
		string name,
		string actualText,
		Transform parent,
		TextAlignmentOptions alignment,
		float width) {

		GameObject newObj = new(name);
		newObj.transform.SetParent(parent);

		// add text
		TextMeshProUGUI newText = newObj.AddComponent<TextMeshProUGUI>();
		newText.text = actualText;
		newText.font = font;
		newText.fontSize = fontSize;
		newText.alignment = alignment;

		// set up rt properly
		if (!newObj.TryGetComponent<RectTransform>(out var newRect)) 
			newRect = newObj.AddComponent<RectTransform>();
		newRect.anchorMin = new(0, 1);
		newRect.anchorMax = new(0, 1);
		newRect.pivot = new(0, 1);
		newRect.sizeDelta = new(width, allLinesHeight);

		return (newObj, newText, newRect);
	}
}