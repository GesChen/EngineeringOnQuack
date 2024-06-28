using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxisIndicator : MonoBehaviour
{
	private TransformTools main;
	private Material mat;
	void Awake()
	{
		main = GetComponentInParent<TransformTools>();
		mat = GetComponent<MeshRenderer>().material;
	}

	public void UpdateIndicator(Vector3 position, Quaternion rotation, Color color, float length = 2)
	{
		transform.SetPositionAndRotation(position, rotation);
		transform.localScale = new Vector3(.015f, .015f, length);
		mat.color = color == Color.white ? Color.white * main.draggingWhiteIntensity : HF.MultiplyColorByVector(main.draggingIntensity, color);
		mat.SetFloat("_Alpha", main.axisIndicatorAlpha);
	}
	/*
	public void Update()
	{
		// main update loop responsible for updating alpha (constantly changing)
		if (inUse)
			alpha = Mathf.Lerp(alpha, main.axisIndicatorAlpha, main.alphaSmoothness);
		else
			alpha = Mathf.Lerp(alpha, 0f, main.alphaSmoothness);

	}*/
}
