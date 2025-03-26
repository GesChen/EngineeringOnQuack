using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class HF {
	#region Base Class Extensions
	public static Color MultiplyColorByVector(Vector3 vector, Color color) {
		return new Color(color.r * vector.x, color.g * vector.y, color.b * vector.z, color.a);
	}

	public static Vector3 MV3(Vector3 a, Vector3 b)
		=> new(a.x * b.x, a.y * b.y, a.z * b.z);

	public static Vector3 Vector3Round(Vector3 v)
		=> new(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));

	public static Vector3 LerpByVector3(Vector3 a, Vector3 b, Vector3 t) {
		return new Vector3(
			Mathf.Lerp(a.x, b.x, t.x),
			Mathf.Lerp(a.y, b.y, t.y),
			Mathf.Lerp(a.z, b.z, t.z));
	}

	public static Vector2 Vector2Round(Vector2 v)
		=> new(Mathf.Round(v.x), Mathf.Round(v.y));

	public static Vector2 Vector2Abs(Vector2 v)
		=> new(Mathf.Abs(v.x), Mathf.Abs(v.y));
	#endregion

	private static void OldLogColor(string str, Color color) {
		Debug.Log(string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), str));
	}

	public static void LogColor(string str, Color color) {
		string colorTag = string.Format("<color=#{0:X2}{1:X2}{2:X2}>", (byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f));
		string[] lines = str.Split('\n');
		string coloredText = string.Join("\n", lines.Select(line => colorTag + line + "</color>"));
		Debug.Log(coloredText);
	}
	public static void WarnColor(string str, Color color) {
		string colorTag = string.Format("<color=#{0:X2}{1:X2}{2:X2}>", (byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f));
		string[] lines = str.Split('\n');
		string coloredText = string.Join("\n", lines.Select(line => colorTag + line + "</color>"));
		Debug.LogWarning(coloredText);
	}

	public static float PointToPolygonEdgeDistance(Vector2 point, Vector2[] polygonVertices) {
		float minDistance = float.MaxValue;

		for (int i = 0; i < polygonVertices.Length; i++) {
			Vector2 p1 = polygonVertices[i];
			Vector2 p2 = polygonVertices[(i + 1) % polygonVertices.Length];

			float distance = PointToLineSegmentDistance(point, p1, p2);

			if (distance < minDistance) {
				minDistance = distance;
			}
		}

		return minDistance;
	}

	public static float PointToLineSegmentDistance(Vector2 point, Vector2 p1, Vector2 p2) {
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

	public static Vector3 ClosestPointAOnTwoLines(Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2) {
		Vector3 deltaP = linePoint2 - linePoint1;
		float a = Vector3.Dot(lineVec1, lineVec1);
		float b = Vector3.Dot(lineVec1, lineVec2);
		float c = Vector3.Dot(lineVec2, lineVec2);
		float d = Vector3.Dot(lineVec1, deltaP);
		float e = Vector3.Dot(lineVec2, deltaP);

		float denom = a * c - b * b;

		if (denom == 0) {
			// Lines are parallel
			return Vector3.zero;
		}
		float t1 = (b * e - c * d) / denom;
		//float t2 = (a * e - b * d) / denom;

		return linePoint1 + t1 * -lineVec1;
		//closestPoint2 = p2 + t2 * v2;
	}

	public static Vector3 RayPlaneIntersect(Vector3 planePoint, Vector3 planeNormal, Vector3 rayOrigin, Vector3 rayDirection) {
		float t = Vector3.Dot(planeNormal, planePoint - rayOrigin) / Vector3.Dot(planeNormal, rayDirection);
		Vector3 intersectionPoint = rayOrigin + t * rayDirection;

		float epsilon = 0.0001f;
		if (t < epsilon)
			return Vector3.zero;

		return intersectionPoint;
	}

	public static bool Vector2InAABB(Vector2 point, Vector2 min, Vector2 max) {
		return point.x < max.x && point.y < max.y && point.x > min.x && point.y > min.y;
	}

	public static bool Vector2InRectTransform(Vector2 point, RectTransform rect) {
		Vector3[] corners = new Vector3[4];
		rect.GetWorldCorners(corners);
		Vector2 min = corners[0];
		Vector2 max = corners[2];
		return Vector2InAABB(point, min, max);
	}


	public static float DistanceInDirection(Vector3 point, Vector3 reference, Vector3 direction)
		=> Vector3.Dot(point - reference, direction);

	public static Vector2 CoordinatesOfPointOnPlane(Vector3 point, Vector3 planeOrig, Vector3 planeXDir, Vector3 planeYDir)
		=> new(Vector3.Dot(point - planeOrig, planeXDir), Vector3.Dot(point - planeOrig, planeYDir));

	public static Vector3 ProjectPointOntoPlane(Vector3 point, Vector3 planeOrig, Vector3 planeNormal) {
		float dist = Vector3.Dot(point - planeOrig, planeNormal);
		return point - dist * planeNormal;
	}

	public static string ReplaceSection(string original, int startIndex, int endIndex, string replaceWith)
		=> original[..startIndex] + replaceWith + original[(endIndex + 1)..];

	public static void ReplaceRange<T>(List<T> originalList, int startIndexInc, int endIndexInc, List<T> replacementList) {
		originalList.RemoveRange(startIndexInc, endIndexInc - startIndexInc + 1);
		originalList.InsertRange(startIndexInc, replacementList);
	}

	/*public static List<T> FasterReplaceRange<T>(List<T> values, int startIndexInc, int endIndexInc, T[] replacement) {
		if (endIndexInc < startIndexInc) throw new("End index cannot be before start");
		int count = endIndexInc - startIndexInc + 1;

		T[] original = values.ToArray();
		T[] replaced = new T[values.Count - count + replacement.Length];
		Array.Copy(original, replaced, startIndexInc - 1);
		Array.Copy(replacement, 0, replaced, startIndexInc, replacement.Length);
		Array.Copy(original, endIndexInc + 1, replaced, startIndexInc + replacement.Length, original.Length - endIndexInc);

		return replaced.ToList();
	}*/

	public static bool FasterStartsWith(string target, string prefix) {
		if (target == null || prefix == null) return false;
		if (prefix.Length > target.Length) return false;

		for (int i = 0; i < prefix.Length; i++)
			if (target[i] != prefix[i])
				return false;
		return true;
	}

	public static string Repr(string input) {
		return input
			.Replace("\t", @"\t")
			.Replace("\n", @"\n")
			.Replace("\r", @"\r")
			.Replace("\v", @"\v")
			.Replace("\f", @"\f")
			.Replace("\0", @"\0");
	}
}