using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using static ScriptEditor;

public class SelectionBox {
	public ScriptEditor main; // might remove later, might make static idk or singleton

	public int line;
	public int start;
	public int end;

	public RectTransform image;
	
	public SelectionBox(
		ScriptEditor se,
		int line,
		int sI,
		int sE) {

		main = se;
		this.line = line;
		start = sI;
		end = sE;
	}

	public RectTransform Realise(){
		GameObject newImage = new("Selection", typeof(RectTransform), typeof(Image));
		image = newImage.GetComponent<RectTransform>();

		Update();

		return image;
	}

	public void Update() {
		(RectTransform lineRT, float sT) = main.GetLocation(new(start, line));
		(RectTransform _, float eT) = main.GetLocation(new(end, line));

		image.SetParent(lineRT);

		image.anchorMin = Vector2.zero;
		image.anchorMax = Vector2.one;
		image.pivot = new(.5f, .5f);

		float sX = sT * lineRT.rect.width;
		float eX = eT * lineRT.rect.width;

		image.offsetMin = new(sX, 0);
		image.offsetMax = new(eX - lineRT.rect.width, 0);
	}
}
