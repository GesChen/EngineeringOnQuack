using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using JetBrains.Annotations;
using PlasticPipe.PlasticProtocol.Messages;
using log4net;

public class ScriptEditor : MonoBehaviour {
	public List<Line> lines;

	public ScrollWindow scroll;
	public CustomVerticalLayout lineContentVerticalLayout;
	public RectTransform lineContentContainer;
	public CustomVerticalLayout lineNumbersVerticalLayout;
	[HideInNormalInspector] public RectTransform lineNumbersRect;

	public SyntaxHighlighter syntaxHighlighter;
	public List<Caret> carets = new();
	public int headCaretI = 0;

	[Header("temporary local config options, should move to global config soon")]
	public float numberToContentSpace;
	public TMP_FontAsset font;
	public float fontSize;
	public Color selectionColor;

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

	void Start() {
		lineNumbersRect = lineNumbersVerticalLayout.GetComponent<RectTransform>();
	}

	#region Loading/Generation
	float charUVAmount;

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

			pos += charUVAmount * (c == '\t' ? Config.Language.SpacesPerTab : 1);
		}

		//// pos isnt gonna be 1 but need to add it again to be able to select last item still
		//TtoIndex.Add(pos);

		// experiment
		while (pos < 1) {
			TtoIndex.Add(pos);
			pos += charUVAmount;
		}
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

	int tabIndexToSpaceCount(int i) => HF.Mod(-i - 1, Config.Language.SpacesPerTab) + 1;
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
	#endregion

	void Update() {
		HandleMouseInput();
		HandleKeyboardInput();
		UpdateCarets();
	}

	#region Caret Utilities
	void SetCaretCount(int n) {
		if (carets.Count != n) {
			foreach (var caret in carets)
				caret.Destroy();
			carets.Clear();

			for (int i = 0; i < n; i++) {
				Caret singleCaret = new(this);
				singleCaret.Initialize();
				carets.Add(singleCaret);
			}
		}
	}
	void SetSingleCaret(Vector2Int head, Vector2Int tail) {
		SetCaretCount(1);

		carets[0].UpdatePos(head, tail);
	}

	void SetMultipleCarets(List<(Vector2Int head, Vector2Int tail)> positions, int headCaretIndex) {
		SetCaretCount(positions.Count);

		for (int i = 0; i < positions.Count; i++) {
			carets[i].UpdatePos(positions[i].head, positions[i].tail);
		}

		headCaretI = headCaretIndex;
	}

	void UpdateCarets() {
		foreach (var caret in carets)
			caret.Update();
	}

	#endregion

	#region Mouse Input

	void HandleMouseInput() {
		bool clickedThisFrame = Controls.IM.Mouse.Left.WasPressedThisFrame();
		Vector2Int? mousePos = CurrentMouseHoverUnclamped();
		if (!mousePos.HasValue) return;

		DetectExtraClicks(clickedThisFrame, mousePos.Value);
		HandleDrag(clickedThisFrame, ClampPosition(mousePos.Value), mousePos.Value);
	}

	float lastClickTime;
	Vector2Int lastClickPos;
	int clicksInARow = 0;
	void DetectExtraClicks(bool clickedThisFrame, Vector2Int pos) {
		if (clickedThisFrame) {
			if (Time.time - lastClickTime < Config.ScriptEditor.MultiClickThresholdMs / 1000 &&
			lastClickPos == pos) {
				clicksInARow++;
			} else {
				clicksInARow = 1;
			}

			lastClickTime = Time.time;
			lastClickPos = pos;
		}
	}

	(int start, int end) DoubleClickWordAt(Vector2Int pos) {
		if (string.IsNullOrWhiteSpace(lines[pos.y].Content)) return (0, 0); // index errors everywhere

		var colors = lines[pos.y].ColorsOriginal; // does this store a reference to it or copy the array?
		string line = lines[pos.y].Content;

		// custom case for symbols
		// returns just that symbol
		if (pos.x < colors.Length && colors[pos.x] == SyntaxHighlighter.Types.symbol)
			return (pos.x, pos.x);

		static int chartype(char c) {
			if (char.IsLetterOrDigit(c)) return 0;
			if (char.IsSymbol(c)) return 1;
			if (char.IsWhiteSpace(c)) return 2;
			return -1;
		}

		var leftColorType = 
			pos.x > 0 
			? colors[pos.x - 1]
			: SyntaxHighlighter.Types.unassigned;

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

		int leftCharType = 
			pos.x > 0 
			? chartype(line[pos.x - 1])
			: -1;

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
		if (left != 0) left++;

		int right = pos.x;
		while (right < colors.Length && 
			colors[right] == findColorType &&
			chartype(line[right]) == findCharType)
			right++;
		//if (right == colors.Length) right--;

		return (left, right);
	}

	bool dragging = false;
	Vector2Int dragStart;
	Vector2Int dragStartUnclamped;
	void HandleDrag(bool clickedThisFrame, Vector2Int pos, Vector2Int posUnclamped) {
		if (clickedThisFrame) { // down
			dragging = true;

			if (!Controls.Keyboard.Modifiers.Shift) {
				dragStart = pos;
				dragStartUnclamped = posUnclamped;
			}
		} else
		if (Controls.IM.Mouse.Left.WasReleasedThisFrame()) {
			dragging = false;
		}

		if (dragging) {
			// will have to add alt and shift and stuff soon
			// for now this is just normal

			bool doubleClickCondition = Controls.Keyboard.Modifiers.Ctrl;

			if (clicksInARow == 1 && !doubleClickCondition) {
				if (!Controls.Keyboard.Modifiers.Alt) {
					// normal dragging
					SetSingleCaret(pos, dragStart);
					carets[0].DesiredCol = ColumnOfPosition(pos);
				} else {
					int startLine = dragStartUnclamped.y;
					int startCol = ColumnOfPosition(dragStartUnclamped);
					
					int endLine = posUnclamped.y;
					int endCol = ColumnOfPosition(posUnclamped);

					if (endLine < startLine)
						(endLine, startLine) = (startLine, endLine);

					List<(Vector2Int head, Vector2Int tail)> carets = new();
					for (int i = startLine; i <= endLine; i++) {
						Vector2Int head = new(PositionOfColumn(endCol, i), i);
						Vector2Int tail = new(PositionOfColumn(startCol, i), i);

						carets.Add((head, tail));
					}

					SetMultipleCarets(carets, carets.Count - 1);
				}
			} else 
			if (clicksInARow == 2 || doubleClickCondition) {
				(int dsS, int dsE) = DoubleClickWordAt(dragStart);
				(int deS, int deE) = DoubleClickWordAt(pos);

				Vector2Int start;
				Vector2Int end;

				if (dragStart.y == pos.y) {
					start = new(Mathf.Max(dsE, deE), pos.y);
					end = new(Mathf.Min(dsS, deS), pos.y);

					if (dragStart.x > pos.x)
						(start, end) = (end, start); // lol theres gotta be a better way but it works
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

	Vector2Int ClampPosition(Vector2Int pos) {
		return new(Mathf.Clamp(pos.x, 0, lines[pos.y].Content.Length), pos.y);
	}
	
	// returns in char space
	Vector2Int? CurrentMouseHoverUnclamped() {
		(int line, int hoverIndex) = FindLineHoveringOver();
		if (hoverIndex == -1) return null;

		int index = GetCharIndexAtWorldSpacePositionUnclamped(line, hoverIndex);
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
	
	// long ass name
	int GetCharIndexAtWorldSpacePositionUnclamped(int line, int hoverIndex) {

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
			float dist = Mathf.Abs(ts[i] - t);
			if (dist < closestDist) {
				closestDist = dist;
				charIndex = i;
			}
		}

		return charIndex;
	}

	#endregion

	#region Keyboard Input

	void HandleKeyboardInput() {
		// normal arrow keys only for now, move to seperate if needed
		Vector2Int movement = Vector2Int.zero;
		if (Controls.IsUsed(Key.UpArrow)) movement.y--;
		if (Controls.IsUsed(Key.DownArrow)) movement.y++;
		if (Controls.IsUsed(Key.LeftArrow)) movement.x--;
		if (Controls.IsUsed(Key.RightArrow)) movement.x++;

		if (movement.sqrMagnitude == 0) return;

		foreach(Caret c in carets) {
			c.MoveHead(movement);

			if (Controls.Keyboard.Modifiers.Ctrl) {
				(int start, int end) = DoubleClickWordAt(c.head);

				c.ResetBlink();
				c.SetHead(new(movement.x > 0 ? end : start, c.head.y));
				c.Update();
			}

			if (!Controls.Keyboard.Modifiers.Shift) {
				c.MatchTail();
			}
		}
	}

	#endregion

	#region Position Utils
	public (RectTransform rt, float t) GetLocation(Vector2Int vec) {
		return (
			lines[vec.y].Components[0] as RectTransform,
			lines[vec.y].IndexTs[vec.x]);
	}

	public int ColumnOfPosition(Vector2Int pos) {
		string line = lines[pos.y].Content;
		if (pos.x >= line.Length) 
			return ColumnOfPositionOvershoot(pos);
		
		int col = 0;
		for (int i = 0; i < pos.x; i++) {
			if (line[i] == '\t') col += tabIndexToSpaceCount(col);
			else col++;
		}
		return col;
	}

	public int ColumnOfPositionOvershoot(Vector2Int pos) {
		string line = lines[pos.y].Content;
		int col = 0;
		foreach (char c in line) {
			if (c == '\t') col += tabIndexToSpaceCount(col);
			else col++; 
		}

		col += pos.x - line.Length;
		return col;
	}

	public int PositionOfColumn(int col, int line) {
		if (col == 0) return 0;

		string content = lines[line].Content;
		int pos = 0;
		int testCol = 0;
		while (pos < content.Length) {
			if (content[pos] == '\t') {
				testCol += tabIndexToSpaceCount(testCol);
			} else {
				testCol++;
			}
			pos++;

			if (testCol >= col) return pos;
		}

		while (testCol < col) { // will = at the end
			pos++;
			testCol++;
		}

		return pos;
	}

	#endregion
}