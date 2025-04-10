using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class SelectionBox {
	public ScriptEditor main; // might remove later, might make static idk or singleton

	public RectTransform curLine;
	public float startT;
	public int startIndex;
	public float endT;
	public int endIndex;

	public RectTransform image;
	
	public void Select(ScriptEditor se, RectTransform line, int from, int to) {
		main = se;
		curLine = line;

		GameObject newImage = new("Selection");
		image = newImage.AddComponent<RectTransform>();
		image.SetParent(curLine);

		image.anchorMin = Vector2.zero;
		image.anchorMax = Vector2.one;
		image.pivot = new(.5f,.5f);

		image.offsetMax = Vector2.zero;
		image.offsetMin = Vector2.zero;
	}

}
