using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

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

	[HideInInspector]
	public static List<string> DefaultInternalFuncs() => new() {
		"num",
		"bool",
		"str",
		"list",
		"dict",
		"abs",
		"sqrt",
		"round",
		"sum",
		"max",
		"min"
	};
	public class LocalContext {
		public List<string> InternalFunctions;

		public struct Variable {
			public string Name;
			public int Type; // 0-normal, 1-function or member
		}
		public List<Variable> Variables;
	}

	void Start() {
	}

	public void Load(Script script) {
		string[] lines = script.OriginalText.Split('\n').Select(l => l.TrimEnd()).ToArray();
		Load(lines);
	}

	public void Load(string[] strLines) {
		Clear();

		lines = new();
		for (int i = 0; i < strLines.Length; i++) {
			lines.Add(new() {
				content = strLines[i],
				lineNumber = i + 1
			});
		}

		Regenerate();
	}

	void Clear() {

		// delete all existing lines

		if (lines == null) return;

		foreach (Line line in lines) {
			if (line.components != null)
				Destroy(line.components[0].gameObject);
		}
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
			lines[i].components = generatedLineComponents;
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

		// convert tabs to aligned spaces (for tmpro, original is unchanged)
		string processed = ConvertTabsToSpaces(line);

		// colorize 

		// make actual line content
		(GameObject LCObj, TextMeshProUGUI LCText, RectTransform LCRect)
			= NewText(
				"Line Content",
				$"{processed}",
				lineContainer.transform,
				TextAlignmentOptions.Left,
				0); // temp set width to zero, recalculate later

		Vector2 LCSize = HF.TextWidthExact(processed, LCText);
		LCRect.sizeDelta = new(LCSize.x, allLinesHeight);

		// setup line container rect
		LRect.anchorMin = new(0, 1);
		LRect.anchorMax = new(0, 1);
		LRect.pivot = new(0, 1);
		LRect.sizeDelta = new(lineNumberWidth + lineNumberToLineGap + LCSize.x, allLinesHeight);

		NRect.localPosition = Vector2.zero;
		LCRect.localPosition = new(lineNumberWidth + lineNumberToLineGap, 0);

		//Labels l = lineContainer.AddComponent<Labels>();
		//l.Set(NRect, "name");
		//l.Set(LCRect, "contents");

		// return components
		return new() {
			lineContainer.transform,
			LCText,
			NRect,
			LCRect
		};
	}

	string ConvertTabsToSpaces(Line line) {
		string tabsToSpaces = line.content;
		static int tabIndexToSpaceCount(int i) => HF.Mod(-i - 1, LanguageConfig.SpacesPerTab) + 1;
		for (int i = 0; i < tabsToSpaces.Length; i++) {
			char c = tabsToSpaces[i];
			if (c == '\t') {
				int num = tabIndexToSpaceCount(i);
				tabsToSpaces = HF.ReplaceSection(tabsToSpaces, i, i, new string(' ', num));
			}
		}

		return tabsToSpaces;
	}

	//string ColorizeLine(Line line) <- might have to make a custom tokenizer or somehow piggyback off
	// existing tokenizer

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

	void Update() {
		print(FindLineHoveringOver());
	}

	int FindLineHoveringOver() {
		print(lines[0].components);
		if (lines[0].components == null) return -1;

		for (int i = 0; i < lines.Count; i++) {
			RectTransform contents = lines[i].components[3] as RectTransform;
			print(contents);
			if (UIHovers.hovers.Contains(contents)) return i;
		}
		return -1;
	}
}