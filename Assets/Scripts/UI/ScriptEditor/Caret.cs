using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Caret {
	public ScriptEditor main;

	public Vector2Int head;
	public Vector2Int tail;
	public int DesiredCol;
	public List<SelectionBox> boxes; // should always be in up to down order hopefully
	public bool isHeadCaret;

	static Color headCaretColor = new(1, 1, 1, 1);
	static Color nonHeadCaretColor = new(.6f, .6f, .6f, 1);

	public float tempwidth = 2;

	static float blinkTimer; // for lots of fun, make this not static ;)
	RectTransform rt;
	Image image;
	(Vector2Int head, Vector2Int tail) lastState;

	public bool HasSelection => head != tail;
	public bool IsSingleWholeLine => boxes.Count == 1 && boxes[0].fullLine;

	public Caret(ScriptEditor se, Vector2Int pos) {
		main = se;
		head = pos;
		tail = pos;
	}

	public Caret(ScriptEditor se, Vector2Int head, Vector2Int tail) {
		main = se;
		this.head = head;
		this.tail = tail;
	}

	public Caret(ScriptEditor se) {
		main = se;
	}

	public void ResetBlink() {
		blinkTimer = Time.time;
	}

	public void Initialize() {
		ResetBlink();
		rt = MakeNewCaret();

		boxes = new(); // it keeps breaking everything man
	}

	bool PositionCheck(Vector2Int v) =>
			v.y < 0 || v.y >= main.lines.Count ||
			v.x < 0 || v.x >= main.lines[v.y].IndexTs.Count;
	public void UpdatePos(Vector2Int newHead, Vector2Int newTail) {
		ResetBlink();

		SetHead(newHead);
		SetTail(newTail);

		Update();
	}

	public void SetHead(Vector2Int pos) {
		if (PositionCheck(pos)) 
			throw new($"bad cursor position at {pos} have {main.lines[pos.y].IndexTs.Count}, {main.lines.Count}");

		head = pos;
	}

	public void SetTail(Vector2Int pos) {
		if (PositionCheck(pos))
			throw new($"bad cursor position at {pos} have {main.lines[pos.y].IndexTs.Count}, {main.lines.Count}");
		
		tail = pos;
	}

	public void MoveHead(Vector2Int v) {
		// soley handle y
		if (v.y != 0) {
			// remember col of old head
			int oldCol = main.ColumnOfPosition(head);
			int oldLineLength = main.lines[head.y].Content.Length;

			// move head y
			head.y += v.y;
			head.y = Mathf.Clamp(head.y, 0, main.lines.Count - 1); // stoopid

			// set x
			// if head x is longer than new line, set to end (keep desired)
			head.x = main.PositionOfColumn(oldCol, head.y);

			int newLineLength = main.lines[head.y].Content.Length;

			if (oldLineLength < newLineLength) {
				head.x = main.PositionOfColumn(DesiredCol, head.y);
			}

			if (head.x > newLineLength) { // too far
				head.x = newLineLength;

				// only set desired to old if old was at desired (??)
				if (oldCol == DesiredCol)
					DesiredCol = oldCol;
			}
		}

		// handle x movement
		head.x += v.x;

		// wrap properly (?)
		head = WrapCaretPos(head);

		if (v.x != 0)
			DesiredCol = main.ColumnOfPosition(head);

		// clamp y AGAIN
		head.y = Mathf.Clamp(head.y, 0, main.lines.Count - 1);
	}

	Vector2Int WrapCaretPos(Vector2Int pos) {
		// assuming only 1 rollover, if ever more then add more
		int x = pos.x;
		int y = pos.y;

		if (x < 0 && y > 0) {
			y--;
			x = main.lines[y].Content.Length;
		} else
		if (x > main.lines[y].Content.Length && y < main.lines.Count) {
			x = 0;
			y++;
		}

		return new(x, y);
	}

	public void MatchTail() {
		tail = head;
	}

	public void Update() {
		RenderCaret();	
		HandleSelections();
	}

	int lineChars(int line) =>
			main.lines[line].Content.Length;

	void HandleSelections() {
		if (!HasSelection) {
			ClearBoxes();
			return;
		}

		if (lastState.head.y != head.y || lastState.tail.y != tail.y || boxes.Count == 0)
			MakeNewSelectionBoxes();
		else
			UpdateSelectionBoxes();

		lastState.head = head;
		lastState.tail = tail;
	}

	void ClearBoxes() {
		// reset
		if (boxes != null && boxes.Count > 0)
			foreach (var box in boxes) box.Destroy();
		boxes = new();
	}

	void MakeNewSelectionBoxes() {
		ClearBoxes();

		if (tail.y == head.y) {
			// only 1 that goes between them
			boxes.Add(new(main, head.y, tail.x, head.x));
		} else
		if (tail.y < head.y) { // tail higher
			// make one that goes from tail to its line end
			boxes.Add(new(main, tail.y, tail.x, lineChars(tail.y)));

			// make full line ones in between
			for (int l = tail.y + 1; l < head.y; l++) {
				boxes.Add(new(main, l, 0, lineChars(l)));
			}

			// make last one that goes to head from start on its line
			boxes.Add(new(main, head.y, 0, head.x));
		} else { // head higher
			// make last one that goes to head from end
			boxes.Add(new(main, head.y, head.x, lineChars(head.y)));

			// make full line ones in between
			for (int l = head.y + 1; l < tail.y; l++) {
				boxes.Add(new(main, l, 0, lineChars(l)));
			}

			// make one that goes from tail to its start
			boxes.Add(new(main, tail.y, 0, tail.x));
		}

		// realise boxes
		foreach (var box in boxes)
			box.Realise();
	}

	void UpdateSelectionBoxes() {
		if (tail.y == head.y) {
			boxes[0].Update(head.y, tail.x, head.x);
		} else
		if (tail.y < head.y) {
			boxes[0].Update(tail.y, tail.x, lineChars(tail.y));
			boxes[^1].Update(head.y, 0, head.x);
		} else {
			boxes[0].Update(head.y, head.x, lineChars(head.y));
			boxes[^1].Update(tail.y, 0, tail.x);
		}
	}

	void RenderCaret() {
		(RectTransform RT, float t) = main.GetLocation(head);

		if (!image.enabled) Debug.LogWarning("cursor image is disabled agian iditio");

		rt.SetParent(RT);
		rt.localPosition = new(t * RT.rect.width, -RT.rect.height / 2); // center 

		// blink
		float rate = Config.ScriptEditor.CursorBlinkRateMs / 1000;
		rt.gameObject.SetActive(
			(Time.time - blinkTimer) % (2 * rate) < rate);

		// color
		image.color = isHeadCaret ? headCaretColor : nonHeadCaretColor;
	}

	RectTransform MakeNewCaret() {
		GameObject newObj = new("Caret", typeof(RectTransform), typeof(Image));
		RectTransform rt = newObj.GetComponent<RectTransform>();
		image = newObj.GetComponent<Image>();

		rt.anchorMin = new(0, .5f);
		rt.anchorMax = new(0, .5f);
		rt.pivot = new(.5f, .5f);

		rt.sizeDelta = new(tempwidth, main.allLinesHeight);

		return rt;
	}

	public void Destroy() {
		if (rt != null)
			Object.Destroy(rt.gameObject);

		foreach(var box in boxes) {
			box.Destroy();
		}
	}
}