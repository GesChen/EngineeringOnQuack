using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Geometry;

public static class PartUtil {
	public static Vector3[] WorldSpaceVertsOfPart(Part part) {
		Transform obj = part.transform;

		Vector3[] rawVerts = part.basePart.allVerts;
		Vector3[] transformed = new Vector3[rawVerts.Length];

		for (int v = 0; v < rawVerts.Length; v++)
			transformed[v] = obj.TransformPoint(rawVerts[v]);

		return rawVerts;
	}

	public static Triangle[] PartToWSTriList(Part part) {
		int[] triIndices = part.basePart.allTris;

		Vector3[] WSVerts = WorldSpaceVertsOfPart(part);
		Vector3[] WStriposes = triIndices.Select(i => WSVerts[i]).ToArray();

		return Triangle.FromVertexArray(WStriposes);
	}
}