using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text;
using UnityEditor;
// is this too many usings?
// answer: yea prolly

// look i KNOW its a god class but i literally can NOT figure out
// how to separate them without making each separate class have to reference 
// main. for every single use of lines or carets or whatever OK
// "ill fix it later" 4-23-25 

public class ScriptEditor : MonoBehaviour {
	public List<Line> lines;
	List<LineNumber> lineNumbers;

	public SEScrollWindow scroll;
	public CustomVerticalLayout lineContentVerticalLayout;
	public RectTransform lineContentContainer;
	public CustomVerticalLayout lineNumbersVerticalLayout;
	[HideInNormalInspector] public RectTransform lineNumbersRect;

	public SyntaxHighlighter syntaxHighlighter;
	public List<Caret> carets = new();
	int headCaretI = 0;
	int tailCaretI = 0;

	[Header("temporary local config options, should move to global config soon")]
	public float numberToContentSpace;
	public TMP_FontAsset font;
	public float fontSize;
	public Color selectionColor;
	public Color test;
	public int xCursorScreenMarginChars;
	public int yCursorScreenMarginLines;

	#region Line Classes
	public class Line {
		public string Content;
		public string ProcessedContent;
		public List<float> IndexTs;
		public (
			RectTransform LineContent, 
			TextMeshProUGUI LineText) Components;
		public bool Realised;
		public SyntaxHighlighter.Types[] ColorsSpaces;
		public SyntaxHighlighter.Types[] ColorsOriginal;
		public Context ContextAfterLine; // context at the end of the line after everything is paresed
	}

	public class LineNumber {
		public int Number;
		public RectTransform Rect;
	}
	#endregion

	#region Context Classes
	[HideInInspector]
	// changed to class so maybe each line's localcontext copy will reference the same variable objects
	public class LCVariable { // it would be inside localcontext if it wasnt so fucking deep
		public string Name;

		public enum Types {
			Normal,
			MembFunc,
			Type
		}
		public Types Type;
		public int IndentLevel;

		public override string ToString() {
			return $"% {Name} ({Type}) at {IndentLevel}";
		}
	}
	public class Context {
		public List<LCVariable> Variables;
		public bool InComment;

		public Context() {
			Variables = new();
			InComment = false;
		}
		public Context(Context original) {
			Variables = original.Variables.ToList(); // copy individual items if needed
			InComment = original.InComment;
		}
	}
	public struct ContextSignature { // for faster comparison
		public string VarNames;
		public bool InComment;
	}
	public ContextSignature ContextToSignature(Context context) => new() {
		VarNames = string.Join(' ', context.Variables.Select(v => v.Name)),
		InComment = context.InComment
	};

	Context LC;
	#endregion

	void Start() {
		lineNumbersRect = lineNumbersVerticalLayout.GetComponent<RectTransform>();
		SubscribeToShortcuts();
	}
	
	void Update() {
		HandleMouseNavigation();
		HandleKeyboardNavgation();
		UpdateCarets();
		HandleTyping();
	}

	#region Loading/Generation
	float longestLineWidth;
	int longestLine;
	float charUVAmount;
	float lineNumberWidth;
	float charWidth;
	[HideInNormalInspector] public float allLinesHeight;

	public void Load(string[] strLines) {
		Clear();

		// recalculate max line number width
		TextMeshProUGUI testingText = lineContentVerticalLayout.gameObject.AddComponent(typeof(TextMeshProUGUI)) as TextMeshProUGUI;
		testingText.font = font;
		testingText.fontSize = fontSize;
		Vector2 numberSize = HF.TextWidthExact(strLines.Length.ToString(), testingText);
		Destroy(testingText);

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
		foreach (string line in strLines) {
			Line newLine = GenerateNewLine(line);
			lines.Add(newLine);
		}

		// generate line numbers
		for (int i = 0; i < lines.Count; i++) {
			LineNumber newLN = GenerateNewNumber(i + 1);
			lineNumbers.Add(newLN);
		}

		RecalculateAll();

		SetSingleCaret(new(0, 0), new(0, 0));
	}

	void RecalculateAll() {
		// scale all containers to max width
		RecalculateLongest();
		RecalculateCharUVA();
		ScaleAllContainersToMax();

		// calculate ts (charuv must have a value)
		CalculateAllTs();
	}

	void Clear() {
		// delete all existing lines

		lines ??= new();
		foreach (Line line in lines) {
			if (line.Components.LineContent != null) {
				Destroy(line.Components.LineContent.gameObject); // line contents
			}
		}
		lines.Clear();

		lineNumbers ??= new();
		foreach (LineNumber ln in lineNumbers) {
			if (ln.Rect != null)
				Destroy(ln.Rect.gameObject);
		}
		lineNumbers.Clear();
	}

	void RecalculateLongest() {
		longestLineWidth = -1;
		longestLine = -1;
		for (int i = 0; i < lines.Count; i++) {
			Line line = lines[i];
			float width = LineWidth(line);

			if (width > longestLineWidth) {
				longestLineWidth = width;
				longestLine = i;
			}
		}
	}

	float LineWidth(Line line) {
		line.Components.LineText.ForceMeshUpdate();
		return LayoutUtility.GetPreferredWidth(line.Components.LineContent);
	}

	void RecalculateCharUVA() {
		charUVAmount = 1f / lines[longestLine].ProcessedContent.Length;
		charWidth = longestLineWidth / lines[longestLine].ProcessedContent.Length;
	}

	void ScaleAllContainersToMax() {
		lines.ForEach(l => l.Components.LineContent
			.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, longestLineWidth));
	}

	void CalculateAllTs() {
		for (int i = 0; i < lines.Count; i++) {
			lines[i].IndexTs = CalculateTs(i);
		}
	}

	List<float> CalculateTs(int i) {
		// hopefully this loop isnt too slow for being called once
		// can be precomputed if needed
		// index = index of cursor location, basically 1 before the actual char

		List<float> TtoIndex = new();
		float pos = 0;
		int charPos = 0;

		for (int l = 0; l < lines[i].Content.Length; l++) {
			char c = lines[i].Content[l];
			TtoIndex.Add(pos);

			int charIncrease = c == '\t' ? TabIndexToSpaceCount(charPos) : 1;
			pos += charUVAmount * charIncrease;
			charPos += charIncrease;
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

	Line GenerateNewLine(string content) { // ~1ms
		Line line = new() {
			Content = content
		};

		// convert tabs to aligned spaces (for tmpro, original is unchanged)
		string processed = ConvertTabsToSpaces(line);
		line.ProcessedContent = processed;

		// colorize
		var colors = syntaxHighlighter.ParseLineToColorList(processed, LC);
		line.ColorsSpaces = colors;

		// keep copy after execution for later use
		line.ContextAfterLine = new(LC);

		// need to store a usable copy based on tabs instead of spaces
		var tabsConvertedBack = RevertSpacesToTabs(colors, processed, line.Content);
		line.ColorsOriginal = tabsConvertedBack;

		// represent tabs and spaces
		//processed = MakeWhiteSpaceVisible(processed);

		// tag line
		processed = syntaxHighlighter.TagLine(processed, colors);

		// make actual line content
		(GameObject _, TextMeshProUGUI LCText, RectTransform LCRect)
			= NewText(
				"Line Content",
				$"{processed}",
				lineContentVerticalLayout.transform,
				TextAlignmentOptions.Left,
				0); // temp set width to zero, recalculate later

		// setup line container rect
		LCRect.anchorMin = new(0, 1);
		LCRect.anchorMax = new(0, 1);
		LCRect.pivot = new(0, 1);

		LCRect.localPosition = new(lineNumberWidth + numberToContentSpace, 0);

		line.Components.LineContent = LCRect;
		line.Components.LineText = LCText;

		line.Realised = true;

		return line;
	}

	LineNumber GenerateNewNumber(int number) {
		(GameObject _, TextMeshProUGUI _, RectTransform NRect)
			= NewText(
				$"Line Number  ({number})",
				number.ToString(),
				lineNumbersVerticalLayout.transform,
				TextAlignmentOptions.Right,
				lineNumberWidth);
		NRect.localPosition = Vector2.zero;

		return new() {
			Number = number,
			Rect = NRect
		};
	}

	void UpdateLine(int lineIndex, bool forceUpdateNextToo = true) {
		Line line = lines[lineIndex];

		Context context =
			lineIndex == 0
			? new()
			: new(lines[lineIndex - 1].ContextAfterLine);

		ContextSignature preRunSignature =
			ContextToSignature(line.ContextAfterLine);

		string processed = ConvertTabsToSpaces(line);
		line.ProcessedContent = processed;

		var colors = syntaxHighlighter.ParseLineToColorList(processed, context);
		line.ColorsSpaces = colors;

		// if the context has changed

		if (!ContextToSignature(context).Equals(preRunSignature) ||
			forceUpdateNextToo) {
			line.ContextAfterLine = context;

			// then propgoate updates down the lines
			if (lineIndex != lines.Count - 1)
				UpdateLine(lineIndex + 1, false);
		}

		var tabsConvertedBack = RevertSpacesToTabs(colors, processed, line.Content);
		line.ColorsOriginal = tabsConvertedBack;

		// tag line
		processed = syntaxHighlighter.TagLine(processed, colors);
		line.Components.LineText.text = processed;

		// manually check if longer than longest
		float width = LineWidth(line);
		if (width > longestLineWidth) { // update everything if this is new longset
			longestLine = lineIndex;
			longestLineWidth = width;
		
			RecalculateCharUVA();
			ScaleAllContainersToMax();

			// recalculate all ts if uvamount changed
			CalculateAllTs();
		} else {
			// SCALE TO MAX!!!
			line.Components.LineContent
				.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, longestLineWidth);

			// otherwise just do this ones ts
			line.IndexTs = CalculateTs(lineIndex);
		}
	}

	void DeleteLine(int index) {
		Line line = lines[index];

		// destroy the line
		Destroy(line.Components.LineContent.gameObject);
		lines.RemoveAt(index);

		// decrement a line number
		Destroy(lineNumbers[^1].Rect.gameObject);
		lineNumbers.RemoveAt(lineNumbers.Count - 1);
	}

	string ConvertTabsToSpaces(Line line) {
		return ConvertTabsToSpaces(line.Content);
	}

	int TabIndexToSpaceCount(int i) => HF.Mod(-i - 1, Config.Language.SpacesPerTab) + 1;
	string ConvertTabsToSpaces(string line) {
		string tabsToSpaces = line;
		for (int i = 0; i < tabsToSpaces.Length; i++) {
			char c = tabsToSpaces[i];
			if (c == '\t') {
				int num = TabIndexToSpaceCount(i);
				tabsToSpaces = HF.ReplaceSection(tabsToSpaces, i, i, new string(' ', num));
			}
		}

		return tabsToSpaces;
	}

	string MakeWhiteSpaceVisible(string original) {

		StringBuilder sb = new();
		
		for (int i = 0; i < original.Length; i++) {
			char c = original[i];
			if (c == ' ') {
				sb.Append('•');
			} else 
			if (c == '\t') {
			} else {
				sb.Append(c);
			}
		}

		return sb.ToString();
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
				ci += TabIndexToSpaceCount(ci);
			else
				ci++;

			oi++;
		}
		return reconstructed;
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

	#region Caret Utilities
	void ResetCarets() {
		if (carets.Count == 0) return; // prolly doesnt help lmao

		foreach (var caret in carets)
			caret.Destroy();
		carets.Clear();
	}

	Caret SetSingleCaret(Vector2Int head, Vector2Int tail) {
		ResetCarets();

		var newCaret = AddNewCaret(head, tail);

		return newCaret;
	}

	List<Caret> AddMultipleCarets(List<(Vector2Int head, Vector2Int tail)> positions) {

		List<Caret> newCarets = new();
		for (int i = 0; i < positions.Count; i++) {
			var newCaret = AddNewCaret(positions[i].head, positions[i].tail);
			newCarets.Add(newCaret);
		}

		return newCarets;
	}

	Caret AddNewCaret(Vector2Int head, Vector2Int tail) {
		Caret newCaret = new(this);
		newCaret.Initialize();
		newCaret.UpdatePos(head, tail);
		carets.Add(newCaret);
		return newCaret;
	}

	void RemoveCaret(int i) {
		carets[i].Destroy();
		carets.RemoveAt(i);
	}

	void UpdateCarets() {
		CheckForOverlaps();
		PushTails();

		foreach (var caret in carets)
			caret.Update();

		for (int i = 0; i < carets.Count; i++)
			carets[i].isHeadCaret = i == headCaretI;
	}

	void CheckForOverlaps() {
		var seen = new Dictionary<(Vector2Int, Vector2Int), int>();
		var duplicateIndices = new List<int>();

		for (int i = 0; i < carets.Count; i++) {
			var key = (carets[i].head, carets[i].tail);

			if (seen.ContainsKey(key)) {
				duplicateIndices.Add(i);
			} else {
				seen[key] = i; // store index of first occurrence
			}
		}

		duplicateIndices.Reverse();

		foreach (int i in duplicateIndices) {
			carets[i].Destroy();
			carets.RemoveAt(i);

			if (headCaretI >= i) headCaretI--;
			if (tailCaretI >= i) tailCaretI--;
		}
	}

	void PushTails() {
		for (int i = 0; i < carets.Count; i++) {
			Caret caret = carets[i];
			for (int j = 0; j < carets.Count; j++) {
				if (i == j) continue;

				Caret check = carets[j];
				if (PosInCaretSelection(caret.head, check)) {
					check.tail = caret.head; // push
				}
			}
		}
	}

	bool PosInCaretSelection(Vector2Int pos, Caret caret) {
		if (!caret.HasSelection) return false;
		
		bool tailBehind = caret.tail.y < caret.head.y ||
			(caret.tail.y == caret.head.y && caret.tail.x < caret.head.x);

		if (tailBehind
			? (pos.y > caret.tail.y && pos.y < caret.head.y)
			: (pos.y > caret.head.y && pos.y < caret.tail.y))
			return true;

		if (pos.y == caret.head.y && pos.y == caret.tail.y) {
			if (tailBehind
				? (pos.x >= caret.tail.x && pos.x <= caret.head.x)
				: (pos.x <= caret.tail.x && pos.x >= caret.head.x))
				return true;
			return false;
		}

		if (pos.y == caret.tail.y &&
			(tailBehind ? pos.x >= caret.tail.x : pos.x <= caret.tail.x))
			return true;

		if (pos.y == caret.head.y &&
			(tailBehind ? pos.x <= caret.head.x : pos.x >= caret.head.x))
			return true;

		return false;
	}

	void KeepHeadCaretHeadOnScreen() {
		Caret head = carets[headCaretI];
		Vector2 pos = LocalPositionOfCaretHead(head);

		int count = 0;
		var offset = CheckCursorOffsets(pos);
		while (offset != (0, 0)) {
			if (offset.x > 0)		scroll.ManuallyScrollX(charWidth);
			else if (offset.x < 0)	scroll.ManuallyScrollX(-charWidth);
			if (offset.y > 0)		scroll.ManuallyScrollY(allLinesHeight);
			else if (offset.y < 0)	scroll.ManuallyScrollY(-allLinesHeight);

			offset = CheckCursorOffsets(pos);

			if (++count > 10) break; // shouldn't be THIS off screen hopefully :(
		}
	}

	public Vector2 LocalPositionOfCaretHead(Caret c) {
		float x = c.LocalX;
		float y = allLinesHeight * c.head.y;

		return new(x, y);
	}

	public (int x, int y) CheckCursorOffsets(Vector2 pos) {
		pos -= scroll.CurrentScrollAmount;

		// definition of insanity
		return // seriously why are we using ternary here :(((((
		(
			pos.x < xCursorScreenMarginChars * charWidth
				? -1
			: (
			pos.x > lineContentContainer.rect.width - xCursorScreenMarginChars * charWidth
				? 1
			: 0)
		,
			pos.y < yCursorScreenMarginLines * allLinesHeight
				? -1
			: (
			pos.y > lineContentContainer.rect.height - yCursorScreenMarginLines * allLinesHeight
				? 1
			: 0)
		);
	}
	#endregion

	#region Mouse Input

	void HandleMouseNavigation() {
		Vector2Int? mousePos = CurrentMouseHoverUnclamped();
		if (!mousePos.HasValue) return;

		DetectExtraClicks(mousePos.Value);
		HandleDrag(ClampPosition(mousePos.Value), mousePos.Value);
	}

	float lastClickTime;
	Vector2Int lastClickPos;
	int clicksInARow = 0;
	void DetectExtraClicks(Vector2Int pos) {
		if (Conatrols.IM.Mouse.Left.WasPressedThisFrame()) {
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

	(int startInc, int endExc) DoubleClickWordAt(Vector2Int pos) {
		if (string.IsNullOrWhiteSpace(lines[pos.y].Content)) return (0, 0); // index errors everywhere

		var colors = lines[pos.y].ColorsOriginal; // does this store a reference to it or copy the array?
		string line = lines[pos.y].Content;

		// custom case for symbols
		// returns just that symbol
		//if (pos.x < colors.Length && colors[pos.x] == SyntaxHighlighter.Types.symbol)
		//	return (pos.x, pos.x + 1);

		static int chartype(char c) {
			if (char.IsLetterOrDigit(c)) return 0;
			if (char.IsSymbol(c)) return 1;
			if (char.IsWhiteSpace(c)) return 2;
			return -1;
		}

		var findColorType =
			pos.x < colors.Length
			? colors[pos.x]
			: SyntaxHighlighter.Types.unassigned;

		int findCharType =
			pos.x < colors.Length
			? chartype(line[pos.x])
			: -1;

		// search
		int left = pos.x - 1;
		while (left > 0 &&
			colors[left] == findColorType &&
			chartype(line[left]) == findCharType)
			left--;

		// honestly idk either its just made to work
		if (left < 0 || chartype(line[left]) != findCharType) left++;

		int right = pos.x;
		while (right < colors.Length &&
			colors[right] == findColorType &&
			chartype(line[right]) == findCharType)
			right++;
		//if (right == colors.Length) right--;

		return (left, right);
	}

	bool dragging = false;
	bool dragStartedWithAlt = false;
	Vector2Int dragStart;
	Vector2Int dragStartUnclamped;
	bool boxCleared = false;
	void HandleDrag(Vector2Int pos, Vector2Int posUnclamped) {
		if (Conatrols.IM.Mouse.Left.WasPressedThisFrame()) { // down
			dragging = true;

			if (!Conatrols.Keyboard.Modifiers.Shift) {
				dragStart = pos;
				dragStartUnclamped = posUnclamped;

				dragStartedWithAlt = Conatrols.Keyboard.Modifiers.Alt;
			}

			if (Conatrols.Keyboard.Modifiers.Ctrl && Conatrols.Keyboard.Modifiers.Alt) {
				// add more carets
				AddNewCaret(pos, pos);
				headCaretI = carets.Count - 1;
				tailCaretI = headCaretI;
			} else { // single select
				SetSingleCaret(pos, pos);
				headCaretI = 0;
				tailCaretI = 0;
			}
		} else
		if (Conatrols.IM.Mouse.Left.WasReleasedThisFrame()) {
			dragging = false;
		}
		if (dragging) {
			if (Conatrols.Keyboard.Modifiers.Ctrl &&
				Conatrols.Keyboard.Modifiers.Shift &&
				Conatrols.Keyboard.Modifiers.Alt)
				return; // do nothing with all 3 

			bool doubleClickCondition = 
				Conatrols.Keyboard.Modifiers.Ctrl && 
				!Conatrols.Keyboard.Modifiers.Alt; // dont want during adding

			bool boxCondition =	// otherwise overrides ctrl alt adding news, TODO fix this later idk
				Conatrols.Keyboard.Modifiers.Alt && // alt dragging
				!Conatrols.Keyboard.Modifiers.Ctrl;

			if (!boxCondition && !boxCleared) {
				// box stop
				SetSingleCaret(pos, dragStart);
				headCaretI = 0;
				tailCaretI = 0;

				boxCleared = true;
			}

			if (boxCondition) {
				ResetCarets();

				Vector2Int boxStart =
					dragStartedWithAlt
					? dragStartUnclamped
					: dragStart;

				int startLine = boxStart.y;
				int startCol = ColumnOfPosition(boxStart);

				int endLine = posUnclamped.y;
				int endCol = ColumnOfPosition(posUnclamped);

				bool down = endLine < startLine;
				if (down)
					(endLine, startLine) = (startLine, endLine);

				List<(Vector2Int head, Vector2Int tail)> newCarets = new();
				for (int i = startLine; i <= endLine; i++) {
					Vector2Int head = new(PositionOfColumn(endCol, i), i);
					Vector2Int tail = new(PositionOfColumn(startCol, i), i);

					newCarets.Add((head, tail));
				}

				boxCleared = false;

				AddMultipleCarets(newCarets);

				tailCaretI = newCarets.Count - 1;
				headCaretI = 0;

				if (!down)
					(tailCaretI, headCaretI) = (headCaretI, tailCaretI);

				boxEditing = true;
			}
			else if (clicksInARow == 1 && !doubleClickCondition) {
				// normal dragging
				SetCurrentCaret(pos, dragStart);
				carets[headCaretI].DesiredCol = ColumnOfPosition(pos);
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

				SetCurrentCaret(start, end);
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

				SetCurrentCaret(start, end);
			}
		}
	}

	void SetCurrentCaret(Vector2Int head, Vector2Int tail) {
		carets[headCaretI].UpdatePos(head, tail);
	}

	// returns in char space
	Vector2Int? CurrentMouseHoverUnclamped() {
		int line = FindLineHoveringOver();
		if (line == -1) return null;

		int index = GetCharIndexAtWorldSpacePositionUnclamped(line);
		if (index == -1) return null;

		return new(index, line);
	}

	int FindLineHoveringOver() {
		if (lines == null || lines[0].Components.LineContent == null) return -1;

		for (int i = 0; i < lines.Count; i++) {
			RectTransform contents = lines[i].Components.LineContent;
			//if (UIHovers.CheckFirstAllowing(contents, lineContentVerticalLayout.transform)) 
			if (UIHovers.CheckIgnoreOrder(contents)) 
				return i;
		}
		return -1;
	}
	
	// long ass name
	int GetCharIndexAtWorldSpacePositionUnclamped(int line) {

		// not sure why this happens but it just does idk
		// ok this was broken before idk its fixed now??
		if (!UIHovers.CheckFirstAllowing(lines[line].Components.LineContent, lineContentVerticalLayout.transform))
		//if (!UIHovers.CheckIgnoreOrder(lines[line].Components.LineContent))
			return -1;

		RectTransform rt = lines[line].Components.LineContent;

		Vector3[] corners = new Vector3[4];
		rt.GetWorldCorners(corners);

		int contentsIndex = UIHovers.hovers.IndexOf(rt);

		RaycastResult result = UIHovers.results[contentsIndex];

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

	#region Keyboard Navigation

	bool boxEditing = false;
	void HandleKeyboardNavgation() {
		if (Conatrols.IsPressed(Key.Escape))
			Escape();

		// normal arrow keys only for now, move to seperate if needed
		Vector2Int movement = Vector2Int.zero;
		if (Conatrols.IsUsed(Key.UpArrow)) movement.y--;
		if (Conatrols.IsUsed(Key.DownArrow)) movement.y++;
		if (Conatrols.IsUsed(Key.LeftArrow)) movement.x--;
		if (Conatrols.IsUsed(Key.RightArrow)) movement.x++;

		if (movement.sqrMagnitude == 0) return;

		if (Conatrols.Keyboard.Modifiers.Shift &&
			Conatrols.Keyboard.Modifiers.Alt)
			boxEditing = true;
		if (!Conatrols.Keyboard.Modifiers.Shift ||
			Conatrols.Keyboard.Modifiers.Ctrl)
			boxEditing = false;

		if (Conatrols.Keyboard.Modifiers.Ctrl &&
			movement.y != 0) {
			scroll.ManuallyScrollY(Mathf.Sign(movement.y) * allLinesHeight);
			return;
		}

		if (boxEditing) { 
			HandleKeyboardBox(movement);
			return;
		}
		foreach (Caret c in carets) {
			if (Conatrols.Keyboard.Modifiers.Ctrl && 
				Conatrols.Keyboard.Modifiers.Shift &&
				Conatrols.Keyboard.Modifiers.Alt)
				continue; // do nothing with all 3

			// this logic is sooooo fucked

			if (Conatrols.Keyboard.Modifiers.Alt && 
				!Conatrols.Keyboard.Modifiers.Ctrl) {
				AltMove(c, movement);
			} else {
				c.MoveHead(movement);
			}

			if (Conatrols.Keyboard.Modifiers.Ctrl) {
				(int start, int end) = DoubleClickWordAt(new(
					Mathf.Max(0, c.head.x - (movement.x > 0 ? 1 : 0)), 
					c.head.y));

				c.SetHead(new(
					movement.x > 0 
					? end 
					: start, c.head.y));
			}
			

			if (!Conatrols.Keyboard.Modifiers.Shift) {
				c.MatchTail();
			}

			c.Update();
			c.ResetBlink();
		}

		KeepHeadCaretHeadOnScreen();
	}

	void AltMove(Caret c, Vector2Int movement) {
		if (movement.x != 0) {
			//if (c.HasSelection) {
				//HorizontalMoveSelection(c, movement);
			//} else {
				int pos =
					movement.x > 0
					? lines[c.head.y].Content.Length // end if right
					: 0; // start if left

				c.SetHead(new(pos, c.head.y));
			//}
		}

		if (movement.y < 0 && c.head.y > 0) {

			// swap contents
			(lines[c.head.y].Content, lines[c.head.y - 1].Content) =
				(lines[c.head.y - 1].Content, lines[c.head.y].Content);

			UpdateLine(c.head.y);
			UpdateLine(c.head.y - 1);

			// move with
			c.head.y--;
			c.MatchTail();
		} else
		if (movement.y > 0 && c.head.y < lines.Count - 1) {

			// swap contents
			(lines[c.head.y].Content, lines[c.head.y + 1].Content) =
				(lines[c.head.y + 1].Content, lines[c.head.y].Content);

			UpdateLine(c.head.y);
			UpdateLine(c.head.y + 1);

			// move with
			c.head.y++;
			c.MatchTail();
		}
	}

	// if you like suffering, then make this work. 
	void HorizontalMoveSelection(Caret c, Vector2Int movement) {

		// move left and right
		// make it go between lines if you really wanna deal with those bugs

		// im doing this a dumb way, more bugs. but . idk im lazy

		bool forward = movement.x > 0;
		int shift = forward ? 1 : -1; // add ctrl and whatever other bullshit later

		if (c.head.y == c.tail.y) {
			Line thisLine = lines[c.head.y];

			int minX = Mathf.Min(c.head.x, c.tail.x);
			int maxX = Mathf.Max(c.head.x, c.tail.x);

			// stop at ends 
			if ((movement.x > 0 && c.head.y == lines.Count - 1 && maxX >= thisLine.Content.Length - 1) ||
				(movement.x < 0 && c.head.y == 0 && minX <= 0))
				return;

			(string shiftedString, bool overflows, string overRegion) =
				HF.ShiftRegion(thisLine.Content, minX, maxX - 1, shift);

			thisLine.Content = shiftedString;
			UpdateLine(c.head.y);

			bool tailBehind = c.tail.x < c.head.x;

			// yeah fuck never nesting we stacking nests to the ceiling rn
			// honestly im too lazy to make the code look good rn
			// sooo easily dryable tho
			if (overflows) {
				if (forward) {
					Line nextLine = lines[c.head.y + 1];
					nextLine.Content = nextLine.Content.Insert(0, overRegion);
					UpdateLine(c.head.y + 1);

					if (tailBehind) {
						c.head.x = overRegion.Length;
						c.head.y++;

						c.tail.x += shift;
					} else {
						c.tail.x = 1;
						c.tail.y++;

						c.head.x += shift;
					}
				} else {
					Line prevLine = lines[c.head.y - 1];
					prevLine.Content += overRegion;
					UpdateLine(c.head.y - 1);

					if (tailBehind) {
						c.tail.x = prevLine.Content.Length - overRegion.Length;
						c.tail.y--;

						c.head.x += shift;
					} else {
						c.head.x = prevLine.Content.Length - overRegion.Length;
						c.head.y--;

						c.tail.x += shift;
					}
				}

				c.WrapHead();
				c.WrapTail();

			} else {
				c.head.x += shift;
				c.tail.x += shift;
			}
		} else {
			string selectedFullLines = string.Join("", c.boxes.Select(b => lines[b.line].Content));
			List<int> lineLengths = c.boxes.Select(b => lines[b.line].Content.Length).ToList();

			int fullStartI = c.boxes[0].start;
			int fullEndI = 
				selectedFullLines.Length - 1 -
				(lines[c.boxes[^1].line].Content.Length - c.boxes[^1].end);

			string selection = selectedFullLines[fullStartI..(fullEndI + 1)];

			(string shiftedString, bool overflows, string overRegion) =
				HF.ShiftRegion(selectedFullLines, fullStartI, fullEndI, shift);
			
			if (!overflows) {
				int index = 0;
				for (int i = 0; i < c.boxes.Count; i++) {
					SelectionBox box = c.boxes[i];
					lines[box.line].Content = shiftedString[index..(index + lineLengths[i])];
					index += lineLengths[i];
				}

				UpdateLine(c.boxes[0].line);

				c.head.x += shift;
				c.tail.x += shift;

				c.WrapHead();
				c.WrapTail();
			} else {

			}
		}
	}

	void Escape() {
		Vector2Int headOfHead = carets[headCaretI].head;
		SetSingleCaret(headOfHead, headOfHead);
		headCaretI = 0;
		tailCaretI = 0;
	}

	void HandleKeyboardBox(Vector2Int movement) {
		Caret headCaret = carets[headCaretI];
		Caret tailCaret = carets[tailCaretI];

		int targetHeadCol = ColumnOfPosition(headCaret.head);
		int targetTailCol = ColumnOfPosition(tailCaret.tail);

		if (movement.x != 0) {
			targetHeadCol = ColumnOfPosition(headCaret.head + movement);

			foreach (Caret c in carets)
				c.SetHead(new(
					PositionOfColumn(targetHeadCol, c.head.y),
					c.head.y));
		}
		
		if (movement.y != 0) {
			if (headCaret.head.y == lines.Count - 1) return;

			int currentBoxVerticalDirection = // hope this is readable enough
				headCaret.head.y == tailCaret.head.y
				? 0
				: (
					headCaret.head.y < tailCaret.head.y
					? -1
					: 1
				);

			// simplify later, TODO DRY THIS!!!!!
			if (movement.y > 0) { // going up
				switch (currentBoxVerticalDirection) {
					case 0: // add go up from head
					case 1:
						int newY = headCaret.head.y + movement.y;
						Vector2Int head = new(
							PositionOfColumn(targetHeadCol, newY),
							newY);

						Vector2Int tail = new(
							PositionOfColumn(targetTailCol, newY),
							newY);

						AddNewCaret(head, tail);
						headCaretI++;
						break;
					case -1: // remove head to go up
						RemoveCaret(headCaretI);
						headCaretI--;
						break;
				}
			} else { // going down
				switch (currentBoxVerticalDirection) {
					case 0:
					case -1:
						int newY = headCaret.head.y + movement.y;
						Vector2Int head = new(
							PositionOfColumn(targetHeadCol, newY),
							newY);

						Vector2Int tail = new(
							PositionOfColumn(targetTailCol, newY),
							newY);

						AddNewCaret(head, tail);
						headCaretI++;
						break;
					case 1:
						RemoveCaret(headCaretI);
						headCaretI--;
						break;
				}
			}
		}

		foreach (Caret c in carets) {
			c.ResetBlink();
			c.Update();
		}
	}

	#endregion

	#region Text Editing
	// aw hell nah i really need to get this into a separate class :((((((((((
	void HandleTyping() {
		foreach (Caret c in carets) {
			HandleCaretTyping(c);
		}
	}

	void HandleCaretTyping(Caret c) {
		if (Conatrols.Keyboard.Presses.Count == 0) return;
		Line line = lines[c.head.y];

		// has to be a text key pressed (incs backspace etc)
		if (!Conatrols.Keyboard.Pressed.Any(k => Conatrols.Keyboard.All.TextKeys.Contains(k)))
			return;

		KeepHeadCaretHeadOnScreen();

		// force stop dragging;
		dragging = false;
		
		// deleters
		if (c.HasSelection)
			DeleteSelection(c, line);

		else if (Conatrols.IsUsed(Key.Backspace))
			Backspace(c, line);

		else if (Conatrols.IsUsed(Key.Delete))
			Delete(c, line);

		// adders
		if (Conatrols.IsUsed(Key.Enter))
			Enter(c, line);

		else if (Conatrols.IsUsed(Key.Tab))
			Tab(c, line);

		else
			NormallyType(c, line);
	}

	void DeleteSelection(Caret c, Line line) {
		// removes the selection. 

		if (c.IsSingleWholeLine) {
			DeleteWholeLine(c);
			return;
		} else
		if (c.head.y == c.tail.y) {

			int start = Mathf.Min(c.head.x, c.tail.x);
			int end = Mathf.Max(c.head.x, c.tail.x);

			if (start >= line.Content.Length) {
				//PadToIndex(start, c.head.y, line);
				// ok so this is kinda useless and breaks shitfuck everything. lets not use it? maybe?

				// js set the position and match gng :(
				c.head.x = start;
				c.MatchTail();

				return;

			} else if (end >= line.Content.Length) {
				line.Content = HF.RemoveSection(line.Content, start, line.Content.Length);
			} else {
				line.Content = HF.RemoveSection(line.Content, start, end);
			}

			c.head.x = start;

		} else {
			var boxes = c.boxes; // this MIGHT just help IDK

			// remove first line end
			Line sLine = lines[boxes[0].line];
			sLine.Content = HF.RemoveSection(sLine.Content, boxes[0].start, boxes[0].end);

			// remove last line start 
			Line eLine = lines[boxes[^1].line];
			eLine.Content = HF.RemoveSection(eLine.Content, boxes[^1].start, boxes[^1].end);


			// put cursor in right place
			c.SetHead(new(lines[boxes[0].line].Content.Length, boxes[0].line));

			// add end to start
			lines[boxes[0].line].Content +=
				lines[boxes[^1].line].Content;

			// delete the floating last line 
			DeleteLine(boxes[^1].line);

			// delete in between contents 
			for (int i = boxes.Count - 1; i >= 0; i--) {
				var box = boxes[i];

				if (box.fullLine) {
					DeleteLine(box.line);
				}
			}
		}

		UpdateLine(c.head.y);

		c.ResetBlink();
		c.MatchTail();
		c.Update();
	}

	void NormallyType(Caret c, Line line) {
		string contentBefore = line.Content;

		// no ctrl or alt!! commands... and and idk
		if (Conatrols.Keyboard.Modifiers.Ctrl ||
			Conatrols.Keyboard.Modifiers.Alt) return;

		string toType = "";
		foreach (Key k in Conatrols.Keyboard.Presses) {
			bool keyIsChar = Conatrols.Keyboard.All.CharacterKeys.Contains(k);
			if (!keyIsChar) continue;

			if (Conatrols.Keyboard.Modifiers.Shift) {
				toType += Conatrols.Keyboard.All.KeyShiftedMapping[k];
			} else {
				toType += Conatrols.Keyboard.All.KeyCharMapping[k];
			}
		}

		if (toType.Length == 0) return;

		// type it
		TypeAt(c, line, toType);

		if (line.Content == contentBefore) return; // dont waste time

		// update
		UpdateLine(c.head.y);

		c.ResetBlink();
		c.MatchTail();
		c.Update();
	}

	void TypeAt(Caret c, Line line, string toType) {
		if (c.head.x <= line.Content.Length) {
			line.Content = line.Content.Insert(c.head.x, toType);
			c.head.x += toType.Length;
		} else {
			PadToIndex(c.head.x, c.head.y, line);
			line.Content += toType;

			c.head.x = line.Content.Length;
		}
	}

	void PadToIndex(int index, int lineNum, Line line) {
		bool useTabs = true; // TODO: figure this out sometime 

		// actual TODO: figure out this stupid tabs thing? cuz i just cant figure out whats 
		// wrong with it and spaces work just fine so
		// im gonna keep using them 

		if (useTabs) {
			string extraIndent = IndentToPosString(line.Content.Length, index, lineNum);

			line.Content += extraIndent;
		} else {
			int spacesToAdd = index - line.Content.Length;
			line.Content += new string(' ', spacesToAdd);
		}
	}

	void DeleteWholeLine(Caret c) {
		int num = c.head.y;

		c.head.y++;
		c.Update();

		DeleteLine(num);
		UpdateLine(num - 1);

		c.head.y--;
		c.head.x = 0;
		c.ResetBlink();
		c.MatchTail();
		c.Update();

		return;
	}

	void Backspace(Caret c, Line line) {
		if (c.head.x == 0) { // delete line
			// might switch to a SMARTER SYSTEM if it is needed later
			// maybe with actual newlines idk
			
			int num = c.head.y;
			if (num == 0) return;

			// put the caret
			c.head.y--;
			c.head.x = lines[c.head.y].Content.Length;
			c.MatchTail();
			c.Update(); // force update before the line gets deleted
			
			// append whatevers on this line onto previous
			lines[num - 1].Content += line.Content;

			// then delete the line
			DeleteLine(num);

			// this line needs to update
			UpdateLine(num - 1);

			return;
		}

		if (c.head.x >= line.Content.Length)
			PadToIndex(c.head.x, c.head.y, line);

		if (Conatrols.Keyboard.Modifiers.Ctrl) {
			(int start, int end) = DoubleClickWordAt(c.head - new Vector2Int(1, 0));
			end = c.head.x;
			int length = end - start;
			line.Content = line.Content.Remove(start, length);
			c.head.x -= length;
		} else {
			line.Content = line.Content.Remove(c.head.x - 1, 1);
			c.head.x--;
		}

		// update
		UpdateLine(c.head.y);

		c.ResetBlink();
		c.MatchTail();
		c.Update();
	}

	void Delete(Caret c, Line line) {
		if (c.head.x >= line.Content.Length) { // delete line

			int num = c.head.y;
			if (num == lines.Count - 1) return;

			// pad to the caret since it might be past
			PadToIndex(c.head.x, c.head.y, line);

			// append prev line onto this
			line.Content += lines[num + 1].Content;

			// then delete the next line
			DeleteLine(num + 1);

			// update
			UpdateLine(c.head.y);
			c.ResetBlink();

			return;
		}

		if (Conatrols.Keyboard.Modifiers.Ctrl) {
			(int start, int end) = DoubleClickWordAt(c.head);
			start = c.head.x;
			int length = end - start;
			line.Content = line.Content.Remove(start, length);
		} else {
			line.Content = line.Content.Remove(c.head.x, 1);
		}

		// update
		UpdateLine(c.head.y);
		c.ResetBlink();
	}

	void Enter(Caret c, Line line) {
		bool splitText = true;
		bool addDownwards = true;

		if (Conatrols.Keyboard.Modifiers.Ctrl &&
			Conatrols.Keyboard.Modifiers.Shift) {
			splitText = true;
			addDownwards = false;
		} else
		if (Conatrols.Keyboard.Modifiers.Ctrl) {
			splitText = false;
			addDownwards = false;
		} else
		if (Conatrols.Keyboard.Modifiers.Shift) {
			splitText = false;
			addDownwards = true;
		}

		// determine end contents and split
		string endContents = "";
		if (splitText) {
			if (c.head.x >= line.Content.Length) {
				PadToIndex(c.head.x, c.head.y, line);
			} else {
				endContents = line.Content[c.head.x..];
				line.Content = line.Content[..c.head.x];
			}
			UpdateLine(c.head.y);
		}

		endContents = endContents.TrimStart(); // no spaces before

		// match indent (uugh have to add smart block indents and stuff later with if and whatevnlksghlfk;sjg
		int indentSpaces = IndentSpacesOfLine(line);
		//print($"FUCK YOU {indentSpaces}");
		string startIndent = IndentToColumnString(0, indentSpaces, c.head.y + 1);
		endContents = endContents.Insert(0, startIndent);

		Line newLine = GenerateNewLine(endContents); 

		// place cursor
		if (addDownwards) c.head.y++;
		c.head.x = newLine.Content.Length - endContents.Length + startIndent.Length;

		// set the indexes
		lines.Insert(c.head.y, newLine);
		newLine.Components.LineContent.transform.SetSiblingIndex(c.head.y);

		// add ln
		LineNumber newLN = GenerateNewNumber(lines.Count);
		lineNumbers.Add(newLN);

		UpdateLine(c.head.y);

		RecalculateAll();

		c.ResetBlink();
		c.MatchTail();
		c.Update();
	}

	void Tab(Caret c, Line line) {
		if (Conatrols.Keyboard.Modifiers.Shift) {
			// dedent entire line
			string spaceIndent = new(' ', Config.Language.SpacesPerTab);

			if (line.Content.StartsWith('\t'))
				line.Content = line.Content[1..]; // tab dedent
			else if (line.Content.StartsWith(spaceIndent))
				line.Content = line.Content[Config.Language.SpacesPerTab..]; // spaces dedent

		} else {
			// normal add tab
			TypeAt(c, line, "\t");
		}

		// update
		UpdateLine(c.head.y);

		c.ResetBlink();
		c.MatchTail();
		c.Update();
	}
	#endregion

	#region Position Utils
	public (RectTransform rt, float t) GetLocation(Vector2Int vec) {
		return (
			lines[vec.y].Components.LineContent,
			lines[vec.y].IndexTs[vec.x]);
	}

	Vector2Int ClampPosition(Vector2Int pos) {
		return new(Mathf.Clamp(pos.x, 0, lines[pos.y].Content.Length), pos.y);
	}

	public int ColumnOfPosition(Vector2Int pos) {
		string line = lines[pos.y].Content;
		if (pos.x >= line.Length) 
			return ColumnOfPositionOvershoot(pos);
		
		int col = 0;
		for (int i = 0; i < pos.x; i++) {
			if (line[i] == '\t') col += TabIndexToSpaceCount(col);
			else col++;
		}
		return col;
	}

	public int ColumnOfPositionOvershoot(Vector2Int pos) {
		string line = lines[pos.y].Content;
		int col = 0;
		foreach (char c in line) {
			if (c == '\t') col += TabIndexToSpaceCount(col);
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
				testCol += TabIndexToSpaceCount(testCol);
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

	public int IndentSpacesOfLine(Line line) {
		int pos = 0;
		int i = 0;
		while (i < line.Content.Length) {
			char c = line.Content[i];
			if (!char.IsWhiteSpace(c)) break;
			pos += c switch {
				'\t' => TabIndexToSpaceCount(pos),
				_ => 1
			};
			i++;
		}
		return pos;
	}

	public (int tabs, int spaces) IndentToPosCharsCount(int startIndex, int endIndex, int line) {
		int startCol = ColumnOfPosition(new(startIndex, line));
		int endCol = ColumnOfPosition(new(endIndex, line));

		return IndentToPosColumn(startCol, endCol, line);
	}

	public (int tabs, int spaces) IndentToPosColumn(int startCol, int endCol, int line) {
		//print($"s {startCol} e {endCol}");

		int tabs = 0;
		int pos = startCol;
		while (pos < endCol) {
			int amount = TabIndexToSpaceCount(pos);
			pos += amount;
			if (pos >= endCol) {
				pos -= amount;
				break;
			}
			tabs++;
		}


		int extraSpaces = endCol - pos;
		// im so tired of this fuckking method
		if (extraSpaces == Config.Language.SpacesPerTab) {
			extraSpaces = 0;
			tabs++;
		}

		//print($"t {tabs} p {pos} e {extraSpaces}");

		return (tabs, extraSpaces);
	}

	public string IndentToPosString(int startIndex, int endIndex, int line) {
		(int tabs, int spaces) = IndentToPosCharsCount(startIndex, endIndex, line);
		return new string('\t', tabs) + new string(' ', spaces);
	}
	public string IndentToColumnString(int startCol, int endCol, int line) {
		(int tabs, int spaces) = IndentToPosColumn(startCol, endCol, line);
		return new string('\t', tabs) + new string(' ', spaces);
	}

	#endregion

	void DebugLines() {
		StringBuilder sb = new();
		for (int i = 0; i < lines.Count; i++) {
			Line line = lines[i];
			sb.Append($"Line {i} real {line.Realised} __ {line.ProcessedContent}\n");
		}
		print(sb.ToString()); // In this economy??? 
	}

	#region Clipboard

	struct ClipboardEntry {
		public bool IsMultiline;
		public bool IsWholeLine;
		public List<string> Strings;
	}

	List<ClipboardEntry> Clipboard = new();
	void Copy() {
		if (carets.Count > 1) {
			CopyMultiCaret();
			return;
		}

		Caret c = carets[0];
		if (c.HasSelection) {
			string content = c.GetSelectionString();

			Clipboard.Add(new() {
				IsMultiline = false,
				IsWholeLine = false,
				Strings = new() { content }
			});
		} else {
			// copy entire line 
			Clipboard.Add(new() {
				IsMultiline = false,
				IsWholeLine = true,
				Strings = new() { lines[c.head.y].Content }
			});
		}

		// possible edge cases
		while (Clipboard.Count > Config.ScriptEditor.MaxClipboardSize) {
			Clipboard.RemoveAt(0);
		}
	}

	void CopyMultiCaret() {
		List<string> contents = carets.Select(c => c.GetSelectionString()).ToList();

		Clipboard.Add(new() {
			IsMultiline = true,
			IsWholeLine = false,
			Strings = contents
		});
	}

	void Cut() {
		Copy();

		foreach(Caret c in carets) {
			DeleteSelection(c, lines[c.head.y]);
		}
	}

	void Paste() {

	}
	
	#endregion

	#region Shortcuts

	void SubscribeToShortcuts() {
		Shortcuts.Get("copy").Subscribe(Copy);
		Shortcuts.Get("cut").Subscribe(Cut);
		Shortcuts.Get("paste").Subscribe(Paste);
	}

	#endregion
}