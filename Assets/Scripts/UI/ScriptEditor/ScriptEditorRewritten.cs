using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScriptEditorRewritten : MonoBehaviour{
	static readonly float NavBarHeight = 50;
	static float LineNumberWidth = 40; // to be calculated procedurally later
	public static float CaretWidth = 2;
	public static float CaretHeightExtra = 2;
	public static Color CaretColor = new(1, 1, 1);

	public string Content;

	public CaretHandler Carets;

	public bool Open => Window.RealisedWindow.Open;
	void Update() {
		HandleMouse();
		HandleKeyboard();
		UpdateCarets();
	}

	#region Util functions
	protected int SS2I(Vector2 SSpos) => // may become bottleneck
		TMP_TextUtilities.FindNearestCharacter(CodeText, SSpos, Camera.main, true);

	protected Vector2 I2SS(int i) {
		var info = CodeText.textInfo.characterInfo[i];
		
		var point = info.bottomLeft;

		return RectTransformUtility.WorldToScreenPoint(Camera.main, point);
	}

	protected (Vector2 tl, Vector2 tr, Vector2 br, Vector2 bl) I2Corners(int i) {
		var info = CodeText.textInfo.characterInfo[i];

		return (
			RectTransformUtility.WorldToScreenPoint(Camera.main, info.topLeft),
			RectTransformUtility.WorldToScreenPoint(Camera.main, info.topRight),
			RectTransformUtility.WorldToScreenPoint(Camera.main, info.bottomRight),
			RectTransformUtility.WorldToScreenPoint(Camera.main, info.bottomLeft)
			);
	}

	protected int NumLines => Content.Count(c => c == '\n');

	bool layoutChanged = true;
	Vector2 m_LH;
	protected Vector2 CharSize => {
		if (layoutChanged) {
			var info = CodeText.textInfo.characterInfo[0];
			m_LH = info.topRight - info.bottomLeft;
			layoutChanged = false;
		}
		return m_LH;
	};

	// just try not to call this.
	protected int[] newLineIs => 
		Content
		.Select((c, i) => (c, i))
		.Where(ci => ci.c == '\n')
		.Select(ci => ci.i)
		.ToArray();

	// startIinc is the index of the first char
	// endIexc is the index of the line's newline aka end char i + 1
	// content does not include \n
	// try to minimize calls to this
	protected (int lineNum, int startIinc, int endIexc, string content) GetLineAt(int i) {
		var newlines = newLineIs;

		int linenum = -1;
		int before = -1;
		int after = -1;
		for (int ni = 0; ni < newlines.Length; ni++) {
			int nli = newlines[ni];
			if (nli > i) {
				linenum = ni;
				before = 
					ni != 0
					? newlines[ni - 1]
					: 0;
				after = nli;
				break;
			}
		}

		string line = Content[before..after];
		return (linenum, before, after, line);
	}

	// same as before with inc and exc (exc=inc \n)
	protected (int startIinc, int endIexc, string content) GetLine(int i) {
		var newlines = newLineIs;
		
		var endI = newlines[i];
		var startI = 
			i != 0
			? newlines[i - 1] + 1
			: 0;

		var line = Content[startI..endI];

		return (startI, endI, line);
	}

	#endregion

	bool dragging = false;
	bool altDragging = false;
	Vector2 dragStart;
	void HandleMouse() {
		if (Conatrols.Mouse.Left.PressedThisFrame) {
			dragging = true;
			
			if (!Conatrols.Keyboard.Modifiers.Shift)
				dragStart = Conatrols.Mouse.Position;

			altDragging = Conatrols.Keyboard.Modifiers.Alt;
		} else 
		if (Conatrols.Mouse.Left.ReleasedThisFrame) {
			dragging = false;
		}

		if (dragging) {
			if (!altDragging) {
				int dragStartI = SS2I(dragStart);
				int dragEndI = SS2I(Conatrols.Mouse.Position);

				Carets.SetSingleCaret(dragStartI, dragEndI);
			} else {

			}
		}
	}


	void HandleKeyboard() {

	}

	void UpdateText() {

	}

	bool updated = false;
	float lastToggleTime = 0;
	bool caretsOn = false;
	bool caretsForceUpdate = false;
	void UpdateCarets() {
		if (!((Time.time - lastToggleTime > Config.ScriptEditor.CursorBlinkRateMs / 1000f && !updated)
			|| caretsForceUpdate)) return;
		caretsForceUpdate = false;
		updated = true;
		lastToggleTime = Time.time;

		caretsOn = !caretsOn;
		
		Carets.UpdateObjects(caretsOn)
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
			public int tail;
			public int head;
			public RectTransform rt;
			public List<RectTransform> selBoxes;

			public Vector2? customTail;
			public Vector2? customHead;

			public void Init(CaretHandler handler) { this.handler = handler; }

			public void Select(int index) {
				tail = index;
				head = index;

				customTail = null;
				customHead = null;
			}

			public void Select(int from, int to) {
				tail = from;
				head = to;

				customTail = null;
				customHead = null;
			}

			public void SetCustom(Vector2 tail, Vector2 head) {
				customTail = tail;
				customHead = head;
			}

			public void ReDraw(bool caretOn) {
				bool isCustom = customTail.HasValue;
				if (isCustom) {

				}

				ReDrawCaret(caretOn);

				ReDrawSelBoxes();
			}

			void ReDrawCustom(bool draw) {
				ClearSelBoxes();
				MakeSelBox(customTail.Value, customHead.Value);

				DestroyCaret();
				if (!draw) return;

				DrawCaret(customHead.Value);
			}

			void ReDrawCaret(bool draw){
				DestroyCaret();

				if (!draw) return;

				// draw caret at head
				var corners = handler.Main.I2Corners(head);
				var center = (corners.bl + corners.tl) / 2;
				DrawCaret(center);
			}

			void DrawCaret(Vector2 at) {
				var corners = handler.Main.I2Corners(head);
				var height = corners.tl.y - corners.bl.y;
				var size = new(ScriptEditorRewritten.CaretWidth, height + ScriptEditorRewritten.CaretHeightExtra);
				
				rt = handler.MakeImageInCode(
					"Caret",
					ScriptEditorRewritten.CaretColor,
					at - size, 
					at + size
				)
			}

			void DestroyCaret() {
				if (rt != null) {
					Destroy(rt.gameObject);
					rt = null;
				}
			}

			void ReDrawSelBoxes() {
				ClearSelBoxes();

				if (head == tail) return;

				// process assuming tail is behind head
				bool swap = head < tail;
				if (swap) (head, tail) = (tail, head);

				// use ints for rounding and speed
				int taily = (int)handler.Main.I2SS(tail).y;
				int heady = (int)handler.Main.I2SS(head).y;

				if (taily == heady) {
					var bl = handler.Main.I2Corners(tail).bl;
					var tl = handler.Main.I2Corners(head).tl;

					MakeSelBox(bl, tl);
				} else {
					// assuming head is after tail now
					List<(Vector2 bl, Vector2 tl)> CornerPairs = new();

					// add obvious head and tail
					var tailLineBL = handler.Main.I2Corners(tail).bl;
					var (tailLine, _, tailLineEndI, _) = handler.Main.GetLineAt(tail);
					var tailLineTL = handler.Main.I2Corners(tailLineEndI).tl;
					CornerPairs.Add((tailLineBL, tailLineTL));

					var (headLine, headLineStartI, _, _) = handler.Main.GetLineAt(head);
					var headLineBL = handler.Main.I2Corners(headLineStartI).bl;
					var headLineTL = handler.Main.I2Corners(head).tl;
					CornerPairs.Add((headLineBL, headLineTL));

					// onto tricky ones
					float lEdgeX = headLineBL.x;
					var linesInBetween = Enumerable.Range(tailLine, headLine); // still assuming tail < head therefore tl < hl
					foreach (var line in linesInBetween) {
						// go from left edge to far right 
						var (ibLineStart, ibLineEnd, _) = handler.Main.GetLine(line);
						
						var ibLineBLy = handler.Main.I2Corners(ibLineStart).bl.y;
						var ibLineTL = handler.Main.I2Corners(ibLineEnd - 1).tl;

						CornerPairs.Add((
							new(lEdgeX, ibLineBLy),
							ibLineTL
						));
					}

					// HANK!!!! DONT ABBREVIATE CORNER PAIRS!!!! NOOOO
					foreach (var cp in CornerPairs) {
						MakeSelBox(cp.bl, cp.tl);
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
		}

		public ScriptEditorRewritten Main;
		public List<Caret> Carets = new();

		public void SetSingleCaret(int tail, int head) {
			Carets = new() {
				new() {
					tail = tail,
					head = head
				}
			};

			UpdateCaretHandlers();
		}
		
		public void SetSingleCustomCaret(Vector2 tail, Vector2 head) {
			Carets = new() {
				new() {
					customTail = tail,
					customHead = head
				}
			};

			UpdateCaretHandlers();
		}
		}

		void UpdateCaretHandlers() {
			foreach (Caret caret in Carets) {
				caret.handler = this;
			}
		}

		// call this every frame and redraw all carets
		// shouldnt be too expensive, if it is we can optimize it
		// write once optimize never
		public void UpdateObjects(bool drawCarets) {
			if (Carets.Count == 0) return;

			foreach (var caret in Carets) {
				caret.ReDraw(drawCarets);
			}
		}
		
		protected RectTransform MakeImageInCode(string name, Color color, Vector2 from, Vector2 to) {
			GameObject obj = new(name);
			var rt = obj.AddComponent<RectTransform>();
			rt.SetParent(Main.CodeText.transform);

			Vector2 min = Vector2.Min(from, to);
			Vector2 max = Vector2.Max(from, to);

			rt.anchoredPosition = (min + max) / 2f;
			rt.sizeDelta = max - min;

			var image = obj.AddComponent<Image>();
			image.color = color;

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
			position: new(0, 0, 0, 1),
			sizeDelta: LineNumberWidth * Vector2.one,
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
			margin: new(0, 0, LineNumberWidth, 0)
		)
	).OnRealized((rt, _) => CodeText = rt.gameObject.GetComponent<TextMeshProUGUI>()),
})
			}
		};
	}
	#endregion
}
