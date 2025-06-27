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

		Vector3[] rawVerts = part.basePart.allVerts;
		Vector3[] transformed = new Vector3[rawVerts.Length];
/*
		for (int v = 0; v < rawVerts.Length; v++)
			transformed[v] = obj.TransformPoint(rawVerts[v]);*/

		obj.TransformPoints(rawVerts, transformed);

		return rawVerts;
	}

	public static Triangle[] PartToWSTriList(Part part) {
		int[] triIndices = part.basePart.allTris;

		Vector3[] WSVerts = WorldSpaceVertsOfPart(part);
		Vector3[] WStriposes = new Vector3[triIndices.Length];

		for (int i = 0; i < triIndices.Length; i++) {
			int index = triIndices[i];
			WStriposes[i] = WSVerts[index];
		}

		return Triangle.FromVertexArray(WStriposes);
	}
}