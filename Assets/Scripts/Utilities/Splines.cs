using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Splines : MonoBehaviour
{
	public static List<Vector3> CatmullRom(List<Vector3> points, float resolution)
	{
		points.Insert(0, points[0]); // duplicate first point
		points.Add(points[^1]); // duplicate last point

		List<Vector3> curve = new();

		for (int i = 0; i < points.Count - 4; i++)
		{
			for (float t = 0; t <= 1; t += resolution)
			{
				curve.Add(GetPoint(points, i, t));
			}
		}

		return curve;
	}

	private static Vector3 GetPoint(List<Vector3> points, int startIndex, float t)
	{
		if (startIndex + 3 >= points.Count || startIndex < 0)
		{
			Debug.LogError($"point index {startIndex} contains out of range points");
			return Vector3.zero;
		}

		Vector3 p1 = points[startIndex];
		Vector3 p2 = points[startIndex + 1];
		Vector3 p3 = points[startIndex + 2];
		Vector3 p4 = points[startIndex + 3];

		float tt = t * t;
		float ttt = tt * t;

		float q1 = -ttt + 2 * tt - t;
		float q2 = 3 * ttt - 5 * tt + 2;
		float q3 = -3 * ttt + 4 * tt + t;
		float q4 = ttt - tt;

		Vector3 point = .5f * (q1 * p1 + q2 * p2 + q3 * p3 + q4 * p4);
		return point;
	}
}
