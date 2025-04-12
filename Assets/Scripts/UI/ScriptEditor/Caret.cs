using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Caret {
	public ScriptEditor main;

	public Vector2Int head;
	public Vector2Int tail;
	public List<SelectionBox> boxes;

	public float tempwidth = 2;

	float initTime;
	RectTransform rt; // turn this kinda thing into a struct maybe 
	(Vector2Int head, Vector2Int tail) lastState;

	public Caret(ScriptEditor se, Vector2Int pos) {
		Debug.Log("making new");
		main = se;
		head = pos;
		tail = pos;
	}

	public void Initialize() {
		initTime = Time.time;
		rt = MakeNewCaret();
	}

	public void UpdatePos(Vector2Int pos) {
		initTime = Time.time;
		tail = pos;
		head = pos;

		Update();
	}

	public void Update() {
		RenderCaret();
		HandleSelections();
	}

	int lineChars(int line) =>
			main.lines[line].content.Length;

	void HandleSelections() {
		if (tail == head) {
			if (boxes != null && boxes.Count > 0) {
				foreach (var box in boxes) box.Destroy();
				boxes.Clear();
			}
			return;
		}

		if (lastState.head.y != head.y || lastState.tail.y != tail.y)
			MakeSelectionBoxes();
		else
			UpdateSelectionBoxes();

		lastState.head = head;
		lastState.tail = tail;
	}

	void MakeSelectionBoxes() {
		// reset
		if (boxes != null && boxes.Count > 0)
			foreach (var box in boxes) box.Destroy();
		boxes = new();

		if (tail.y == head.y) {
			// only 1 that goes between them
			boxes.Add(new(main, head.y, tail.x, head.x));
		}
		else
		if (tail.y < head.y) {
			// make one that goes from tail to its line end
			boxes.Add(new(main, tail.y, tail.x, lineChars(tail.y)));

			// make full line ones in between
			for (int l = tail.y + 1; l < head.y; l++) {
				boxes.Add(new(main, l, 0, lineChars(l)));
			}

			// make last one that goes to head from start on its line
			boxes.Add(new(main, head.y, 0, head.x));
		}
		else {
			// make one that goes from tail to its start
			boxes.Add(new(main, tail.y, 0, tail.x));

			// make full line ones in between
			for (int l = tail.y - 1; l > head.y; l--) {
				boxes.Add(new(main, l, 0, lineChars(l)));
			}

			// make last one that goes to head from end
			boxes.Add(new(main, head.y, head.x, lineChars(head.y)));
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
			boxes[0].Update(tail.y, 0, tail.x);
			boxes[^1].Update(head.y, head.x, lineChars(head.y));
		}
	}

	void RenderCaret() {
		(RectTransform RT, float t) = main.GetLocation(head);

		rt.SetParent(RT);
		rt.localPosition = new(t * RT.rect.width, -RT.rect.height / 2); // center 

		// blink
		float rate = SEConfig.DefaultCursorBlinkRateMs / 1000;
		rt.gameObject.SetActive(
			(Time.time - initTime) % (2 * rate) < rate);
	}

	RectTransform MakeNewCaret() {
		GameObject newObj = new("Caret", typeof(RectTransform), typeof(Image));
		RectTransform rt = newObj.GetComponent<RectTransform>();

		rt.anchorMin = new(0, .5f);
		rt.anchorMax = new(0, .5f);
		rt.pivot = new(.5f, .5f);

		rt.sizeDelta = new(tempwidth, main.allLinesHeight);

		return rt;
	}

	public void Destroy() {
		if (rt != null)
			Object.Destroy(rt.gameObject);
	}
}