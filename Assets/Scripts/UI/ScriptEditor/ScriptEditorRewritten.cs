using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ScriptEditorRewritten : MonoBehaviour{
	static readonly float NavBarHeight = 50;
	static float LineNumberWidth = 40; // to be calculated procedurally later
	public static float CaretWidth = 1.5f;
	public static Color CaretColor = new(1, 1, 1);

	public string Content = "";

	public CaretHandler Carets;

	// coordinates work in screen space
	// +y is up in the doc, back in the content

	void Awake() {
		Carets = new() {
			Main = this
		};
	}


	static Action<Transform> PostRealizationAction;
	public static void CreateWindow() {
		var temp = new GameObject("ser");
		var ser = temp.AddComponent<ScriptEditorRewritten>();

		ser.SetWindow();

		PostRealizationAction = (t) => {
			var nsr = t.gameObject.AddComponent<ScriptEditorRewritten>();
			nsr.Window = ser.Window;
			nsr.CodeText = ser.CodeText;
			nsr.LineNumbersText = ser.LineNumbersText;

			Destroy(temp);
			PostRealizationAction = null;
		};

		WindowManager.Instance.RealiseWindows(ser.Window);
	}

	public bool Open => Window.RealisedWindow.Open;
	void Update() {
		HandleMouse();
		HandleKeyboard();
		UpdateCarets();
	}

	#region Util functions
	// uses global coords
	protected int SS2I(Vector2 SSpos) => FindNearestCharacterModified(CodeText, SSpos);
	protected int LS2I(Vector2 SSpos) => FindNearestCharacterModified(CodeText, SSpos, false);


	// only checks against left edge
	// also only check against the nearest line
	public static int FindNearestCharacterModified(TMP_Text text, Vector2 position, bool global = true) {
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
			DebugExtra.DrawPoint(new(0, position.y), 10, MoreColors.Green);
*/
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

		return closestCharI;
	}

	/// <summary>
	/// <b>RETURNS IN LS</b>
	/// </summary>
	protected TMP_CharacterInfo CharInfo(int i) =>
		CodeText.textInfo.characterInfo[i];
	

	protected Vector2 L2G(Vector2 localPos) =>
		CodeText.transform.TransformPoint(localPos);
	protected Vector2 G2L(Vector2 globalPos) =>
		CodeText.transform.InverseTransformPoint(globalPos);


	protected TMP_LineInfo LineInfo(int lineNum) =>
		CodeText.textInfo.lineInfo[lineNum];

	protected int NumLines => CodeText.textInfo.lineCount;

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

	protected bool MouseInCodeRegion {
		get {
			var corners = new Vector3[4];
			CodeText.rectTransform.GetWorldCorners(corners);
			return HF.IsPointInBounds(
				Conatrols.Mouse.Position,
				(Vector2)corners[2],
				(Vector2)corners[0]
			);
		}
	}

	#endregion

	bool dragging = false;
	bool altDragging = false;
	Vector2 dragStart;
	void HandleMouse() {
		if (Conatrols.Mouse.Left.PressedThisFrame) {
			if (MouseInCodeRegion) {

				dragging = true;

				if (!Conatrols.Keyboard.Modifiers.Shift)
					dragStart = Conatrols.Mouse.Position;

				altDragging = Conatrols.Keyboard.Modifiers.Alt;

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
			ForceCaretOnState();
			if (!altDragging) {
				int dragStartI = SS2I(dragStart);
				int dragEndI = SS2I(Conatrols.Mouse.Position);

				Carets.SetSingleCaret(dragStartI, dragEndI);
				Carets.Carets[0].RememberTargetX();
			} else {
				Carets.SetSingleCustomCaret(dragStart, Conatrols.Mouse.Position);
			}
		}
	}


	void HandleKeyboard() {
		HandleTyping();

		HandleKeyboardMovement();
	}

	void HandleTyping() {
		var presses = Conatrols.Keyboard.Presses;

		bool modified = false;
		foreach (var key in presses) {
			if (Conatrols.Keyboard.All.TextKeys.Contains(key)) {
				Type(key);

				modified = true;
			}
		}

		if (modified) {
			UpdateText();
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
			Carets.Move(movement, Conatrols.Keyboard.Modifiers.Shift);
			ForceCaretOnState();
		}
		// todo figure out why calling this function breaks everything 
	}

	void Type(Key key) {
		// handle special
		char c;
		if (key == Key.Enter) c = '\n';
		else if (key == Key.Tab) c = '\t';
		else if (key == Key.Backspace) {
			if (Content.Length == 0) return;

			if (Conatrols.Keyboard.Modifiers.Ctrl) {
				// special nuts
			} else 
				Content = Content[..^1];

			return;
		} else
			c = Conatrols.Keyboard.Modifiers.Shift
				? Conatrols.Keyboard.All.KeyShiftedMapping[key]
				: Conatrols.Keyboard.All.KeyCharMapping[key];

		Content += c;
	}

	void UpdateText() {
		// do colors later
		CodeText.text = $"<mspace=.5em>{Content}</mspace>";

		// .5em is 150 tab width
	}

	float lastToggleTime = 0;
	bool caretsOn = false;
	bool caretsForceUpdate = false;
	string debugcarets;
	void UpdateCarets() {
		if (!((Time.time - lastToggleTime > Config.ScriptEditor.CursorBlinkRateMs / 1000f)
			|| caretsForceUpdate)) return;
		caretsForceUpdate = false;
		lastToggleTime = Time.time;

		caretsOn = !caretsOn;
		
		Carets.Update(caretsOn);

		debugcarets = string.Join(", ", Carets.Carets.Select(c => $"c({c.tail}-{c.head}"));
	}

	protected void ForceCaretsUpdate(){
		caretsForceUpdate = true;
	}

	protected void ForceCaretOnState() {
		caretsOn = false; // toggles to true
		ForceCaretsUpdate();
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

			float targetX = -1;

			public bool isCustom => customTail.HasValue || customHead.HasValue;

			// trigger redraw in the handler 
			public void Move(Vector2Int amount) {
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
					head += amount.x;
					head = Mathf.Clamp(head, 0, main.Content.Length - 1);
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
				bool isCustom = customTail.HasValue;
				if (isCustom) {
					DrawCustom(caretOn);
					return;
				}

				DrawCaret(caretOn);

				DrawSelBoxes();
			}

			void DrawCustom(bool draw) {
				MakeSelBox(
					main.G2L(customTail.Value),
					main.G2L(customHead.Value));

				if (!draw) return;

				DrawCaret(main.G2L(customHead.Value));
			}

			void DrawCaret(bool draw){
				if (!draw) return;

				// draw caret at head
				var LI = main.LineInfo(main.CharInfo(head).lineNumber);
				Vector2 centerL = new(
					main.CharInfo(head).bottomLeft.x,
					(LI.descender + LI.ascender) / 2f);

				DrawCaret(centerL);
			}

			void DrawCaret(Vector2 atLocal) {
				// assuming theres at least 1 line

				var LI0 = main.LineInfo(0);
				var height = LI0.ascender - LI0.descender;
				var size = new Vector2(CaretWidth, height);

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
					Config.ScriptEditor.SelectionColor,
					from, to
				);

				selBoxes.Add(box);
			}
			#endregion
		}

		public ScriptEditorRewritten Main;
		public List<Caret> Carets = new();
		List<Transform> Objects = new();

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

		void MoveAll(Vector2Int amount) {
			foreach (var c in Carets) {
				c.Move(amount);
			}
		}

		public void MatchAllTails() {
			foreach (var c in Carets) {
				c.MatchTail();
			}
		}

		// still expecting a manual update from this one's caller
		public void Move(Vector2Int amount, bool shift) {
			MoveAll(amount);
			
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

			rt.anchoredPosition = (min + max) / 2f;
			rt.sizeDelta = max - min;

			var image = obj.AddComponent<Image>();
			image.color = color;

			Objects.Add(rt);

			return rt;
		}
	}

	#region UI
	protected TextMeshProUGUI LineNumbersText;
	protected TextMeshProUGUI CodeText;

	public CWindow Window;
	public void SetWindow() {
		Window = new() {
			Name = "Script Editor",
			Config = new() {
				Size = CWindow.Configuration.FreeSizeMinimum(new(500, 400)),
				HideOnStart = false
			},
			Items = new WindowItem[] {
				// just do the actual editor stuff for now
				WindowItem.NewEmpty(
					"Editor",
					WindowItem.LayoutConfig.DynamicLayout(
						margin: NavBarHeight * FourSides.UpConst
					),
new() {
	WindowItem.NewText(
		"Line Numbers",
		new PComponents.Text(
			"0",
			Config.ScriptEditor.Font,
			fontSize: Config.ScriptEditor.FontSize
		),
		WindowItem.LayoutConfig.Custom(
			position: new(0, 1, 0, 0),
			sizeDelta: new(LineNumberWidth, 0),
			fixedPosition: new() {
				Pivot = UIPosition.MiddleLeft
			}
		)
	).OnRealized((rt, _) => LineNumbersText = rt.gameObject.GetComponent<TextMeshProUGUI>()),
	WindowItem.NewText(
		"Code",
		new PComponents.Text(
			"",
			Config.ScriptEditor.Font,
			fontSize: Config.ScriptEditor.FontSize
		),
		WindowItem.LayoutConfig.DynamicLayout(
			margin: LineNumberWidth * FourSides.LeftConst
		)
	).OnRealized((rt, _) => CodeText = rt.gameObject.GetComponent<TextMeshProUGUI>()),
})
			}
		};

		Window.AddEvent(
			TimedEventInvoker.Timing.Awake,
			(src) => PostRealizationAction?.Invoke(src.transform)
		);
	}
	#endregion
}
