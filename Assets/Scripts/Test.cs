using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class Test : MonoBehaviour
{
	public float step;

	public Transform obj;
	public Transform testcollision;
	public int precision;
	bool trying = false;
	Vector3 startPos;
	private void Start()
	{
		startPos = obj.position;
	}
	void Update()
	{
		if (Input.GetKeyDown("e"))
		{
			//StartCoroutine(Snapping.SnapCo(obj, Camera.main, 15));
			startPos = obj.position;
			trying = !trying;
		}
		
		if (trying)
		{
			double start = Time.realtimeSinceStartupAsDouble;
			obj.position = testcollision.position;
			obj.rotation = testcollision.rotation;
			obj.localScale = testcollision.localScale;
			//Debug.Log(Intersections.MeshesIntersect(obj, testcollision));
			Snapping.Snap(obj);
			Debug.Log(Time.realtimeSinceStartupAsDouble - start);
			Debug.Log(1 / (Time.realtimeSinceStartupAsDouble - start));
		}
	}
}