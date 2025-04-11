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

	RectTransform headImageObject; // turn this kinda thing into a struct maybe 
	RectTransform tailImageObject; 

	void Update() {
		RenderCaret(ref headImageObject, head);

		MakeSelectionBoxes();
	}

	void MakeSelectionBoxes() {
		// reset
		if (boxes != null && boxes.Count > 0)
			foreach (var box in boxes) if (box.image != null)
				Destroy(box.image.gameObject);
		boxes = new();

		int lineChars(int line) =>
			main.lines[line].content.Length;

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

	}

	void RenderCaret(ref RectTransform rt, Vector2Int pos) {
		if (rt == null) {
			GameObject newCaret = MakeNewCaret();

			rt = newCaret.GetComponent<RectTransform>();
			//im = newCaret.GetComponent<Image>();
		}

		(RectTransform RT, float t) = main.GetLocation(pos);

		rt.SetParent(RT);
		PutLeftMiddleCenterPivot(rt);

		rt.sizeDelta = new(tempwidth, RT.rect.height);
		rt.localPosition = new(t * RT.rect.width, -RT.rect.height / 2); // center 
	}

	void PutLeftMiddleCenterPivot(RectTransform RT) {
		RT.anchorMin = new(0, .5f);
		RT.anchorMax = new(0, .5f);
		RT.pivot = new(.5f, .5f);
	}

	GameObject MakeNewCaret() {
		return new("Caret", typeof(RectTransform), typeof(Image));
	}
}
