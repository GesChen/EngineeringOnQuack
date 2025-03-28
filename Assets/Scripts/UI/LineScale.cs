using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LineScale : MonoBehaviour
{
	public List<RectTransform> dependencies;
	public Vector2 extraSpace;
	public void Start()
	{		
		Canvas.ForceUpdateCanvases();

		Vector2 size = extraSpace;
		foreach (var rt in dependencies) {
			size += new Vector2(rt.rect.width, rt.rect.height);
		}

		GetComponent<RectTransform>().sizeDelta = size;
	}
}
