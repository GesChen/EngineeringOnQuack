using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text;

public class ScriptEditor : MonoBehaviour {
	public List<Line> lines;
	List<LineNumber> lineNumbers;

	public ScrollWindow scroll;
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

	#region LocalContext
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
	[HideInNormalInspector] public float allLinesHeight;

	public void Load(string[] strLines) {
		Clear();

		// recalculate max line number width
		TextMeshProUGUI testingText = lineContentVerticalLayout.gameObject.AddComponent(typeof(TextMeshProUGUI)) as TextMeshProUGUI;
		testingText.font = font;
		testingText.fontSize = fontSize;
		Vector2 numberSize = HF.TextWidthExact(strLines.Length.ToString(), testingText);

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

		lineNumbers ??= new();
		foreach (LineNumber ln in lineNumbers) {
			if (ln.Rect != null)
				Destroy(ln.Rect.gameObject);
		}
	}

	void GenerateAllLines() {

		
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

			int charIncrease = c == '\t' ? TabIndexSpaceCount(charPos) : 1;
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

	void UpdateLine(int lineIndex) {
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

		if (!ContextToSignature(context).Equals(preRunSignature)) {
			line.ContextAfterLine = context;

			// then propgoate updates down the lines
			if (lineIndex != lines.Count - 1)
				UpdateLine(lineIndex + 1);
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

	int TabIndexSpaceCount(int i) => HF.Mod(-i - 1, Config.Language.SpacesPerTab) + 1;
	string ConvertTabsToSpaces(string line) {
		string tabsToSpaces = line;
		for (int i = 0; i < tabsToSpaces.Length; i++) {
			char c = tabsToSpaces[i];
			if (c == '\t') {
				int num = TabIndexSpaceCount(i);
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
				ci += TabIndexSpaceCount(ci);
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

	List<Caret> SetMultipleCarets(List<(Vector2Int head, Vector2Int tail)> positions) {
		ResetCarets();

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

	void AddMultipleCarets(List<(Vector2Int head, Vector2Int tail)> positions) {
		for (int i = 0; i < positions.Count; i++) {
			AddNewCaret(positions[i].head, positions[i].tail);
		}
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
		if (caret.HasSelection) return false;
		
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

	#endregion

	#region Mouse Input

	void HandleMouseNavigation() {
		bool clickedThisFrame = Conatrols.IM.Mouse.Left.WasPressedThisFrame();
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
		if (left < 0 || 
			left != 0 && chartype(line[left]) != findCharType) left++;

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
	void HandleDrag(bool clickedThisFrame, Vector2Int pos, Vector2Int posUnclamped) {
		if (clickedThisFrame) { // down
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

			if (Conatrols.Keyboard.Modifiers.Alt && 
				!Conatrols.Keyboard.Modifiers.Ctrl) { // otherwise overrides ctrl alt adding news, TODO fix this later idk
													 // alt dragging
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

				List<(Vector2Int head, Vector2Int tail)> carets = new();
				for (int i = startLine; i <= endLine; i++) {
					Vector2Int head = new(PositionOfColumn(endCol, i), i);
					Vector2Int tail = new(PositionOfColumn(startCol, i), i);

					carets.Add((head, tail));
				}

				SetMultipleCarets(carets);

				tailCaretI = carets.Count - 1;
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
		(int line, int hoverIndex) = FindLineHoveringOver();
		if (hoverIndex == -1) return null;

		int index = GetCharIndexAtWorldSpacePositionUnclamped(line, hoverIndex);
		if (index == -1) return null;

		return new(index, line);
	}

	(int lineIndex, int hoverIndex) FindLineHoveringOver() {
		if (lines == null || lines[0].Components.LineContent == null) return (-1, -1);

		for (int i = 0; i < lines.Count; i++) {
			RectTransform contents = lines[i].Components.LineContent;
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

		RectTransform rt = lines[line].Components.LineContent;

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

		if (boxEditing) { 
			HandleKeyboardBox(movement);
			return;
		}
		foreach (Caret c in carets) {
			if (Conatrols.Keyboard.Modifiers.Ctrl && 
				Conatrols.Keyboard.Modifiers.Shift &&
				Conatrols.Keyboard.Modifiers.Alt)
				continue; // do nothing with all 3

			c.MoveHead(movement);

			if (Conatrols.Keyboard.Modifiers.Ctrl) {
				(int start, int end) = DoubleClickWordAt(c.head - (movement.x > 0 ? 1 : 0) * Vector2Int.right);

				c.SetHead(new(
					movement.x > 0 
					? end 
					: start, c.head.y));
			} else // ctrl overrides alt 
			if (Conatrols.Keyboard.Modifiers.Alt) {
				int pos =
					movement.x > 0
					? lines[c.head.y].Content.Length // end if right
					: 0; // start if left

				c.SetHead(new(pos, c.head.y));
			}

			if (!Conatrols.Keyboard.Modifiers.Shift) {
				c.MatchTail();
			}

			c.Update();
			c.ResetBlink();
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

	#region Typing Input
	void HandleTyping() {
		foreach (Caret c in carets) {
			HandleCaretTyping(c);
		}
	}

	void HandleCaretTyping(Caret c) {
		if (Conatrols.Keyboard.Presses.Count == 0) return;
		Line line = lines[c.head.y];
		string contentBefore = line.Content;

		// handle selection replacement later

		if (Conatrols.IsUsed(Key.Backspace))
			Backspace(c, line);
		else if (Conatrols.IsUsed(Key.Delete))
			Delete(c, line);
		else if (Conatrols.IsUsed(Key.Tab))
			Tab(c, line);
		else {
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
		}

		if (line.Content == contentBefore) return; // dont waste time

		UpdateLine(c.head.y);

		c.ResetBlink();
		c.MatchTail();
		c.Update();
	}

	void TypeAt(Caret c, Line line, string toType) {
		if (c.head.x < line.Content.Length) {
			line.Content = line.Content.Insert(c.head.x, toType);
			c.head.x += toType.Length;
		} else {
			PadToCaret(c, line);
			line.Content += toType;

			c.head.x = line.Content.Length;
		}
	}

	void PadToCaret(Caret c, Line line) {
		int spacesToAdd = c.head.x - line.Content.Length;
		line.Content += new string(' ', spacesToAdd);
	}

	void Backspace(Caret c, Line line) {
		if (c.head.x == 0) { // delete line
			// might switch to a SMARTER SYSTEM if it is needed later
			// maybe with actual newlines idk
			
			int num = c.head.y;
			if (num == 0) return;

			// put the caret
			c.head.y--;
			c.head.x = lines[num - 1].Content.Length;
			c.MatchTail();
			c.Update(); // force update before the line gets deleted
			
			// append whatevers on this line onto previous
			lines[num - 1].Content += line.Content;

			// then delete the line
			DeleteLine(num);

			// before line needs to update
			UpdateLine(num - 1);

			return;
		}

		if (c.head.x >= line.Content.Length)
			PadToCaret(c, line);

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
	}

	void Delete(Caret c, Line line) {
		if (c.head.x >= line.Content.Length) { // delete line

			int num = c.head.y;
			if (num == lines.Count - 1) return;

			// append prev line onto this
			line.Content += lines[num + 1].Content;

			// then delete the next line
			DeleteLine(num + 1);
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
			if (line[i] == '\t') col += TabIndexSpaceCount(col);
			else col++;
		}
		return col;
	}

	public int ColumnOfPositionOvershoot(Vector2Int pos) {
		string line = lines[pos.y].Content;
		int col = 0;
		foreach (char c in line) {
			if (c == '\t') col += TabIndexSpaceCount(col);
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
				testCol += TabIndexSpaceCount(testCol);
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
	
	void DebugLines() {
		StringBuilder sb = new();
		for (int i = 0; i < lines.Count; i++) {
			Line line = lines[i];
			sb.Append($"Line {i} real {line.Realised} __ {line.ProcessedContent}\n");
		}
		print(sb.ToString());
	}
}