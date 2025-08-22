using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LineScale : MonoBehaviour
{
	public List<RectTransform> dependencies;
	public Vector2 extraSpace;
	public bool widthMax;
	public bool heightMax;
	public void Start()
	{		
		Canvas.ForceUpdateCanvases();

		Vector2 size = extraSpace;
		Vector2 max = Vector2.negativeInfinity;
		foreach (var rt in dependencies) {
			Vector2 rtSize = new (rt.rect.width, rt.rect.height);
			size += rtSize;
			max = Vector2.Max(max, rtSize);
		}

		if (widthMax) size.x = max.x;
		if (heightMax) size.y = max.y;

		GetComponent<RectTransform>().sizeDelta = size;
	}
}
