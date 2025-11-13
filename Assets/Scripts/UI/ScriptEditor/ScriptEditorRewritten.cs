using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using cfg = Config.ScriptEditor;

// coordinates work in screen space
// +y is up in the doc, back in the content

public class ScriptEditorRewritten : MonoBehaviour {
	static readonly float NavBarHeight = 50;
	public static float CaretWidth = 1.5f;
	public static Color CaretColor = new(1, 1, 1);

	public string Content = "";

	public CaretHandler Carets;
	public SyntaxHighlighter SyntaxHighlighter;
	public LazyHistory History;

	float LineNumberWidth = 0;

	public bool Open => Window.RealisedWindow.Open;

	static Action<Transform> PostRealizationAction;
	public static void CreateWindow() {
		var temp = new GameObject("ser");
		var ser = temp.AddComponent<ScriptEditorRewritten>();

		ser.SetWindow();
		ser.UpdateWindow();

		PostRealizationAction = (t) => {
			// make the new component on the actual object and copy over all values
			var nsr = t.gameObject.AddComponent<ScriptEditorRewritten>();
			nsr.Window = ser.Window;
			nsr.LineNumbersText = ser.LineNumbersText;
			nsr.MainEditorScrollRect = ser.MainEditorScrollRect;
			nsr.CodeText = ser.CodeText;
			nsr.ExtraRaycastTarget = ser.ExtraRaycastTarget;

			// move numbers into viewport
			nsr.LineNumbersText.rectTransform.SetParent(
				nsr.MainEditorScrollRect.viewport);

			// move content into a parent mask
			var codeMask = HF.CreateRectTransform(
				"Code Mask",
				nsr.MainEditorScrollRect.viewport,
				new(0, 0), new(1, 1), new(.5f, .5f),
				new(0, 0), new(0, 0), new(0, 0)
				);
			codeMask.gameObject.AddComponent<RectMask2D>();

			var con = nsr.MainEditorScrollRect.content;
			con.SetParent(codeMask);
			nsr.CodeMask = codeMask;

			// give content an autoscaler
			var AS = con.gameObject.AddComponent<TMPAutoScaler>();
			AS.tmp = nsr.CodeText;
			nsr.ContentAutoScaler = AS;

			// fix content
			con.anchorMin = new(0, 1);
			con.anchorMax = new(0, 1);
			var stc = con.gameObject.GetComponent<ScaleToContents>();
			Destroy(stc);

			// update stuff
			nsr.UpdateLineNumbers();

			Destroy(temp);
			PostRealizationAction = null;
		};

		WindowManager.Instance.RealiseWindows(ser.Window);
	}

	void Awake() {
		Carets = new() {
			Main = this
		};
		SyntaxHighlighter = new();
	}
	void Update() {
		UpdateCarets();
		HandleMouse();
		HandleKeyboard();
	}
	void LateUpdate() {
		MoveLineNumbers();
	}

	#region Util functions
	// uses global coords
	protected int SS2I(Vector2 SSpos) => FindNearestCharacterModified(CodeText, SSpos);
	protected int LS2I(Vector2 SSpos) => FindNearestCharacterModified(CodeText, SSpos, false);


	// only checks against left edge
	// also only check against the nearest line
	public static int FindNearestCharacterModified(TextMeshProUGUI text, Vector2 position, bool global = true) {
		if (global)
			position = text.transform.InverseTransformPoint(position);

		var LIs = text.textInfo.lineInfo;

		// find closest line
		float distY = Mathf.Infinity;
		int closestLineI = -1;
		for (int i = 0; i < LIs.Length; i++) {
			TMP_LineInfo line = LIs[i];

			/*Debug.DrawLine(new(0, line.ascender), new(0, line.descender), MoreColors.SkyBlue);
			Debug.DrawLine(new(0, line.ascender), new(10, line.ascender), MoreColors.Orange);
			Debug.DrawLine(new(0, line.descender), new(10, line.descender), MoreColors.Red);
			DebugExtra.DrawPoint(new(0, (line.ascender + line.descender) / 2), 10, MoreColors.Teal);
			DebugExtra.DrawPoint(new(0, position.y), 10, MoreColors.Green);*/

			if (position.y < line.ascender && position.y > line.descender) {
				closestLineI = i;
				break;
			}

			// abs dist to the center y
			float d = Mathf.Abs((line.ascender + line.descender) / 2 - position.y);

			if (d < distY) {
				distY = d;
				closestLineI = i;
			}
		}

		if (closestLineI == -1) throw new("bad code!");

		var cLineInfo = LIs[closestLineI];

		float distX = Mathf.Infinity;
		int closestCharI = -1;
		// find closest char in that line
		for (int c = cLineInfo.firstCharacterIndex; c <= cLineInfo.lastCharacterIndex; c++) {
			var cInfo = text.textInfo.characterInfo[c];

			float d = Mathf.Abs(cInfo.bottomLeft.x - position.x);
			if (d < distX) {
				distX = d;
				closestCharI = c;
			}
		}

		// last char check
		if (closestLineI == text.textInfo.lineCount - 1) {
			float lastCharD = Mathf.Abs(
				text.textInfo.characterInfo[text.textInfo.characterCount - 1].bottomRight.x
				- position.x);
			if (lastCharD < distX)
				return text.textInfo.characterCount;
		}

		return closestCharI;
	}

	/// <summary>
	/// <b>RETURNS IN LS</b>
	/// </summary>
	protected TMP_CharacterInfo CharInfo(int i) {
		if (Content.Length == 0) {
			Content = "H"; // give it sum to work with
			UpdateText(false);
			CodeText.ForceMeshUpdate();

			var info = CharInfo(0);

			Content = "";
			UpdateText(false);
			CodeText.ForceMeshUpdate();
			return info;
		}
		if (i < 0 || i > Content.Length) throw new IndexOutOfRangeException();
		if (i < Content.Length) return CodeText.textInfo.characterInfo[i];

		// i == length

		// newlines treated special
		if (Content[^1] == '\n') {
			// do the content pretend trick from earlier
			Content += "H";
			UpdateText(false);
			CodeText.ForceMeshUpdate();

			var info = CharInfo(Content.Length - 1);

			Content = Content[..^1];
			UpdateText(true);
			CodeText.ForceMeshUpdate();
			return info;
		}

		var penultimate = CharInfo(i - 1);
		return new() { // save some cycles
			ascender = penultimate.ascender,
			//aspectRatio = penultimate.aspectRatio,
			baseLine = penultimate.baseLine,
			bottomLeft = penultimate.bottomRight,
			bottomRight = penultimate.bottomRight,
			character = default,
			color = penultimate.color,
			descender = penultimate.descender,
			//elementType = penultimate.elementType,
			//fontAsset = penultimate.fontAsset,
			//highlightColor = penultimate.highlightColor,
			//highlightState = penultimate.highlightState,
			index = i + 1,
			//isUsingAlternateTypeface = penultimate.isUsingAlternateTypeface,
			//isVisible = penultimate.isVisible,
			lineNumber = penultimate.lineNumber,
			//material = penultimate.material,
			//materialReferenceIndex = penultimate.materialReferenceIndex,
			//origin = penultimate.origin,
			//pageNumber = penultimate.pageNumber,
			//pointSize = penultimate.pointSize,
			//scale = penultimate.scale,
			//spriteAsset = penultimate.spriteAsset,
			//spriteIndex = penultimate.spriteIndex,
			//strikethroughColor = penultimate.strikethroughColor,
			//strikethroughVertexIndex = penultimate.strikethroughVertexIndex,
			//stringLength = penultimate.stringLength,
			//style = penultimate.style,
			//textElement = penultimate.textElement,
			topLeft = penultimate.topRight,
			topRight = penultimate.topRight,
			//underlineColor = penultimate.underlineColor,
			//underlineVertexIndex = penultimate.underlineVertexIndex,
			//xAdvance = penultimate.xAdvance,
		};
	}

	protected Vector2 L2G(Vector2 localPos) =>
		CodeText.transform.TransformPoint(localPos);
	protected Vector2 G2L(Vector2 globalPos) =>
		CodeText.transform.InverseTransformPoint(globalPos);

	protected TMP_LineInfo LineInfo(int lineNum) {
		if (Content.Length == 0) {

		}
		return CodeText.textInfo.lineInfo[lineNum];
	}

	protected int NumLines => Content.Count(c => c == '\n') + 1;

	bool layoutChanged = true;
	Vector2 m_LH;
	protected Vector2 CharSize {
		get {
			if (layoutChanged) {
				CodeText.text = 'H' + CodeText.text; // test on H
				CodeText.ForceMeshUpdate();

				var info = CodeText.textInfo.characterInfo[0];
				float width = info.bottomRight.x - info.bottomLeft.x;
				float height = LineInfo(0).ascender - LineInfo(0).descender;

				m_LH = new(width, height);

				CodeText.text = CodeText.text[1..];
				layoutChanged = false;
			}
			return m_LH;
		}
	}

	/// <summary>
	/// BL, TL, TR, BR
	/// </summary>
	protected Vector3[] MaskCorners {
		get {
			var corners = new Vector3[4];
			CodeMask.GetWorldCorners(corners);
			return corners;
		}
	}
	protected Vector3[] ContentCorners {
		get {
			var corners = new Vector3[4];
			ContentAutoScaler.GetComponent<RectTransform>().GetWorldCorners(corners);
			return corners;
		}
	}


	protected bool MouseOverCode {
		get {
			var corners = MaskCorners;
			bool inbounds = HF.IsPointInBounds(
				Conatrols.Mouse.Position,
				(Vector2)corners[2],
				(Vector2)corners[0]
			);

			bool hovered =
				UIHovers.CheckStrictlyFirst(CodeText.transform)
				|| UIHovers.CheckStrictlyFirst(ExtraRaycastTarget);
			return inbounds && hovered;
		}
	}

	static int charToType(char c) {
		if (char.IsWhiteSpace(c)) return 0;
		if (char.IsDigit(c) || char.IsLetter(c)) return 1;
		if (char.IsSymbol(c)) return 2;
		return 3; // all unknown types are treated as their own ig?
	}

	// direction is -1 or 1
	// output is inclusive
	protected int EndIndexOfSameType(int i, int direction) {
		if (direction != -1 && direction != 1) throw new ArgumentException();
		if ((direction == -1 && i < 0)
			|| (direction == 1 && i >= Content.Length)) return i;

		if (direction == -1) i--; // nudge

		int against = charToType(Content[i]);
		int check = against; // you think this is funny?
		while (check == against) {
			i += direction;
			
			if (i < 0 || i >= Content.Length) break;

			against = charToType(Content[i]);
		}

		if (direction == 1) i++; // nudge

		return i - direction;
	}
	#endregion

	#region Navigation
	bool dragging = false;
	bool altDragging = false;
	Vector2 dragStart;
	float lastPressTime = 0;
	bool doubleClickDragging = false;
	void HandleMouse() {
		if (Conatrols.Mouse.Left.PressedThisFrame) {
			if (MouseOverCode) {
				doubleClickDragging = 
					(Time.time - lastPressTime < Config.Input.doubleClickMaxTimeMs / 1000f
					|| Conatrols.Keyboard.Modifiers.Ctrl)
					&& !Conatrols.Keyboard.Modifiers.Shift;
				lastPressTime = Time.time;

				dragging = true;

				if (!Conatrols.Keyboard.Modifiers.Shift)
					dragStart = Conatrols.Mouse.Position;

				altDragging = Conatrols.Keyboard.Modifiers.Alt && !doubleClickDragging;

				Window.RealisedWindow.Config.Movable = false;
				Window.RealisedWindow.dragging = false;

			} else {
				Carets.ClearCarets();
			}
		} else 
		if (Conatrols.Mouse.Left.ReleasedThisFrame) {
			dragging = false;

			Window.RealisedWindow.Config.Movable = true;
			Window.RealisedWindow.dragging = false;
		}

		if (dragging) {
			ForceCaretOnState_Update();
			if (!altDragging) {
				if (Content.Length == 0) {
					Carets.SetSingleCaret(0, 0);
					return;
				}

				int dragStartI = SS2I(dragStart); // tail
				int dragEndI = SS2I(Conatrols.Mouse.Position); // head

				if (doubleClickDragging) {
					dragStartI = EndIndexOfSameType(
						dragStartI,
						dragEndI < dragStartI // goes left normally cuz of == case
						? 1 : -1);

					dragEndI = EndIndexOfSameType(
						dragEndI,
						dragEndI < dragStartI // goes right normally cuz of == case
						? -1 : 1);
				}

				Carets.SetSingleCaret(dragStartI, dragEndI);
				Carets.Carets[0].RememberTargetX();
			} else {
				var corners = MaskCorners;
				var altDragPos = Conatrols.Mouse.Position.Clamp(
					corners[0],
					corners[2]
					);
				Carets.SetSingleCustomCaret(dragStart, altDragPos);
			}
		}
	}

	void HandleKeyboardMovement() {
		var presses = Conatrols.Keyboard.Presses;

		Vector2Int movement = new(
			(presses.Contains(Key.LeftArrow) ? -1 : 0) +
			(presses.Contains(Key.RightArrow) ? 1 : 0),
			(presses.Contains(Key.DownArrow) ? -1 : 0) +
			(presses.Contains(Key.UpArrow) ? 1 : 0)
		);

		if (movement.sqrMagnitude > 0) {
			Carets.Move(
				movement, 
				Conatrols.Keyboard.Modifiers.Shift,
				Conatrols.Keyboard.Modifiers.Ctrl);
			ForceCaretOnState_Update();

			KeepMainCaretOnScreen();
		}

		if (Conatrols.Keyboard.PressedThisFrame.Contains(Key.Escape)) {
			Carets.MatchAllTails();
			ForceCaretOnState_Update();
		}
	}
	#endregion

	#region Typing
	void HandleKeyboard() {
		HandleTyping();

		HandleKeyboardMovement();
	}

	void HandleTyping() {
		if (Conatrols.Keyboard.Modifiers.Ctrl) return;

		var presses = Conatrols.Keyboard.Presses;

		bool modified = false;
		foreach (var key in presses) {
			if (Conatrols.Keyboard.All.TextKeys.Contains(key)) {
				Type(key);

				modified = true;
			}
		}

		if (modified) {
			UpdateText(true);
			KeepMainCaretOnScreen();
			ForceCaretOnState_Update();
		}
	}

	void Type(Key key) {
		// handle special
		char c;
		if (key == Key.Enter) c = '\n';
		else if (key == Key.Tab) c = '\t';
		else if (key == Key.Backspace) {
			if (Content.Length == 0) return;

			Carets.Backspace(Conatrols.Keyboard.Modifiers.Ctrl);

			return;
		} else
			c = Conatrols.Keyboard.Modifiers.Shift
				? Conatrols.Keyboard.All.KeyShiftedMapping[key]
				: Conatrols.Keyboard.All.KeyCharMapping[key];

		Carets.Type(c.ToString());
	}
	#endregion

	#region UI Management
	// MAY potentially become laggy. if this does happen then optimize it. otherwise 
	// keep the naive code cuz it works and its <1ms anyway
	//string lastContent = "";
	void UpdateText(bool renderColors) {
		// do colors later
		// great we have to do colors now
		// identify modified lines
		var newLines = Content.Split('\n');
		/*var modifiedLinesI =
			lastContent.Split('\n')
			.Select((s, i) => (s, i))
			.Where(si => newLines[si.i] != si.s)
			.Select(si => si.i)
			.ToArray();

		var updatedLines =
			modifiedLinesI.ToDictionary(i => i, i => false);

		// should be in ascending order already
		foreach (var line in modifiedLinesI) {
			if (updatedLines[line]) continue; // already updated

			
		}*/

		ScriptEditor.Context context = new();
		StringBuilder builder = new();
		for (int i = 0; i < newLines.Length; i++) {
			string line = newLines[i];
			var colors = SyntaxHighlighter.ParseLineToColorList(line, context);

			string tagged = SyntaxHighlighter.TagLine(line, colors);
			builder.Append(tagged);
			if (i != newLines.Length - 1)
				builder.Append("\n");
		}

		CodeText.text = $"<mspace=.5em>{builder}</mspace>";

		// .5em is 150 tab width

		UpdateLineNumbers();
	}

	void UpdateLineNumbers() {
		int digits = NumLines.ToString().Length;
		float width = digits * CharSize.x;
		width = Mathf.Max(cfg.NumberDefaultWidth, width);

		if (width != LineNumberWidth) {
			LineNumberWidth = width;

			// manually change sizes (instead of recreating the window)
			LineNumbersText.rectTransform.anchoredPosition = new(0, 0);
			LineNumbersText.rectTransform.sizeDelta = new(LineNumberWidth + cfg.NumberExtraWidth, 0);

			float text = LineNumberWidth + cfg.NumberExtraWidth + cfg.NumberToContentSpace;
			CodeMask.offsetMin = new(text, 0);
			CodeMask.offsetMax = new(0, 0);

			ContentAutoScaler.Padding = new(text + cfg.ContentExtraWidth, 0);

			CodeText.rectTransform.anchoredPosition = new(text, 0);
		}

		LineNumbersText.text = string.Join('\n', Enumerable.Range(0, NumLines));
	}

	void MoveLineNumbers() {
		LineNumbersText.rectTransform.anchoredPosition = 
			new(0, MainEditorScrollRect.content.anchoredPosition.y);
	}
	#endregion

	#region Shortcuts
	
	#endregion

	#region Carets
	float lastToggleTime = 0;
	bool caretsOn = false;
	bool caretsForceUpdate = false;
	//string debugcarets;
	void UpdateCarets() {
		if (!((Time.time - lastToggleTime > cfg.CursorBlinkRateMs / 1000f)
			|| caretsForceUpdate)) return;
		caretsForceUpdate = false;
		lastToggleTime = Time.time;

		caretsOn = !caretsOn;
		
		Carets.Update(caretsOn);


		//debugcarets = string.Join(", ", Carets.Carets.Select(c => $"c({c.tail}-{c.head}"));
	}

	protected void ForceCaretsUpdate(){
		caretsForceUpdate = true;
	}

	protected void ForceCaretOnState_Update() {
		caretsOn = false; // toggles to true
		ForceCaretsUpdate();
	}

	// todo fix the thing where carets on the top and left edge
	// yea figure it out lol
	void KeepMainCaretOnScreen(int rec = 0) {
		if (Carets.Carets.Count == 0) return;
		if (rec > cfg.MaxCaretViewRecoverySteps) return;

		Vector2 caret0pos = Carets.Carets[0].HeadPos;
		caret0pos = L2G(caret0pos);

		Vector2Int shift = Vector2Int.zero;
		Vector3[] mCorners = MaskCorners;
		Vector3[] cCorners = ContentCorners;

		// is over?
		mCorners[0] += cfg.CursorScreenMargin * Vector3.one;
		mCorners[2] -= cfg.CursorScreenMargin * Vector3.one;
		//cCorners[0] += cfg.CursorScreenMargin * Vector3.one;
		//cCorners[2] -= cfg.CursorScreenMargin * Vector3.one;

		bool mLedge = caret0pos.x < mCorners[0].x;
		bool mRedge = caret0pos.x > mCorners[2].x;
		bool mTedge = caret0pos.y < mCorners[0].y;
		bool mBedge = caret0pos.y > mCorners[2].y;

		//bool cLedge = caret0pos.x < cCorners[0].x;
		//bool cRedge = caret0pos.x > cCorners[2].x;
		//bool cTedge = caret0pos.y < cCorners[0].y;
		//bool cBedge = caret0pos.y > cCorners[2].y;

		if (mLedge) shift.x--;
		if (mRedge) shift.x++;
		if (mTedge) shift.y++;
		if (mBedge) shift.y--;

		/*DebugExtra.DrawRect2D(mCorners[0], mCorners[2], color: Color.cyan);
		DebugExtra.DrawRect2D(cCorners[0], cCorners[2], color: Color.yellow);
		DebugExtra.DrawPoint(cCorners[0], 10, Color.green);
		DebugExtra.DrawPoint(cCorners[2], 10, Color.blue);
		DebugExtra.DrawPoint(caret0pos, 10, Color.red);*/

		if (shift.sqrMagnitude > 0) {
			Debug.Log($"scrolling {shift}");
			MainEditorScrollRect.ManuallyScrollX(shift.x * CharSize.x);
			MainEditorScrollRect.ManuallyScrollY(shift.y * CharSize.y);
			KeepMainCaretOnScreen(rec + 1);
		}
	}

	public class CaretHandler {
		public class Caret {
			public CaretHandler handler;
			public ScriptEditorRewritten main;
			public int head; // if this == content.length, then it is at the end
			public int tail;
			public RectTransform rt;
			public List<RectTransform> selBoxes = new();
			
			// caret centers
			public Vector2? customTail;
			public Vector2? customHead;

			public Vector2 HeadPos =>
				isCustom
				? customHead.Value :
				main.CharInfo(head).bottomLeft;

			float targetX = -1;

			public bool isCustom => customTail.HasValue || customHead.HasValue;

			void Clamp() {
				tail = Mathf.Clamp(tail, 0, main.Content.Length);
				head = Mathf.Clamp(head, 0, main.Content.Length);
			}

			// trigger redraw in the handler 
			public void Move(Vector2Int amount, bool shift, bool ctrl) {
				// y movement overrides x
				if (amount.y != 0) {
					if (main.CharInfo(head).lineNumber == 0
						&& amount.y > 0) return;
					if (main.CharInfo(head).lineNumber == main.NumLines - 1
						&& amount.y < 0) return;

					// attempt to move to target x
					Vector2 targetPos = new(
						targetX,
						main.LineInfo(main.CharInfo(head).lineNumber - amount.y).CenterY()
					);

					head = main.SS2I(main.L2G(targetPos));
					return;
				}

				if (amount.x != 0) {
					if (head == tail || shift) {
						if (!ctrl) {
							head += amount.x;
						} else {
							int otherSide = main.EndIndexOfSameType(head, amount.x);

							head = otherSide;
						}
					} else {
						head =
							amount.x > 0
							? Mathf.Max(head, tail)
							: Mathf.Min(head, tail);
					}

					Clamp();

					RememberTargetX();
				}
			}

			public void RememberTargetX() {
				targetX = main.CharInfo(head).bottomLeft.x;
			}

			public void MatchTail() {
				tail = head;
			}

			#region Drawing

			public void Draw(bool caretOn) {
				if (isCustom) {
					DrawCustom(caretOn);
					return;
				}

				Clamp();

				DrawCaret(caretOn);
				DrawSelBoxes();
			}

			void DrawCustom(bool draw) {
				MakeSelBox(
					main.G2L(customTail.Value + new Vector2(0, main.CharSize.y / 2f)),
					main.G2L(customHead.Value - new Vector2(0, main.CharSize.y / 2f)));

				if (!draw) return;

				DrawCaret(main.G2L(customHead.Value));
			}

			void DrawCaret(bool draw){
				if (!draw) return;

				DrawCaret(main.CharInfo(head).CenterLeft());
			}

			void DrawCaret(Vector2 atLocal) {
				// assuming theres at least 1 line

				var size = new Vector2(CaretWidth, main.CharSize.y);

				rt = handler.MakeImageInCode(
					"Caret",
					CaretColor,
					atLocal - size / 2f,
					atLocal + size / 2f
				);
			}

			internal void DestroyCaret() {
				if (rt != null) {
					Destroy(rt.gameObject);
					rt = null;
				}
			}

			void DrawSelBoxes() {
				ClearSelBoxes();

				if (head == tail) return;

				// process assuming tail is behind head
				bool swap = head < tail;
				if (swap) (head, tail) = (tail, head);

				// use ints for rounding and speed
				int tailLN = main.CharInfo(tail).lineNumber;
				int headLN = main.CharInfo(head).lineNumber;

				if (tailLN == headLN) {
					var LI = main.LineInfo(headLN);
					MakeSelBox(
						new(main.CharInfo(tail).bottomLeft.x, LI.descender), 
						new(main.CharInfo(head).bottomLeft.x, LI.ascender));
				} else {
					// assuming head is after tail now
					List<(Vector2 bl, Vector2 tr)> CornerPairs = new();

					// add obvious head and tail
					var tailCI = main.CharInfo(tail);
					var tailLI = main.LineInfo(tailCI.lineNumber);
					CornerPairs.Add((
						new(tailCI.bottomLeft.x,
							tailLI.descender),
						new(main.CharInfo(tailLI.lastCharacterIndex).topRight.x,
							tailLI.ascender)));

					var headCI = main.CharInfo(head);
					var headLI = main.LineInfo(headCI.lineNumber);
					float headBLx = main.CharInfo(headLI.firstCharacterIndex).bottomLeft.x;
					CornerPairs.Add((
						new(headBLx,
							headLI.descender),
						new(headCI.bottomLeft.x,
							headLI.ascender)));

					// onto tricky ones
					float lEdgeX = headBLx;
					for (int line = tailLN + 1; line < headLN; line++) {
						// hope i never have to deal with this again

						// go from left edge to far right 
						var lineinfo = main.LineInfo(line);

						float ibLineBLy = lineinfo.descender;
						
						// i think this is the newline
						// which is why we take TL
						float ibLineTLx = main.CharInfo(lineinfo.lastCharacterIndex).topLeft.x;
						float ibLineTLy = lineinfo.ascender;

						CornerPairs.Add((
							new(lEdgeX, ibLineBLy),
							new(ibLineTLx, ibLineTLy)
						));
					}

					// HANK!!!! DONT ABBREVIATE CORNER PAIRS!!!! NOOOO
					foreach (var (bl, tr) in CornerPairs) {
						MakeSelBox(bl, tr);
					}
				}

				// swap back
				if (swap) (tail, head) = (head, tail);
			}
			
			void ClearSelBoxes() {
				if (selBoxes.Count == 0) return;

				foreach (var box in selBoxes) {
					Destroy(box.gameObject);
				}

				selBoxes.Clear();
			}

			void MakeSelBox(Vector2 from, Vector2 to) {
				var box = handler.MakeImageInCode(
					"Selection Box",
					cfg.SelectionColor,
					from, to
				);

				selBoxes.Add(box);
			}
			#endregion

			#region Typing
			public void Type(string s) {
				if (head == tail) {
					main.Content = main.Content.Insert(head, s);
				} else {
					main.Content = HF.ReplaceSection(
						main.Content,
						Mathf.Min(head, tail),
						Mathf.Max(head, tail),
						s
					);
				}

				head++;
				MatchTail();
			}

			public void Backspace(bool ctrl) {
				if (head != tail) {
					// this works the same lamo
					Type("");

					head = Mathf.Min(head, tail) - 1;
					MatchTail();
					return;
				}

				if (!ctrl) {
					main.Content = main.Content.Remove(head - 1, 1);
					head--;
					MatchTail();
				} else {

				}
			}
			#endregion
		}

		public ScriptEditorRewritten Main;
		public List<Caret> Carets = new();
		private readonly List<Transform> Objects = new();

		public void ClearCarets() {
			Carets.Clear();
		}

		public void SetSingleCaret(int tail, int head) {
			Carets = new() {
				new() {
					tail = tail,
					head = head
				}
			};

			InitCarets();
		}
		
		public void SetSingleCustomCaret(Vector2 tail, Vector2 head) {
			Carets = new() {
				new() {
					customTail = tail,
					customHead = head
				}
			};

			InitCarets();
		}

		public void MatchAllTails() {
			foreach (var c in Carets) {
				c.MatchTail();
			}
		}

		// still expecting a manual update from this one's caller
		public void Move(Vector2Int amount, bool shift, bool ctrl) {
			foreach (var c in Carets) {
				c.Move(amount, shift, ctrl);
			}

			if (!shift)
				MatchAllTails();
		}

		void InitCarets() {
			foreach (Caret caret in Carets) {
				caret.handler = this;
				caret.main = Main;
			}
		}

		// call this every frame and redraw all carets
		// shouldnt be too expensive, if it is we can optimize it
		// write once optimize never
		public void Update(bool drawCarets) {
			// clear everything
			foreach (var t in Objects) {
				Destroy(t.gameObject);
			}
			Objects.Clear();

			// then redraw
			foreach (var caret in Carets) {
				caret.Draw(drawCarets);
			}
		}
		
		protected RectTransform MakeImageInCode(string name, Color color, Vector2 fromLocal, Vector2 toLocal) {
			GameObject obj = new(name);
			var rt = obj.AddComponent<RectTransform>();
			rt.SetParent(Main.CodeText.transform);

			Vector2 min = Vector2.Min(fromLocal, toLocal);
			Vector2 max = Vector2.Max(fromLocal, toLocal);

			rt.localPosition = (min + max) / 2f;
			rt.sizeDelta = max - min;

			var image = obj.AddComponent<Image>();
			image.color = color;

			Objects.Add(rt);

			return rt;
		}

		void Sort() {
			Carets.Sort((a, b) => a.head.CompareTo(b.head));
			Carets.Reverse();
		}
		public void Type(string s) {
			Sort();

			foreach (var c in Carets)
				c.Type(s);
		}

		public void Backspace(bool ctrl) {
			Sort();

			foreach (var c in Carets)
				c.Backspace(ctrl);
		}
	}
	#endregion

	#region UI
	protected TextMeshProUGUI LineNumbersText;
	protected BetterScrollRect MainEditorScrollRect;
	protected TextMeshProUGUI CodeText;
	protected RectTransform CodeMask; // parent of content
	protected TMPAutoScaler ContentAutoScaler;
	protected RectTransform ExtraRaycastTarget;

	public CWindow Window;

	public void SetWindow() { Window = new(); }
	public void UpdateWindow() {
		Window.Name = "Script Editor";
		Window.Config = new() {
			Size = CWindow.Configuration.FreeSize(new(500, 400)),
			HideOnStart = false
		};
		Window.Items = new WindowItem[] {
				// just do the actual editor stuff for now
				WindowItem.NewScrollView(
					"Editor",
					new PComponents.ScrollView()
						.OnRealised<PComponents.ScrollView>(c => MainEditorScrollRect = (BetterScrollRect)c),
					WindowItem.LayoutConfig.DynamicLayout(
						margin: NavBarHeight * FourSides.UpConst
					),
new() {
	WindowItem.NewText(
		"Line Numbers",
		new PComponents.Text(
			"0",
			cfg.Font,
			fontSize: cfg.FontSize,
			alignment: TextAlignmentOptions.TopRight
		),
		WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredAt(UIPosition.TopLeft),
			new(0, 0) // size set by script
		)
	).OnRealized((rt, _) => LineNumbersText = rt.gameObject.GetComponent<TextMeshProUGUI>()),
	WindowItem.NewText(
		"Code",
		new PComponents.Text(
			"",
			cfg.Font,
			fontSize: cfg.FontSize
		).OnRealised<PComponents.Text>(c => CodeText = (TextMeshProUGUI)c),
		WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredAt(UIPosition.TopLeft),
			new(100000, 100000) // real scaling happens on the contents object so this can be any size
			// disable overflow 
			// it must be selectable tho so make it big
		)
	).OnRealized((rt, _) => CodeText = rt.gameObject.GetComponent<TextMeshProUGUI>()),
	WindowItem.NewImage(
		"Raycast Target",
		new PComponents.Image(Color.clear),
		WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredAt(UIPosition.TopLeft),
			new(100000, 100000) // must be omnipresent
		)
	).OnRealized((rt, _) => ExtraRaycastTarget = rt)
})
		};

		Window.CustomEvents = new() {
			new(
				TimedEventInvoker.Timing.Awake,
				(src) => PostRealizationAction?.Invoke(src.transform)
			) 
		};
	}
	#endregion
}