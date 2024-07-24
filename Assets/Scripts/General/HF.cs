using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public static class HF {
	#region Base Class Extensions
	public static Color MultiplyColorByVector(Vector3 vector, Color color)
	{
		return new Color(color.r * vector.x, color.g * vector.y, color.b * vector.z, color.a);
	}

	public static Vector3 MV3(Vector3 a, Vector3 b)
		=> new(a.x * b.x, a.y * b.y, a.z * b.z);

	public static Vector3 Vector3Round(Vector3 v)
		=> new(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));

	public static Vector3 LerpByVector3(Vector3 a, Vector3 b, Vector3 t)
	{
		return new Vector3(
			Mathf.Lerp(a.x, b.x, t.x),
			Mathf.Lerp(a.y, b.y, t.y),
			Mathf.Lerp(a.z, b.z, t.z));
	}

	public static Vector2 Vector2Round(Vector2 v)
		=> new(Mathf.Round(v.x), Mathf.Round(v.y));

	public static Vector2 Vector2Abs(Vector2 v)
		=> new (Mathf.Abs(v.x), Mathf.Abs(v.y));
	#endregion


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

	public static bool Vector2InAABB(Vector2 point, Vector2 min, Vector2 max)
	{
		return point.x < max.x && point.y < max.y && point.x > min.x && point.y > min.y;
	}

	public static bool Vector2InRectTransform(Vector2 point, RectTransform rect)
	{
		Vector3[] corners = new Vector3[4];
		rect.GetWorldCorners(corners);
		Vector2 min = corners[0];
		Vector2 max = corners[2];
		return Vector2InAABB(point, min, max);
	}


	public static float DistanceInDirection(Vector3 point, Vector3 reference, Vector3 direction)
		=> Vector3.Dot(point - reference, direction);
	
	public static Vector2 CoordinatesOfPointOnPlane(Vector3 point, Vector3 planeOrig, Vector3 planeXDir, Vector3 planeYDir)
		=> new (Vector3.Dot(point - planeOrig, planeXDir), Vector3.Dot(point - planeOrig, planeYDir));
	
	public static Vector3 ProjectPointOntoPlane(Vector3 point, Vector3 planeOrig, Vector3 planeNormal)
	{
		float dist = Vector3.Dot(point - planeOrig, planeNormal);
		return point - dist * planeNormal;
	}

	public static string ReplaceSection(string original, int startIndex, int endIndex, string replaceWith)
		=> original[..startIndex] + replaceWith + original[(endIndex + 1)..];

	public static string ConvertToString(dynamic value)
		=> ConvertToString(value, true);
	public static string ConvertToString(dynamic value, bool stringQuotes)
	{
		Type t = value.GetType();
		if (value == null) return "";
		else if (value is string) return stringQuotes ? $"\"{value}\"" : value;
		else if (value is int || value is float) return value.ToString("G10");
		else if (value is bool) return value ? "true" : "false";
		else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
		{
			string builtString = "[";
			for (int i = 0; i < value.Count; i++)
			{
				builtString += ConvertToString(value[i], stringQuotes);
				if (i < value.Count - 1) builtString += ", ";
			}
			builtString += "]";
			return builtString;
		}
		else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
		{
			List<string> keys = new(value.Keys);
			string builtString = "{";
			for (int i = 0; i < keys.Count; i++)
			{
				dynamic key = keys[i];
				builtString += ConvertToString(key, stringQuotes);
				builtString += " : ";
				builtString += ConvertToString(value[key], stringQuotes);
				if (i != keys.Count - 1) builtString += ", ";
			}
			builtString += "}";
			return builtString;
		}
		else if (t.Name == "ScriptLine") return value.Line;
		return value.ToString();
	}


	public static bool ContainsSubstringOutsideQuotes(string text, string substring)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(substring))
		{
			return false;
		}

		bool inQuotes = false;
		int prevCharIndex = -1;

		for (int i = 0; i < text.Length; i++)
		{
			char currentChar = text[i];

			// Handle escaped quotes within quotes
			if (currentChar == '"' && prevCharIndex >= 0 && text[prevCharIndex] == '\\')
			{
				prevCharIndex = -1;
				continue;
			}

			// Toggle quote state
			if (currentChar == '"')
			{
				inQuotes = !inQuotes;
			}

			// Check substring if not within quotes
			else if (!inQuotes)
			{
				if (text[i..].StartsWith(substring))
				{
					return true;
				}
			}

			prevCharIndex = i;
		}

		return false;
	}

	public static string DetermineTypeFromString(string s)
	{
		if (s.Length == 0) return null;

		if (s[0] == '"' && s[^1] == '"') return "string";
		else if (s[0] == '"' && s[^1] != '"' || s[0] != '"' && s[^1] == '"') return "malformed string"; // start is " but not end, or end is " but not start

		if (s[0] == '[' && s[^1] == ']') return "list";
		else if (s[0] == '[' && s[^1] != ']' || s[0] != '[' && s[^1] == ']') return "malformed list"; // start is " but not end, or end is " but not start

		bool isnumber = true;
		foreach (char c in s) if (!(char.IsDigit(c) || c == '.' || c == '-')) isnumber = false;
		if (isnumber) return "number";

		if (s == "true" || s == "false") return "bool";
		return "variable"; //TODO!!!!!!!!!!
	}

	public static string DetermineTypeFromVariable(dynamic v)
	{
		if (v is string) return "string";
		else if (v is int || v is float || v is long) return "number";
		else if (v is bool) return "bool";
		else if (v is List<dynamic>) return "list";
		return "unknown";
	}

	public static bool VariableNameIsValid(string name)
	{
		/* naming convention:
		 - starts either with letter or _
		 - following characters can be letter, number or _
		 - variable names are case sensitive
		*/
		if (string.IsNullOrWhiteSpace(name)) return false;
		if (!(char.IsLetter(name[0]) || name[0] == '_')) return false;

		foreach (char c in name)
			if (!(char.IsLetter(c) || char.IsDigit(c) || c == '_')) return false;

		return true;
	}
}