using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;
using static ScriptEditor;
using Codice.Client.Common;
using UnityEngine.UIElements;

public class ScriptEditor : MonoBehaviour {
	public List<Line> lines;
	public ScrollWindow scroll;
	public CustomVerticalLayout lineContentVerticalLayout;
	public RectTransform lineContentContainer;
	public CustomVerticalLayout lineNumbersVerticalLayout;
	[HideInNormalInspector] public RectTransform lineNumbersRect;
	public SyntaxHighlighter syntaxHighlighter;
	public List<Caret> carets = new();

	[Header("temporary local config options, should move to global config soon")]
	public float numberToContentSpace;
	public TMP_FontAsset font;
	public float fontSize;
	public Color selectionColor;

	public static Line NewLine(string str) => new() { content = str };
	public struct Line {
		public int lineNumber;
		public string content;
		public List<float> IndexTs;
		public List<Component> components;
	}

	[HideInNormalInspector] public float lineNumberWidth;
	[HideInNormalInspector] public float allLinesHeight;

	#region LocalContext
	[HideInInspector]
	public struct LCVariable { // it would be inside localcontext if it wasnt so fucking deep
		public string Name;

		public enum Types {
			Normal,
			MembFunc,
			Type
		}
		public Types Type;
		public int IndentLevel;

		public override readonly string ToString() {
			return $"% {Name} ({Type}) at {IndentLevel}";
		}
	}
	public class LocalContext {
		public List<LCVariable> Variables;
		public bool InComment;

		public LocalContext() {
			Variables = new();
			InComment = false;
		}
	}

	LocalContext LC;
	#endregion

	float charUVAmount;

	void Start() {
		lineNumbersRect = lineNumbersVerticalLayout.GetComponent<RectTransform>();
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
			if (line.components != null) {
				Destroy(line.components[0].gameObject); // line contents
				Destroy(line.components[2].gameObject); // line number
			}
		}
	}

	void Regenerate() {

		// recalculate max line number width
		TextMeshProUGUI testingText = lineContentVerticalLayout.gameObject.AddComponent(typeof(TextMeshProUGUI)) as TextMeshProUGUI;
		testingText.font = font;
		testingText.fontSize = fontSize;
		Vector2 numberSize = HF.TextWidthExact(lines.Count.ToString(), testingText);

		lineNumberWidth = numberSize.x;
		allLinesHeight = numberSize.y;

		// fix container
		lineContentContainer.offsetMin = new(lineNumberWidth + numberToContentSpace, 0);

		// reset localcontext
		LC = new() {
			Variables = new(),
			InComment = false,
		};

		// generate lines
		for (int i = 0; i < lines.Count; i++) {
			Line line = lines[i];

			List<Component> generatedLineComponents = GenerateLine(line);
			line.components = generatedLineComponents;
			lines[i] = line;
		}

		// scale all containers to max width
		float longestLineWidth = -1;
		int longestLine = -1;
		for (int i = 0; i < lines.Count; i++) {
			float width = (lines[i].components[0] as RectTransform).rect.width;
			if (width > longestLineWidth) {
				longestLineWidth = width;
				longestLine = i;
			}
		}

		float maxWidth = Mathf.Max(
			longestLineWidth, // widest of all components
			lineContentContainer.rect.width); // must at minimum be as wide as the container

		lines.ForEach(l => (l.components[0] as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth));

		string longestConvertedTabs = ConvertTabsToSpaces(lines[longestLine].content);

		charUVAmount = 1f / longestConvertedTabs.Length;

		// calculate ts (charuv must have a value)
		for (int i = 0; i < lines.Count; i++) {
			Line line = lines[i];
			List<float> ts = CalculateTs(i);
			line.IndexTs = ts;

			lines[i] = line;
		}
	}

	List<float> CalculateTs(int i) {
		// hopefully this loop isnt too slow for being called once
		// can be precomputed if needed
		// index = index of cursor location, basically 1 before the actual char

		List<float> TtoIndex = new();
		float pos = 0;
		foreach (char c in lines[i].content) {
			TtoIndex.Add(pos);

			pos += charUVAmount * (c == '\t' ? LanguageConfig.SpacesPerTab : 1);
		}
		// pos isnt gonna be 1 but need to add it again to be able to select last item still
		TtoIndex.Add(pos);

		return TtoIndex;
	}

	List<Component> GenerateLine(Line line) {
		// make line number object
		(GameObject NObj, TextMeshProUGUI NText, RectTransform NRect)
			= NewText(
				"Line Number",
				line.lineNumber.ToString(),
				lineNumbersVerticalLayout.transform,
				TextAlignmentOptions.Right,
				lineNumberWidth);

		// convert tabs to aligned spaces (for tmpro, original is unchanged)
		string processed = ConvertTabsToSpaces(line);

		// colorize
		var colors = syntaxHighlighter.LineColorTypesArray(processed, LC);
		print(processed);
		print(syntaxHighlighter.TypeArrayToString(colors));
		processed = syntaxHighlighter.TagLine(processed, colors);

		// make actual line content
		(GameObject LCObj, TextMeshProUGUI LCText, RectTransform LCRect)
			= NewText(
				"Line Content",
				$"{processed}",
				lineContentVerticalLayout.transform,
				TextAlignmentOptions.Left,
				0); // temp set width to zero, recalculate later

		Vector2 LCSize = HF.TextWidthExact(processed, LCText);
		LCRect.sizeDelta = new(LCSize.x, allLinesHeight);

		// setup line container rect
		LCRect.anchorMin = new(0, 1);
		LCRect.anchorMax = new(0, 1);
		LCRect.pivot = new(0, 1);
		LCRect.sizeDelta = new(LCSize.x, allLinesHeight);

		NRect.localPosition = Vector2.zero;
		LCRect.localPosition = new(lineNumberWidth + numberToContentSpace, 0);

		//Labels l = lineContainer.AddComponent<Labels>();
		//l.Set(NRect, "name");
		//l.Set(LCRect, "contents");

		// return components
		return new() {
			LCRect,
			LCText,
			NRect
		};
	}

	string ConvertTabsToSpaces(Line line) {
		return ConvertTabsToSpaces(line.content);
	}

	string ConvertTabsToSpaces(string line) {
		string tabsToSpaces = line;
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

	// TODO: do something with this
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
		HandleMouseInput();
		UpdateCarets();
	}

	void HandleMouseInput() {
		HandleSingleClicks();
		HandleDrag();
	}

	void HandleSingleClicks() {
		if (!Controls.IM.Mouse.Left.WasPressedThisFrame()) return;

		Vector2Int? pos = CurrentMouseHover();
		if (!pos.HasValue) return;

		SetSingleCaret(pos.Value, pos.Value);
	}

	bool dragging = false;
	Vector2Int dragStart;
	void HandleDrag() {
		Vector2Int? pos = CurrentMouseHover();
		if (Controls.IM.Mouse.Left.WasPressedThisFrame() &&
			pos.HasValue) { // down && hovering
			dragging = true;
			dragStart = pos.Value;
		} else
		if (Controls.IM.Mouse.Left.WasReleasedThisFrame()) {
			dragging = false;
		}

		if (dragging) {
			// will have to add alt and shift and stuff soon
			// for now this is just normal

			if (pos.HasValue)
				SetSingleCaret(pos.Value, dragStart);
		}
	}

	void SetSingleCaret(Vector2Int head, Vector2Int tail) {
		if (carets.Count != 1) {
			foreach (var caret in carets)
				caret.Destroy();
			carets.Clear();

			Caret singleCaret = new(this, head, tail);
			singleCaret.Initialize();
			carets.Add(singleCaret);
		}
		else {
			carets[0].UpdatePos(head, tail);
		}
	}

	void UpdateCarets() {
		foreach (var caret in carets)
			caret.Update();
	}

	// long ass name
	int GetCharIndexAtWorldSpacePosition(int line, int hoverIndex) {

		// not sure why this happens but it just does idk
		if (hoverIndex >= UIHovers.results.Count)
			return -1;

		RectTransform rt = lines[line].components[0] as RectTransform;

		Vector3[] corners = new Vector3[4];
		rt.GetWorldCorners(corners);

		RaycastResult result = UIHovers.results[hoverIndex];
		
		Vector2? uv = HF.UVOfHover(result);
		if (!uv.HasValue) return -1;
		float t = uv.Value.x;

		// determine which t is closest to real t
		List<float> ts = lines[line].IndexTs;
		float closestDist = float.PositiveInfinity;
		int charIndex = -1;
		for (int i = 0; i < ts.Count; i++) {
			float dist = MathF.Abs(ts[i] - t);
			if (dist < closestDist) {
				closestDist = dist;
				charIndex = i;
			}
		}

		return charIndex;
	}

	Vector2Int? CurrentMouseHover() {
		(int line, int hoverIndex) = FindLineHoveringOver();
		if (hoverIndex == -1) return null;

		int index = GetCharIndexAtWorldSpacePosition(line, hoverIndex);
		if (index == -1) return null;

		return new(index, line);
	}

	(int lineIndex, int hoverIndex) FindLineHoveringOver() {
		if (lines == null || lines[0].components == null) return (-1, -1);

		for (int i = 0; i < lines.Count; i++) {
			RectTransform contents = lines[i].components[0] as RectTransform;
			int index = UIHovers.hovers.IndexOf(contents);
			if (index != -1) return (i, index);
		}
		return (-1, -1);
	}

	public (RectTransform rt, float t) GetLocation(Vector2Int vec) {
		return (
			lines[vec.y].components[0] as RectTransform,
			lines[vec.y].IndexTs[vec.x]);
	}
}