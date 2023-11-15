using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
	public enum Type
	{
		axis,
		free,
		view
	}
	public Type type;
	public Vector3 axis;

	private TransformTools main;
	private Material mat;

	void Awake()
	{
		main = GetComponentInParent<TransformTools>();
	}

	// Update is called once per frame
	void Update()
	{
		// raycast method
		if (main.controls.Transform.Drag.IsPressed())
		{
			Vector2 pos = main.controls.Transform.MousePos.ReadValue<Vector2>();
			RaycastHit hit;
			if (Physics.Raycast(Camera.main.ScreenPointToRay(pos), out hit, Mathf.Infinity, LayerMask.NameToLayer("Transform")) && hit.transform == transform)
			{
				Debug.Log("hit");
			}
		}
	}
}
