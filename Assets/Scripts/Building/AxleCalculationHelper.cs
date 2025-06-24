using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AxleCalculationHelper {
	public static bool AxleIntersectionText(
		Assembler.Subassembly subassembly, 
		Vector3 axleEndA,
		Vector3 axleEndB) {

		// ray tri intersect both ways and count?

	}

	static int IntersectionsInDirection(
		Assembler.Subassembly subassembly, 
		Vector3 origin,
		Vector3 target) {

		Vector3 direction = (target - origin).normalized;
		
	}

	static int PartIntersectionsWithRay(
		Part part,
		Vector3 origin,
		Vector3 direction) {

		PartUtil.Triangle[] wsTris = PartUtil.PartToWSTriList(part);

		int count = 0;
		foreach (var tri in wsTris) {
			float intersect = Intersections.RayTriIntersectDist(
				origin,
				direction,
				tri.p1,
				tri.p2,
				tri.p3);
		}
	}
}