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
	public static Vector3 ClosestPointAOnTwoLines(Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
	{
		Vector3 deltaP = linePoint2 - linePoint1;
		float a = Vector3.Dot(lineVec1, lineVec1);
		float b = Vector3.Dot(lineVec1, lineVec2);
		float c = Vector3.Dot(lineVec2, lineVec2);
		float d = Vector3.Dot(lineVec1, deltaP);
		float e = Vector3.Dot(lineVec2, deltaP);

		float denom = a * c - b * b;

		if (denom == 0)
		{
			// Lines are parallel
			return Vector3.zero;
		}
		float t1 = (b * e - c * d) / denom;
		//float t2 = (a * e - b * d) / denom;

		return linePoint1 + t1 * -lineVec1;
		//closestPoint2 = p2 + t2 * v2;
	}
	public static Vector3 RayPlaneIntersect(Vector3 planePoint, Vector3 planeNormal, Vector3 rayOrigin, Vector3 rayDirection)
	{
		float t = Vector3.Dot(planeNormal, planePoint - rayOrigin) / Vector3.Dot(planeNormal, rayDirection);
		Vector3 intersectionPoint = rayOrigin + t * rayDirection;

		float epsilon = 0.0001f;
		if (t < epsilon)
			return Vector3.zero;

		return intersectionPoint;
	}
	public static Color MultiplyColorByVector(Vector3 vector, Color color)
	{
		return new Color(color.r * vector.x, color.g * vector.y, color.b * vector.z, color.a);
	}
	public static Vector3 MV3(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
}
