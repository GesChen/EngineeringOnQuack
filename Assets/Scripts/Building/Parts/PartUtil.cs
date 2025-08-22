using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Geometry;

public static class PartUtil {

	// these methods  seem really unoptimized so i wouldn;t use them.
	public static Vector3[] WorldSpaceVertsOfPart(Part part) {
		Transform obj = part.transform;

		Vector3[] verts = part.basePart.AllVerts.ToArray();
/*
		for (int v = 0; v < rawVerts.Length; v++)
			transformed[v] = obj.TransformPoint(rawVerts[v]);*/

		obj.TransformPoints(verts);

		return verts;
	}

	public static Triangle[] PartToWSTriList(Part part) {
		int[] triIndices = part.basePart.AllTris;

		Vector3[] WSVerts = WorldSpaceVertsOfPart(part);
		Vector3[] WStriposes = new Vector3[triIndices.Length];

		for (int i = 0; i < triIndices.Length; i++) {
			int index = triIndices[i];
			WStriposes[i] = WSVerts[index];
		}

		return Triangle.FromVertexArray(WStriposes);
	}
}