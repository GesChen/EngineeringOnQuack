using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperFunctions
{
	public static float PointToPolygonEdgeDistance(Vector2 point, Vector2[] polygonVertices)
	{
		float minDistance = float.MaxValue;

		for (int i = 0; i < polygonVertices.Length; i++)
		{
			Vector2 p1 = polygonVertices[i];
			Vector2 p2 = polygonVertices[(i + 1) % polygonVertices.Length];

			float distance = PointToLineSegmentDistance(point, p1, p2);

			if (distance < minDistance)
			{
				minDistance = distance;
			}
		}

		return minDistance;
	}

	public static float PointToLineSegmentDistance(Vector2 point, Vector2 p1, Vector2 p2)
	{
		Vector2 v = p2 - p1;
		Vector2 w = point - p1;

		float c1 = Vector2.Dot(w, v);
		if (c1 <= 0)
			return Vector2.Distance(point, p1);

		float c2 = Vector2.Dot(v, v);
		if (c2 <= c1)
			return Vector2.Distance(point, p2);

		float b = c1 / c2;
		Vector2 pb = p1 + b * v;

		return Vector2.Distance(point, pb);
	}
}
