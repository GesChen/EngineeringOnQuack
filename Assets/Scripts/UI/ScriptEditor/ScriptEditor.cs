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
	[HideInNormalInspector] public RectTransform lineNumbersRect;
	public SyntaxHighlighter syntaxHighlighter;
	public List<Caret> carets = new();

	[Header("temporary local config options, should move to global config soon")]
	public float numberToContentSpace;
	public TMP_FontAsset font;
	public float fontSize;
	public Color selectionColor;

	public static Line NewLine(string str) => new() { Content = str };
	public class Line {
		public int LineNumber;
		public string Content;
		public List<float> IndexTs;
		public List<Component> Components;
		public SyntaxHighlighter.Types[] ColorsSpaces;
		public SyntaxHighlighter.Types[] ColorsOriginal;
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
				Content = strLines[i],
				LineNumber = i + 1
			});
		}

		Regenerate();
	}

	void Clear() {

		// delete all existing lines

		if (lines == null) return;

		foreach (Line line in lines) {
			if (line.Components != null) {
				Destroy(line.Components[0].gameObject); // line contents
				Destroy(line.Components[2].gameObject); // line number
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

			GenerateLine(line);
		}

		// scale all containers to max width
		float longestLineWidth = -1;
		int longestLine = -1;
		for (int i = 0; i < lines.Count; i++) {
			float width = (lines[i].Components[0] as RectTransform).rect.width;
			if (width > longestLineWidth) {
				longestLineWidth = width;
				longestLine = i;
			}
		}

		float maxWidth = Mathf.Max(
			longestLineWidth, // widest of all components
			lineContentContainer.rect.width); // must at minimum be as wide as the container

		lines.ForEach(l => (l.Components[0] as RectTransform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth));

		string longestConvertedTabs = ConvertTabsToSpaces(lines[longestLine]);

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
		foreach (char c in lines[i].Content) {
			TtoIndex.Add(pos);

			pos += charUVAmount * (c == '\t' ? LanguageConfig.SpacesPerTab : 1);
		}
		// pos isnt gonna be 1 but need to add it again to be able to select last item still
		TtoIndex.Add(pos);

		return TtoIndex;
	}

	void GenerateLine(Line line) {
		// make line number object
		(GameObject NObj, TextMeshProUGUI NText, RectTransform NRect)
			= NewText(
				"Line Number",
				line.LineNumber.ToString(),
				lineNumbersVerticalLayout.transform,
				TextAlignmentOptions.Right,
				lineNumberWidth);

		// convert tabs to aligned spaces (for tmpro, original is unchanged)
		string processed = ConvertTabsToSpaces(line);

		// colorize
		var colors = syntaxHighlighter.LineColorTypesArray(processed, LC);
		line.ColorsSpaces = colors;

		// need to store a usable copy based on tabs instead of spaces
		var tabsConvertedBack = RevertSpacesToTabs(colors, processed, line.Content);
		line.ColorsOriginal = tabsConvertedBack;

		//print(processed);
		//print(syntaxHighlighter.TypeArrayToString(colors));
		
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

		line.Components = new() {
			LCRect,
			LCText,
			NRect
		};
	}

	string ConvertTabsToSpaces(Line line) {
		return ConvertTabsToSpaces(line.Content);
	}

	int tabIndexToSpaceCount(int i) => HF.Mod(-i - 1, LanguageConfig.SpacesPerTab) + 1;
	string ConvertTabsToSpaces(string line) {
		string tabsToSpaces = line;
		for (int i = 0; i < tabsToSpaces.Length; i++) {
			char c = tabsToSpaces[i];
			if (c == '\t') {
				int num = tabIndexToSpaceCount(i);
				tabsToSpaces = HF.ReplaceSection(tabsToSpaces, i, i, new string(' ', num));
			}
		}

		return tabsToSpaces;
	}

	SyntaxHighlighter.Types[] RevertSpacesToTabs(
		SyntaxHighlighter.Types[] colors, 
		string convertedString, 
		string originalString) {

		var reconstructed = new SyntaxHighlighter.Types[originalString.Length];
		int ci = 0;
		int oi = 0;
		while (ci < convertedString.Length) { // they should both each their ends at the same time idk
			reconstructed[oi] = colors[ci];
			if (originalString[oi] == '\t')
				ci += tabIndexToSpaceCount(ci);
			else
				ci++;

			oi++;
		}
		return reconstructed;
	}

	// TODO: do something with this
	void UpdateLineContents(Line line, string newContents) {
		TextMeshProUGUI text = line.Components[0] as TextMeshProUGUI;
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
		bool clickedThisFrame = Controls.IM.Mouse.Left.WasPressedThisFrame();
		Vector2Int? mousePos = CurrentMouseHover();
		if (!mousePos.HasValue) return;

		DetectExtraClicks(clickedThisFrame, mousePos.Value);
		//HandleSingleClick(clickedThisFrame, mousePos.Value);
		//HandleDoubleClick(clickedThisFrame, mousePos.Value);
		//HandleTripleClick(clickedThisFrame, mousePos.Value);
		HandleDrag(clickedThisFrame, mousePos.Value);
	}

	void HandleSingleClick(bool clickedThisFrame,Vector2Int pos) {
		if (!clickedThisFrame) return;

		SetSingleCaret(pos, pos);
	}

	float lastClickTime;
	Vector2Int lastClickPos;
	int clicksInARow = 0;
	void DetectExtraClicks(bool clickedThisFrame, Vector2Int pos) {
		if (clickedThisFrame) {
			if (Time.time - lastClickTime < SEConfig.MultiClickThresholdMs / 1000 &&
			lastClickPos == pos) {
				clicksInARow++;
			} else {
				clicksInARow = 1;
			}

			lastClickTime = Time.time;
			lastClickPos = pos;
		}
	}

	void HandleDoubleClick(bool clickedThisFrame, Vector2Int pos) {
		if (!clickedThisFrame) return;
		if (clicksInARow == 2) {
			(int start, int end) = DoubleClickWordAt(pos);

			// gotta figure out shift soon
			SetSingleCaret(new(end, pos.y), new(start, pos.y));
		}
	}

	(int start, int end) DoubleClickWordAt(Vector2Int pos) {
		if (string.IsNullOrWhiteSpace(lines[pos.y].Content)) return (0, 0); // index errors everywhere

		var colors = lines[pos.y].ColorsOriginal; // does this store a reference to it or copy the array?
		string line = lines[pos.y].Content;

		static int chartype(char c) {
			if (char.IsLetterOrDigit(c)) return 0;
			if (char.IsSymbol(c)) return 1;
			if (char.IsWhiteSpace(c)) return 2;
			return -1;
		}

		var leftColorType = colors[pos.x - 1];
		var rightColorType =
			pos.x < colors.Length
			? colors[pos.x]
			: SyntaxHighlighter.Types.unassigned;

		bool leftUnassigned = leftColorType == SyntaxHighlighter.Types.unassigned;
		bool rightUnassigned = rightColorType == SyntaxHighlighter.Types.unassigned;

		// funy rules
		int findType = (leftUnassigned, rightUnassigned) switch {
			(true, true) => 0,
			(false, true) => 1,
			(true, false) => 2,
			(false, false) => 3
		};

		var findColorType = findType switch {
			0 => SyntaxHighlighter.Types.unassigned, // both dont, take the space
			1 => leftColorType, // only left, choose left
			2 => rightColorType, // only right, choose right
			3 => rightColorType, // both exist? default right
			_ => SyntaxHighlighter.Types.unassigned
		};

		int leftCharType = chartype(line[pos.x - 1]);
		int rightCharType =
			pos.x < colors.Length
			? chartype(line[pos.x])
			: -1;

		int findCharType = findType switch {
			0 => 2,
			1 => leftCharType,
			2 => rightCharType,
			3 => rightCharType,
			_ => -1
		};

		// search
		int left = pos.x - 1;
		while (left > 0 && 
			colors[left] == findColorType &&
			chartype(line[left]) == findCharType) 
			left--;
		left++;

		int right = pos.x;
		while (right < colors.Length && 
			colors[right] == findColorType &&
			chartype(line[right]) == findCharType)
			right++;
		//if (right == colors.Length) right--;

		return (left, right);
	}

	void HandleTripleClick(bool clickedThisFrame, Vector2Int pos) {
		if (!clickedThisFrame) return;
		if (clicksInARow >= 3) {
			SetSingleCaret(
				new(lines[pos.y].Content.Length, pos.y), 
				new(0, pos.y));
		}
	}

	bool dragging = false;
	Vector2Int dragStart;
	void HandleDrag(bool clickedThisFrame, Vector2Int pos) {
		if (clickedThisFrame) { // down
			dragging = true;
			dragStart = pos;
		} else
		if (Controls.IM.Mouse.Left.WasReleasedThisFrame()) {
			dragging = false;
		}

		if (dragging) {
			// will have to add alt and shift and stuff soon
			// for now this is just normal

			if (clicksInARow == 1)
				SetSingleCaret(pos, dragStart);
			else if (clicksInARow == 2) {
				(int dsS, int dsE) = DoubleClickWordAt(dragStart);
				(int deS, int deE) = DoubleClickWordAt(pos);

				Vector2Int start;
				Vector2Int end;

				if (dragStart.y == pos.y) {
					start = new(Mathf.Max(dsE, deE), pos.y);
					end = new(Mathf.Min(dsS, deS), pos.y);

					if (dragStart.x > pos.x) (start, end) = (end, start);
				} else
				if (dragStart.y < pos.y) {
					start = new(deE, pos.y);
					end = new(dsS, dragStart.y);
				} else {
					start = new(deS, pos.y);
					end = new(dsE, dragStart.y);
				}

				SetSingleCaret(start, end);
			} else {
				Vector2Int start;
				Vector2Int end;

				if (dragStart.y == pos.y) {
					start = new(lines[pos.y].Content.Length, pos.y);
					end = new(0, pos.y);
				} else
				if (dragStart.y < pos.y) {
					start = new(lines[pos.y].Content.Length, pos.y);
					end = new(0, dragStart.y);
				} else {
					start = new(0, pos.y);
					end = new(lines[dragStart.y].Content.Length, dragStart.y);
				}

				SetSingleCaret(start, end);
			}
		}
	}

	void SetSingleCaret(Vector2Int head, Vector2Int tail) {
		if (carets.Count != 1) {
			foreach (var caret in carets)
				caret.Destroy();
			carets.Clear();

			Caret singleCaret = new(this);
			singleCaret.Initialize();
			carets.Add(singleCaret);
		}

		carets[0].UpdatePos(head, tail);
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

		RectTransform rt = lines[line].Components[0] as RectTransform;

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

	// returns in char space
	Vector2Int? CurrentMouseHover() {
		(int line, int hoverIndex) = FindLineHoveringOver();
		if (hoverIndex == -1) return null;

		int index = GetCharIndexAtWorldSpacePosition(line, hoverIndex);
		if (index == -1) return null;

		return new(index, line);
	}

	(int lineIndex, int hoverIndex) FindLineHoveringOver() {
		if (lines == null || lines[0].Components == null) return (-1, -1);

		for (int i = 0; i < lines.Count; i++) {
			RectTransform contents = lines[i].Components[0] as RectTransform;
			int index = UIHovers.hovers.IndexOf(contents);
			if (index != -1) return (i, index);
		}
		return (-1, -1);
	}

	public (RectTransform rt, float t) GetLocation(Vector2Int vec) {
		return (
			lines[vec.y].Components[0] as RectTransform,
			lines[vec.y].IndexTs[vec.x]);
	}
}