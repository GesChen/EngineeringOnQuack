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

	(RectTransform rt, Image im) headImageObject; // turn this kinda thing into a struct maybe 

	private void Update() {
		UpdateVisuals();
	}

	void UpdateVisuals() {
		if (headImageObject.rt == null) {
			GameObject newCaret = MakeNewCaret();

			headImageObject.rt = newCaret.GetComponent<RectTransform>();
			headImageObject.im = newCaret.GetComponent<Image>();
		}

		(RectTransform headRT, float headT) = main.GetLocation(head);

		headImageObject.rt.SetParent(headRT);
		PutLeftMiddleCenterPivot(headImageObject.rt);

		headImageObject.rt.sizeDelta = new(tempwidth, headRT.rect.height);
		headImageObject.rt.localPosition = new(headT * headRT.rect.width, -headRT.rect.height / 2); // center 
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
