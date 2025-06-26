using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Geometry;

public static class AxleCalculationHelper {
	public static bool AxleIntersectionText(
		Assembler.Subassembly subassembly, 
		Vector3 axleEndA,
		Vector3 axleEndB) {

		// get all intersections between both ends


		// if either end is inside a part then count it too
		return false;
	}

	static List<Vector3> IntersectionsInDirection(
		Assembler.Subassembly subassembly, 
		Vector3 origin,
		Vector3 target) {

		Vector3 direction = (target - origin).normalized;
		List<float> points = new();

		foreach (var part in subassembly.parts) {
			points.AddRange(PartIntersectionsWithRay(part, origin, direction));
		}

		List<Vector3> intersectionPoints = points.Select(t => origin + direction * t).ToList();
		return intersectionPoints;
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