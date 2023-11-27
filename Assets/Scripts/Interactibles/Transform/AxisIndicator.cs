using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxisIndicator : MonoBehaviour
{
	public bool inUse;
	public float alpha;
	public Quaternion rotation;
	public Color color;

	private TransformTools main;
	private Material mat;
	void Start()
	{
		main = GetComponentInParent<TransformTools>();
		mat = GetComponent<MeshRenderer>().material;
	}

	void Update()
	{
		if (inUse)
			alpha = Mathf.Lerp(alpha, main.axisIndicatorAlpha, main.alphaSmoothness);
		else
			alpha = Mathf.Lerp(alpha, 0f, main.alphaSmoothness);

		mat.color = color == Color.white ? Color.white * main.draggingWhiteIntensity : HelperFunctions.MultiplyColorByVector(main.draggingIntensity, color);
		mat.SetFloat("_Alpha", alpha);
		transform.rotation = rotation;
	}
}
