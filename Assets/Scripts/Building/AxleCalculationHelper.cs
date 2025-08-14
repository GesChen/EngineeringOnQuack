using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Geometry;
using UnityEngine;

public static class AxleCalculationHelper {
	public static bool AxleIntersectionTest(
		AssemblerRewritten.SubAssembly subassembly, 
		Vector3 axleEndA,
		Vector3 axleEndB,
		out Vector3 jointPos) {

		var parts = BuildingManager.Instance.Parts;

		// get all intersections between both ends
		Vector3 direction = (axleEndB - axleEndA).normalized;
		List<float> points = new();

		foreach (int pi in subassembly.Parts) {
			var part = parts[pi];

			points.AddRange(PartIntersectionsWithRay(part, axleEndA, direction));
		}

		jointPos = Vector3.zero;
		if (points.Count == 0) return false;	

		// dont include intersections that extend outside the range
		float maxDistSquared = (axleEndA - axleEndB).sqrMagnitude;

		var intersectionPoints =
			points
			.Where(t => t * t < maxDistSquared)
			.Select(t => axleEndA + direction * t);

		// lots of debugging potential and probably need here :}
		var average = Vector3.zero;
		int count = 0;
		foreach (var point in intersectionPoints) {
			average += point;
			count++;
		}
		average /= count;
		jointPos = average;

		return true;
	}

	static List<float> PartIntersectionsWithRay(
		Part part,
		Vector3 origin,
		Vector3 direction) {

		Triangle[] wsTris = PartUtil.PartToWSTriList(part);

		List<float> dists = new();
		foreach (var tri in wsTris) {
			float intersect = Intersections.RayTriIntersectDist(
				origin,
				direction,
				tri.p1,
				tri.p2,
				tri.p3);

			if (intersect != -1)
				dists.Add(intersect);
		}

		return dists;
	}
}