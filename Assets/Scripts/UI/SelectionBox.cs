using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class SelectionBox {
	public ScriptEditor main; // might remove later, might make static idk or singleton

	public int line;
	public int start;
	public int end;

	public bool fullLine;

	public RectTransform boxObject;

	public SelectionBox(
		ScriptEditor se,
		int line,
		int sI,
		int sE,
		bool full) {

		main = se;
		this.line = line;
		if (sI < sE) {
			start = sI;
			end = sE;
		} else {
			start = sE;
			end = sI;
		}

		fullLine = full;
	}

	public RectTransform Realise(){
		GameObject newImage = new("Selection", typeof(RectTransform), typeof(Image));
		boxObject = newImage.GetComponent<RectTransform>();
		newImage.GetComponent<Image>().color = main.selectionColor;

		boxObject.anchorMin = Vector2.zero;
		boxObject.anchorMax = Vector2.one;
		boxObject.pivot = new(.5f, .5f);

		Update();

		return boxObject;
	}

	public void Update() {
		if (boxObject == null) {
			Debug.Log($"ur dead bruh istg if u break again im crashing out");
			return;
		}

		(RectTransform lineRT, float sT) = main.GetLocation(new(start, line));
		(RectTransform _, float eT) = main.GetLocation(new(end, line));

		boxObject.SetParent(lineRT);

		float sX = sT * lineRT.rect.width;
		float eX = eT * lineRT.rect.width;

		boxObject.offsetMin = new(sX, 0);
		boxObject.offsetMax = new(eX - lineRT.rect.width, 0);
	}

	public void Update(int line, int start, int end) {
		this.line = line;

		if (start < end) {
			this.start = start;
			this.end = end;
		} else {
			this.start = end;
			this.end = start;
		}

		Update();
	}

	public void Destroy() {
		if (boxObject != null)
			Object.Destroy(boxObject.gameObject);
	}
}
