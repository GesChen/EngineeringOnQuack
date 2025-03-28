using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomVerticalLayout : MonoBehaviour
{
	public float separation;
	[HideInNormalInspector] public List<RectTransform> cells = new();
	[HideInNormalInspector] public float totalHeight;

	public delegate void ContentsChangedEvent();
	public ContentsChangedEvent OnContentsChanged;

	int lastCount = 0;
	void Update()
	{
		cells = new();
		foreach (Transform child in transform)
		{
			cells.Add(child.GetComponent<RectTransform>());
		}

		if (cells.Count != lastCount)
			OnContentsChanged?.Invoke();

		lastCount = cells.Count;

		totalHeight = 0;
		foreach (RectTransform t in cells)
		{
			t.localPosition = Vector2.up * totalHeight;
			totalHeight -= t.rect.height + separation;
		}
		totalHeight *= -1;

	}
}
