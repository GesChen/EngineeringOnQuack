using System.Collections.Generic;
using UnityEngine;

public class TranslateMain : MonoBehaviour
{
	public TransformTools main;
	public Translate[] interactiveElements;

	[Header("axis-color unserializable dictionary workaround")]
	public List<Vector3> axes;
	public List<Color> colors;

	public Dictionary<Vector3, Color> colorOfAxes;
	private void Awake()
	{
		colorOfAxes = new();
		for (int i = 0; i < axes.Count; i++)
		{
			colorOfAxes[axes[i]] = colors[i];
		}
	}

	void Update()
	{
		foreach (Translate translate in interactiveElements)
			translate.enabled = main.translating;

		transform.localScale = main.translating ? Vector3.one : Vector3.zero;
	}
}
