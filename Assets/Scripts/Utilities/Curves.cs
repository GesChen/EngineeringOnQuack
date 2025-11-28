//#define DEBUGMODE

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Curves : MonoBehaviour {
	public static List<Vector3> CatmullRomSpline(List<Vector3> points, float resolution) {
		if (resolution <= 0) {
			Debug.LogError("Resolution cannot be zero");
			return null;
		}
#if DEBUGMODE
		Debug.Log("generating catmull rom spline");
#endif

		points.Insert(0, points[0]); // duplicate first point
		points.Add(points[^1]); // duplicate last point

#if DEBUGMODE
		foreach (Vector3 point in points)
			DebugExtra.DrawPoint(point, .1f);
#endif

		List<Vector3> curve = new();

		for (int i = 0; i < points.Count - 4; i++) {
			for (float t = 0; t <= 1; t += resolution) {
				curve.Add(GetPoint(points, i, t));
			}
		}

		return curve;
	}

	private static Vector3 GetPoint(List<Vector3> points, int startIndex, float t) {
		if (startIndex + 3 >= points.Count || startIndex < 0) {
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

	// de casteljau
	public static Vector3 Bezier(Vector3[] pts, float t, bool debug = false) {
		int n = pts.Length;
		if (n == 0) return Vector3.zero;

		if (t == 0) return pts[0];
		if (t == 1) return pts[^1];

		Vector3[] tmp = new Vector3[n];
		for (int i = 0; i < n; i++) tmp[i] = pts[i];

		for (int k = n - 1; k > 0; k--) {
			for (int i = 0; i < k; i++) {
				tmp[i] = Vector3.Lerp(tmp[i], tmp[i + 1], t);
			}
		}

		if (debug) {
			int debugQuality = 10;
			Vector3[] debugPts = Enumerable.Range(0, debugQuality).Select(n =>
				Bezier(pts, Mathf.InverseLerp(0, debugQuality, n), false)).ToArray();

			DebugExtra.DrawPoly(debugPts, false, MoreColors.SlateGray);

			foreach (var p in pts)
				DebugExtra.DrawPoint(p, color: MoreColors.Tan);
		}

		return tmp[0];
	}

}