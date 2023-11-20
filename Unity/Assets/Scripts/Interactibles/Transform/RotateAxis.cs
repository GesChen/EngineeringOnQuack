using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAxis : MonoBehaviour
{ 
	public Vector3 axis;

	private TransformTools main;
	private Material mat;

	private Transform[] samplePoints;
	void Awake()
	{
		main = GetComponentInParent<TransformTools>();
	}

	void Update()
	{

	}
	float PointToPolygonDistance(Vector2 point, Vector2[] polygon)
	{
		float minDistance = float.MaxValue;

		for (int i = 0; i < polygon.Length; i++)
		{
			Vector2 p1 = polygon[i];
			Vector2 p2 = polygon[(i + 1) % polygon.Length];

			float currentDistance = PointToLineDistance(point, p1, p2);
			minDistance = Mathf.Min(minDistance, currentDistance);
		}

		return minDistance;
	}

	float PointToLineDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
	{
		float numerator = Mathf.Abs((lineEnd.y - lineStart.y) * point.x - (lineEnd.x - lineStart.x) * point.y + lineEnd.x * lineStart.y - lineEnd.y * lineStart.x);
		float denominator = Mathf.Sqrt(Mathf.Pow(lineEnd.y - lineStart.y, 2) + Mathf.Pow(lineEnd.x - lineStart.x, 2));

		return numerator / denominator;
	}
}
