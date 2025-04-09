using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class ScriptEditor : MonoBehaviour {
	public List<Line> lines;
	public ScrollWindow scroll;
	public CustomVerticalLayout lineContentVerticalLayout;
	public RectTransform lineContentContainer;
	public CustomVerticalLayout lineNumbersVerticalLayout;
	RectTransform lineNumbersRect;
	public SyntaxHighlighter syntaxHighlighter;

	[Header("temporary local config options, should move to global config soon")]
	public float numberToContentSpace;
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

	bool parentCanvasIsScreenSpace = false;

	float charUVAmount;

	void Start() {
		lineNumbersRect = lineNumbersVerticalLayout.GetComponent<RectTransform>();

		parentCanvasIsScreenSpace = GetComponentInParent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay;
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

		string lineContent = lines[longestLine].content;
		int tabs = lineContent.Count(c => c == '\t'); // its just a for loop under the hood
		int singleChars = (lineContent.Length - tabs) + (tabs * LanguageConfig.SpacesPerTab); // convert tabs to spaces

		charUVAmount = 1f / singleChars;

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
		LCRect.sizeDelta = new(lineNumberWidth + numberToContentSpace + LCSize.x, allLinesHeight);

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
		(int line, int hoverIndex) = FindLineHoveringOver();
		if (line != -1) {
			GetCharIndexAtWorldSpacePosition(line, hoverIndex);
		}
	}

	// long ass name
	int GetCharIndexAtWorldSpacePosition(int line, int hoverIndex) {
		RectTransform rt = lines[line].components[0] as RectTransform;

		Vector3[] corners = new Vector3[4];
		rt.GetWorldCorners(corners);

		RaycastResult result;
		try { result = UIHovers.results[hoverIndex]; }
		catch {
			print($"fucking index error at {hoverIndex}");
			print($"theres {UIHovers.results.Count} btw");
			return -1;
		}
		
		Vector2? uv = HF.UVOfHover(result);
		if (!uv.HasValue) return -1;
		float t = uv.Value.x;

		// hopefully this loop isnt too slow for being called once
		// can be precomputed if needed
		// index = index of cursor location, basically 1 before the actual char
		List<float> TtoIndex = new();
		float pos = 0;
		foreach (char c in lines[line].content) {
			TtoIndex.Add(pos);

			if (c == '\t') pos += charUVAmount * 4;
			else pos += charUVAmount;
		}
		// pos isnt gonna be 1 but need to add it again to be able to select last item still
		TtoIndex.Add(pos);

		// determine which t is closest to real t
		float closestDist = float.PositiveInfinity;
		int charIndex = -1;
		for (int i = 0; i < TtoIndex.Count; i++) {
			float dist = MathF.Abs(TtoIndex[i] - t);
			if (dist < closestDist) {
				closestDist = dist;
				charIndex = i;
			}
		}

		return charIndex;
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
}