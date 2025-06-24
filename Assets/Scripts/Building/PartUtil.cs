using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PartUtil {
	public static Vector3[] WorldSpaceVertsOfPart(Part part) {
		Transform obj = part.transform;

		Vector3[] rawVerts = part.basePart.allVerts;
		Vector3[] transformed = new Vector3[rawVerts.Length];

		for (int v = 0; v < rawVerts.Length; v++)
			transformed[v] = obj.TransformPoint(rawVerts[v]);

		return rawVerts;
	}

	public struct Triangle {
		public Vector3 p1;
		public Vector3 p2;
		public Vector3 p3;
	}

	public static Triangle[] PartToWSTriList(Part part) {
		int[] triIndices = part.basePart.allTris;

		Vector3[] WSVerts = WorldSpaceVertsOfPart(part);
		Vector3[] WStriposes = triIndices.Select(i => WSVerts[i]).ToArray();

		Triangle[] tris = new Triangle[WStriposes.Length / 3];
		for (int i = 0; i < WStriposes.Length; i += 3) {
			int ti = i / 3; // truncates

			Triangle tri = new(){
				p1 = WStriposes[ti],
				p2 = WStriposes[ti + 1],
				p3 = WStriposes[ti + 2]
			};

			tris[ti] = tri;
		}

		return tris;
	}
}