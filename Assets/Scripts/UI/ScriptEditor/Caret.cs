using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Caret : MonoBehaviour {
	public ScriptEditor main;

	public Vector2Int head;
	public Vector2Int tail;
	public List<SelectionBox> boxes;

	public float tempwidth;

	RectTransform rt; // turn this kinda thing into a struct maybe 

	(Vector2Int head, Vector2Int tail) lastState;
	void Update() {
		RenderCaret();

		if (lastState.head.y != head.y || lastState.tail.y != tail.y)
			MakeSelectionBoxes();	
		else
			UpdateSelectionBoxes();

		lastState.head = head;
		lastState.tail = tail;
	}

	int lineChars(int line) =>
			main.lines[line].content.Length;

	void MakeSelectionBoxes() {
		// reset
		if (boxes != null && boxes.Count > 0)
			foreach (var box in boxes) if (box.boxObject != null)
				Destroy(box.boxObject.gameObject);
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
		if (rt == null) {
			rt = MakeNewCaret();
		}

		(RectTransform RT, float t) = main.GetLocation(head);

		rt.SetParent(RT);
		rt.localPosition = new(t * RT.rect.width, -RT.rect.height / 2); // center 
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
}
