using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
			.Replace(' ', '_')
			.Replace("\t", @"\t")
			.Replace("\n", @"\n")
			.Replace("\r", @"\r")
			.Replace("\v", @"\v")
			.Replace("\f", @"\f")
			.Replace("\0", @"\0");
	}

	// inheritly a very slow op, prepare for >.1 ms times
	public static Vector2 TextWidthExact(string text, TextMeshProUGUI source) {
		string originalText = source.text; // copy

		source.text = text; // set new

		source.ForceMeshUpdate(); // probably faster calculation instead of whole canvas update
								  //Canvas.ForceUpdateCanvases(); // update

		// get size
		float preferredWidth = LayoutUtility.GetPreferredWidth(source.rectTransform);
		float preferredHeight = LayoutUtility.GetPreferredHeight(source.rectTransform);
		Vector2 preferredSize = new(preferredWidth, preferredHeight);

		source.text = originalText; // reset

		return preferredSize;
	}

	public static float TextWidthApproximation(string text, TMP_FontAsset fontAsset, int fontSize) {
		// Compute scale of the target point size relative to the sampling point size of the font asset.
		float pointSizeScale = fontSize / (fontAsset.faceInfo.pointSize * fontAsset.faceInfo.scale);
		float emScale = fontSize * 0.01f;

		float styleSpacingAdjustment = 0; // (style & FontStyles.Bold) == FontStyles.Bold ? fontAsset.boldSpacing : 0;
		float normalSpacingAdjustment = fontAsset.normalSpacingOffset;

		float width = 0;

		for (int i = 0; i < text.Length; i++) {
			char unicode = text[i];
			// Make sure the given unicode exists in the font asset.
			if (fontAsset.characterLookupTable.TryGetValue(unicode, out TMP_Character character))
				width += character.glyph.metrics.horizontalAdvance * pointSizeScale + (styleSpacingAdjustment + normalSpacingAdjustment) * emScale;
		}

		return width;
	}

	public static float Mod(float a, float b) => (Mathf.Abs(a * b) + a) % b;
	public static int Mod(int a, int b) => (Mathf.Abs(a * b) + a) % b;

	public static void Test(Action toTest, int iters) {
		System.Diagnostics.Stopwatch sw = new();
		sw.Start();

		for (int i = 0; i < iters; i++) {
			toTest();
		}
		sw.Stop();

		double ms = sw.Elapsed.TotalMilliseconds;
		double ns = ms * 1e6;
		long fps = (long)(1e9 / ns);

		WarnColor($"{toTest.Method.Name}: {ns} ns ({ms:F10} ms) ({fps} fps)", MoreColors.Crimson);

		if (iters > 1)
			WarnColor($"average {ns / iters} ns ({(ms / iters):F10} ms) ({(long)(1e9 / (ns / iters))}) each", MoreColors.Crimson);
	}

	public static Vector2? UVOfHover(RaycastResult result) {
		RectTransform rt = result.gameObject.GetComponent<RectTransform>();

		Vector3[] corners = new Vector3[4];
		rt.GetWorldCorners(corners);

		Vector3? worldspaceHit = 
			RayPlanarQuadIntersect(result.screenPosition, Vector3.forward, corners);

		if (!worldspaceHit.HasValue)
			return null; // ray didn't actually hit

		return UVOf3DPointOnQuad(corners[0], corners[3], corners[1], worldspaceHit.Value);
	}

	public static Vector2? RectScreenSpaceMouseUV(RectTransform rt) {
		Vector3[] corners = new Vector3[4];
		rt.GetWorldCorners(corners);

		Vector2 screenSpaceMousePos = Mouse.current.position.value;

		Vector3? planeIntersect = RayPlaneOfQuadIntersect(screenSpaceMousePos, Vector3.forward, corners);
		if (!planeIntersect.HasValue) return null;

		return UVOf3DPointOnQuad(corners[0], corners[3], corners[1], planeIntersect.Value);
	}

	// point must be on the plane of the quad
	public static Vector2 UVOf3DPointOnQuad(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 point) {
		return new(
			UVAxis(bottomLeft, bottomRight, point),
			UVAxis(bottomLeft, topLeft, point));
	}

	public static float UVAxis(Vector3 origin, Vector3 directionVector, Vector3 point) {
		return Vector3.Dot(point - origin, (directionVector - origin).normalized) / Vector3.Distance(origin, directionVector);
	}

	// just intersects with the plane, doesnt necessarily have to be in the quad
	public static Vector3? RayPlaneOfQuadIntersect(Vector3 rayOrigin, Vector3 rayDir, Vector3[] points) {
		// using unity's rect corners function ordering of points

		// Step 1: Calculate the normal of the plane
		Vector3 v1 = points[1] - points[0];
		Vector3 v2 = points[3] - points[0];
		Vector3 normal = Vector3.Cross(v1, v2);

		// Step 2: Find intersection with the plane
		float denom = Vector3.Dot(normal, rayDir);
		if (Mathf.Abs(denom) < Mathf.Epsilon) // Line is parallel to the plane
			return null;

		float t = Vector3.Dot(normal, points[0] - rayOrigin) / denom;

		// Step 3: Find the point of intersection on the line
		Vector3 intersectionPoint = rayOrigin + t * rayDir;

		return intersectionPoint;
	}

	// intersect the quad, return false if no intersect with the quad
	public static Vector3? RayPlanarQuadIntersect(Vector3 rayOrigin, Vector3 rayDir, Vector3[] points) {
		Vector3? intersectionPoint = RayPlaneOfQuadIntersect(rayOrigin, rayDir, points);

		if (!intersectionPoint.HasValue)
			return null;

		// Step 4: Check if the intersection point is inside the quad
		if (IsPointInQuad(intersectionPoint.Value, points[0], points[1], points[2], points[3]))
			return intersectionPoint;
		
		return null;
	}

	public static bool IsPointInQuad(Vector3 point, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
		bool b0 = CrossProductSign(point, p0, p1) < 0.0f;
		bool b1 = CrossProductSign(point, p1, p2) < 0.0f;
		bool b2 = CrossProductSign(point, p2, p3) < 0.0f;
		bool b3 = CrossProductSign(point, p3, p0) < 0.0f;

		return (b0 == b1) && (b1 == b2) && (b2 == b3);
	}

	public static float CrossProductSign(Vector3 p1, Vector3 p2, Vector3 p3) {
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}
}