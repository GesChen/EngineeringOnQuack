using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomVerticalLayout : MonoBehaviour
{
	public float separation;
	[HideInNormalInspector] public List<RectTransform> cells = new();
	[HideInNormalInspector] public float totalHeight;
	[HideInNormalInspector] public float maxWidth;

	RectTransform rt;

	void Start() {
		rt = GetComponent<RectTransform>();
	}

	void Update()
	{
		cells = new();
		foreach (Transform child in transform)
		{
			cells.Add(child.GetComponent<RectTransform>());
		}

		maxWidth = -1;
		totalHeight = 0;
		foreach (RectTransform t in cells)
		{
			t.localPosition = Vector2.up * totalHeight;
			totalHeight -= t.rect.height + separation;

			maxWidth = Mathf.Max(maxWidth, t.rect.width);
		}
		totalHeight *= -1;

		rt.sizeDelta = new(maxWidth, totalHeight);
	}
}
