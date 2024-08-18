//#define DEBUGMODE

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Intersections
{
	// determines if two objects are intersecting
	public static bool MeshesIntersect(Transform obj1, Transform obj2)
	{
		Mesh mesh1 = obj1.GetComponent<MeshFilter>().mesh;
		Mesh mesh2 = obj2.GetComponent<MeshFilter>().mesh;

		Vector3[] m1v = mesh1.vertices;
		Vector3[] m2v = mesh2.vertices;

		for (int i = 0; i < m1v.Length; i++)
			m1v[i] = obj1.TransformPoint(m1v[i]);
		for (int i = 0; i < m2v.Length; i++)
			m2v[i] = obj2.TransformPoint(m2v[i]);

		//Debug.Log($"bounds {BoundsIntersect(m1v, obj1, m2v, obj2)}");
		if (!BoundsIntersectWorldSpace(m1v, m2v)) return false;

		int[] m1t = mesh1.triangles;
		int[] m2t = mesh2.triangles;

		int numtris1 = m1t.Length / 3;
		int numtris2 = m2t.Length / 3;
		int combinations = numtris1 * numtris2;

		int[] indices = new int[combinations];
		float[] distances = new float[combinations];
		int[] triindex1 = new int[combinations];
		int[] triindex2 = new int[combinations];
		int count = 0;
		for (int i = 0; i < numtris1; i++)
		{
			for (int j = 0; j < numtris2; j++)
			{
				indices[count] = count;
				triindex1[count] = i;
				triindex2[count] = j;

				Vector3 avg1 =
					(m1v[m1t[i * 3]] +
					m1v[m1t[i * 3 + 1]] +
					m1v[m1t[i * 3 + 2]]) / 3f;

				Vector3 avg2 =
					(m2v[m2t[j * 3]] +
					m2v[m2t[j * 3 + 1]] +
					m2v[m2t[j * 3 + 2]]) / 3f;

				float dist = (avg1 - avg2).sqrMagnitude;
				distances[count] = dist;

				count++;
			}
		}

		indices = SortIndicesByDistance(indices, distances);

		float t = 0;
		foreach (int index in indices)
		{
			t++;
			int i = triindex1[index];
			int j = triindex2[index];

			
			Vector3 p1 = m1v[m1t[i * 3]];
			Vector3 p2 = m1v[m1t[i * 3 + 1]];
			Vector3 p3 = m1v[m1t[i * 3 + 2]];
			Vector3 q1 = m2v[m2t[j * 3]];
			Vector3 q2 = m2v[m2t[j * 3 + 1]];
			Vector3 q3 = m2v[m2t[j * 3 + 2]];

#if DEBUGMODE
			DebugExtra.DrawTriangle(p1, p2, p3, Color.red);
			DebugExtra.DrawTriangle(q1, q2, q3, Color.blue);
#endif

			if (TrianglesIntersect(
				m1v[m1t[i * 3]],
				m1v[m1t[i * 3 + 1]],
				m1v[m1t[i * 3 + 2]],
				m2v[m2t[j * 3]],
				m2v[m2t[j * 3 + 1]],
				m2v[m2t[j * 3 + 2]]
				))
			{
				return true;
			}
		}

		return false;
	}
	// expects world space verts, determines if bounds collide
	public static bool BoundsIntersectWorldSpace(Vector3[] verts1, Vector3[] verts2)
	{
		Vector3 min1 = Vector3.positiveInfinity;
		Vector3 max1 = Vector3.negativeInfinity;
		foreach (Vector3 v in verts1)
		{
			min1 = Vector3.Min(min1, v);
			max1 = Vector3.Max(max1, v);
		}

		Vector3 min2 = Vector3.positiveInfinity;
		Vector3 max2 = Vector3.negativeInfinity;
		foreach (Vector3 v in verts2)
		{
			min2 = Vector3.Min(min2, v);
			max2 = Vector3.Max(max2, v);
		}

		//DebugExtra.DrawCube(min1, max1, Color.red);
		//DebugExtra.DrawCube(min2, max2, Color.blue;

		return
			min1.x <= max2.x &&
			max1.x >= min2.x &&
			min1.y <= max2.y &&
			max1.y >= min2.y &&
			min1.z <= max2.z &&
			max1.z >= min2.z;

	}

	// quicksort, sorts indicies based on distance at that index in indices list
	private static int[] SortIndicesByDistance(int[] indices, float[] distances)
	{
		return SortIndicesByDistance(indices, distances, 0, indices.Length - 1);
	}
	private static int[] SortIndicesByDistance(int[] indices, float[] distances, int leftIndex, int rightIndex)
	{
		var i = leftIndex;
		var j = rightIndex;
		var pivot = distances[leftIndex];
		while (i <= j)
		{
			while (distances[i] < pivot)
			{
				i++;
			}

			while (distances[j] > pivot)
			{
				j--;
			}
			if (i <= j)
			{
				(distances[j], distances[i]) = (distances[i], distances[j]);
				(indices[j], indices[i]) = (indices[i], indices[j]);
				i++;
				j--;
			}
		}

		if (leftIndex < j)
			SortIndicesByDistance(indices, distances, leftIndex, j);
		if (i < rightIndex)
			SortIndicesByDistance(indices, distances, i, rightIndex);
		return indices;
	}

	// triangle-triangle intersection
	public static bool TrianglesIntersect(Vector3 a1, Vector3 a2, Vector3 a3, Vector3 b1, Vector3 b2, Vector3 b3)
	{
		// step 1: get the planes of the two triangles
		Vector3 planeDirA = Vector3.Cross(a2 - a1, a3 - a1).normalized;
		Vector3 planeDirB = Vector3.Cross(b2 - b1, b3 - b1).normalized;
		// plane origs will be any point 

		// determine intersections with planes
		List<Vector3> AIntersects = TriPlaneIntersect(b1, planeDirB, a1, a2, a3);
		List<Vector3> BIntersects = TriPlaneIntersect(a1, planeDirA, b1, b2, b3);

		// early returns if one tri doesn't intersect
		if (AIntersects.Count == 0) return false;
		if (BIntersects.Count == 0) return false;
		// both tris intersect the other plane at some place

		// get to the point on the line to measure from
		List<Vector3> intersections = AIntersects.Concat(BIntersects).ToList();
		Vector3 midPoint = Vector3.zero;
		foreach (Vector3 i in intersections)
			midPoint += i;
		midPoint /= AIntersects.Count + BIntersects.Count;

		float maxDistSquared = float.NegativeInfinity;
		foreach (Vector3 i in intersections)
		{
			float d = (i - midPoint).sqrMagnitude;
			if (d > maxDistSquared) maxDistSquared = d;
		}

		// convert intersection points into distances
		Vector3 planeIntersectionVector = (AIntersects[0] - BIntersects[0]).normalized; // 1 from each guarunteed
		Vector3 measurementPoint = midPoint + planeIntersectionVector * maxDistSquared * 2; // arbitrary point guarunteed not in 

		float[] aDists = new float[AIntersects.Count];
		float[] bDists = new float[BIntersects.Count];
		for (int i = 0; i < AIntersects.Count; i++)
			aDists[i] = (AIntersects[i] - measurementPoint).sqrMagnitude;
		for (int i = 0; i < BIntersects.Count; i++)
			bDists[i] = (BIntersects[i] - measurementPoint).sqrMagnitude;

		// check overlapping cases
		if (bDists.Length > 1)
		{
			// sorted
			if (bDists[0] > bDists[1]) (bDists[0], bDists[1]) = (bDists[1], bDists[0]);

			// either point of a inside b
			if ((aDists[0] > bDists[0] && aDists[0] < bDists[1]) ||
				(aDists[1] > bDists[1] && aDists[0] < bDists[1]))
				return true;
		}
		if (aDists.Length > 1)
		{
			// sorted
			if (aDists[0] > aDists[1]) (aDists[0], aDists[1]) = (aDists[1], aDists[0]);

			// either point of b inside a
			if ((bDists[0] > aDists[0] && bDists[0] < aDists[1]) ||
				(bDists[1] > aDists[1] && bDists[0] < aDists[1]))
				return true;
		}

		return false;
	}

	public static List<Vector3> TriPlaneIntersect(Vector3 planeOrig, Vector3 planeDir, Vector3 p1, Vector3 p2, Vector3 p3)
	{
		List<Vector3> intersects = new List<Vector3>();

		Vector3 v;
		float t;

		v = p2 - p1;
		t = Vector3.Dot(planeDir, planeOrig - p1) / Vector3.Dot(planeDir, v);
		if (t >= 0 && t <= 1) // intersects
			intersects.Add(p1 + t * v);

		v = p3 - p2;
		t = Vector3.Dot(planeDir, planeOrig - p2) / Vector3.Dot(planeDir, v);
		if (t >= 0 && t <= 1) // intersects
			intersects.Add(p2 + t * v);

		v = p1 - p3;
		t = Vector3.Dot(planeDir, planeOrig - p3) / Vector3.Dot(planeDir, v);
		if (t >= 0 && t <= 1) // intersects
			intersects.Add(p3 + t * v);

		return intersects;
	}
	
	// very slow!!! do not use.
	public static List<Vector3> TriPlaneOld(Vector3 planeOrig, Vector3 planeDir, Vector3 p1, Vector3 p2, Vector3 p3)
	{

		List<Vector3> intersects = new List<Vector3>();

		Vector3? i1 = SegmentPlaneIntersect(planeOrig, planeDir, p1, p2);
		if (i1 != null) intersects.Add(i1.Value);

		Vector3? i2 = SegmentPlaneIntersect(planeOrig, planeDir, p2, p3);
		if (i2 != null) intersects.Add(i2.Value);

		Vector3? i3 = SegmentPlaneIntersect(planeOrig, planeDir, p3, p1);
		if (i3 != null) intersects.Add(i3.Value);

		return intersects;
	}
	public static Vector3? SegmentPlaneIntersect(Vector3 planeOrig, Vector3 planeDir, Vector3 p1, Vector3 p2)
	{
		Vector3 v = p2 - p1;
		float t = Vector3.Dot(planeDir, (planeOrig - p1)) / Vector3.Dot(planeDir, v);

		if (t >= 0 && t <= 1) // intersects
			return p1 + t * v;
		return null;
	}

	// do two triangles p and q intersect?
	// OLD CODE!! dont use it, it's slow
	public static bool TrianglesIntersect2(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 q1, Vector3 q2, Vector3 q3)
	{
		// turn edges of p into rays and get distance to hit with q
		float d;
		// disgusting hack, dont normalize the direction, check the result with 1, same result
		d = RayTriIntersectDist(p1, p2 - p1, q1, q2, q3); //p1->p2
		if (d > 0 && d <= 1) return true;
		d = RayTriIntersectDist(p2, p3 - p2, q1, q2, q3); //p2->p3
		if (d > 0 && d <= 1) return true;
		d = RayTriIntersectDist(p3, p1 - p3, q1, q2, q3); //p3->p1
		if (d > 0 && d <= 1) return true;

		d = RayTriIntersectDist(q1, q2 - q1, p1, p2, p3); //q1->q2
		if (d > 0 && d <= 1) return true;
		d = RayTriIntersectDist(q2, q3 - q2, p1, p2, p3); //q2->q3
		if (d > 0 && d <= 1) return true;
		d = RayTriIntersectDist(q3, q1 - q3, p1, p2, p3); //q3->q1
		if (d > 0 && d <= 1) return true;

		return false;
	}

	// ray-triangle intersection distance, -1 is no hit
	public static float RayTriIntersectDist(Vector3 orig, Vector3 dir, Vector3 a, Vector3 b, Vector3 c)
	{
		Vector3 edge1 = b - a;
		Vector3 edge2 = c - a;
		Vector3 ray_cross_e2 = Vector3.Cross(dir, edge2);

		float inv_det = 1f / Vector3.Dot(edge1, ray_cross_e2);
		Vector3 s = orig - a;
		float u = inv_det * Vector3.Dot(s, ray_cross_e2);

		if (u < 0 || u > 1)
			return -1;

		Vector3 s_cross_e1 = Vector3.Cross(s, edge1);
		float v = inv_det * Vector3.Dot(dir, s_cross_e1);

		if (v < 0 || u + v > 1)
			return -1;

		// At this stage we can compute t to find out where the intersection point is on the line.
		return inv_det * Vector3.Dot(edge2, s_cross_e1);
	}
	
	// TODO
	// projects points of tri onto plane of other, if projected outside of tri's bounds, then return
	/*public static bool ProjectionCheck(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 q1, Vector3 q2, Vector3 q3, float rank)
	{
		DebugExtra.DrawTriangle(p1, p2, p3, Color.HSVToRGB(0, 1, rank));
		DebugExtra.DrawTriangle(q1, q2, q3, Color.HSVToRGB(.6f, 1, rank));

		Vector3 dir = Vector3.Cross(p2 - p1, p3 - p1).normalized;
		Debug.DrawRay(p1, dir);
		return false;
	}*/
	public Vector3 ProjectPointOnPlane(Vector3 point, Vector3 planeOrig, Vector3 planeNormal)
	{
		Vector3 v = point - planeOrig;
		float dist = Vector3.Dot(v, planeNormal);
		return point - dist * planeNormal;
	}
}
// tomas mullers, i doubt it would be any bit faster, probably slower even
/*
	bool triIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 q1, Vector3 q2, Vector3 q3)
	{
		Vector3 E1 = p2 - p1;
		Vector3 E2 = p3 - p1;
		Vector3 N1 = Vector3.Cross(E1, E2);
		float d1 = -Vector3.Dot(N1, p1);

		float du0 = Vector3.Dot(N1, q1) + d1;
		float du1 = Vector3.Dot(N1, q2) + d1;
		float du2 = Vector3.Dot(N1, q3) + d1;

		float du0du1 = du0 * du1;
		float du0du2 = du0 * du2;

		if (du0du1 > 0 && du0du2 > 0)
			return false;

		E1 = q2 - q1;
		E2 = q3 - q1;
		Vector3 N2 = Vector3.Cross(E1, E2);
		float d2 = -Vector3.Dot(N2, q1);

		float dv0 = Vector3.Dot(N2, p1) + d2;
		float dv1 = Vector3.Dot(N2, p2) + d2;
		float dv2 = Vector3.Dot(N2, p3) + d2;

		float dv0dv1 = dv0 * dv1;
		float dv0dv2 = dv0 * dv2;

		if (dv0dv1 > 0 && dv0dv2 > 0)
			return false;

		Vector3 D = Vector3.Cross(N1, N2);

		float max = Mathf.Max(D.x);
		int index = 0;
		float b = Mathf.Abs(D.y);
		float c = Mathf.Abs(D.z);
		if (b > max) { max = b; index = 1; }
		if (c > max) { max = c; index = 2; }

		float vp0 = index == 0 ? p1.x : (index == 1 ? p1.y : p1.z);
		float vp1 = index == 0 ? p2.x : (index == 1 ? p2.y : p2.z);
		float vp2 = index == 0 ? p3.x : (index == 1 ? p3.y : p3.z);

		float up0 = index == 0 ? q1.x : (index == 1 ? q1.y : q1.z);
		float up1 = index == 0 ? q2.x : (index == 1 ? q2.y : q2.z);
		float up2 = index == 0 ? q3.x : (index == 1 ? q3.y : q3.z);

		float isect10;
		float isect11;
		float isect20;
		float isect21;

		return coplanar_tri_tri(N1, V0, V1, V2, U0, U1, U2);


		return true;
	}
	bool COMPUTE_INTERVALS(float VV0, float VV1, float VV2, float D0, float D1, float D2, float D0D1, float D0D2, ref float isect0, ref float isect1)
	{
		if (D0D1 > 0.0f)
		{
			ISECT(VV2, VV0, VV1, D2, D0, D1, ref isect0, ref isect1);
		}
		else if (D0D2 > 0.0f)
		{
			ISECT(VV1, VV0, VV2, D1, D0, D2, ref isect0, ref isect1);
		}
		else if (D1 * D2 > 0.0f || D0 != 0.0f)
		{
			ISECT(VV0, VV1, VV2, D0, D1, D2, ref isect0, ref isect1);
		}
		else if (D1 != 0.0f)
		{
			ISECT(VV1, VV0, VV2, D1, D0, D2, ref isect0, ref isect1);
		}
		else if (D2 != 0.0f)
		{
			ISECT(VV2, VV0, VV1, D2, D0, D1, ref isect0, ref isect1);
		}
		else
		{
			return true;
		}
		return false;
	}
	void ISECT(float VV0, float VV1, float VV2, float D0, float D1, float D2, ref float isect0, ref float isect1) 
	{
		isect0 = VV0 + (VV1 - VV0) * D0 / (D0 - D1);
		isect1 = VV0 + (VV2 - VV0) * D0 / (D0 - D2);
	}
	int coplanar_tri_tri(Vector3 N, Vector3 V0, Vector3 V1, Vector3 V2, Vector3 U0, Vector3 U1, Vector3 U2)
	{
		short i0, i1;
		Vector3 A = new(
			Mathf.Abs(N.x),
			Mathf.Abs(N.y),
			Mathf.Abs(N.z)
		);

		if (A.x > A.y)
		{
			if (A.x > A.z)
			{
				i0 = 1;
				i1 = 2;
			}
			else
			{
				i0 = 0;
				i1 = 1;
			}
		}
		else
		{
			if (A.z > A.y)
			{
				i0 = 0;
				i1 = 1;
			}
			else
			{
				i0 = 0;
				i1 = 2;
			}
		}

		EDGE_AGAINST_TRI_EDGES(V0, V1, U0, U1, U2);
		EDGE_AGAINST_TRI_EDGES(V1, V2, U0, U1, U2);
		EDGE_AGAINST_TRI_EDGES(V2, V0, U0, U1, U2);

		POINT_IN_TRI(V0, U0, U1, U2);
		POINT_IN_TRI(U0, V0, V1, V2);

		return 0;
	}
	void EDGE_AGAINST_TRI_EDGES(Vector3 V0, Vector3 V1, Vector3 U0, Vector3 U1, Vector3 U2)
	{
		float Ax, Ay, Bx, By, Cx, Cy, e, d, f;
		Ax = V1[i0] - V0[i0];
		Ay = V1[i1] - V0[i1];
		EDGE_EDGE_TEST(V0, U0, U1);
		EDGE_EDGE_TEST(V0, U1, U2);
		EDGE_EDGE_TEST(V0, U2, U0);
	}
	void EDGE_EDGE_TEST(Vector3 V0, Vector3 U0, Vector3 U1)
	{
		Bx = U0[i0] - U1[i0];
		By = U0[i1] - U1[i1];
		Cx = V0[i0] - U0[i0];
		Cy = V0[i1] - U0[i1];
		f = Ay * Bx - Ax * By;
		d = By * Cx - Bx * Cy;
		if ((f > 0 && d >= 0 && d <= f) || (f < 0 && d <= 0 && d >= f))
		{
			e = Ax * Cy - Ay * Cx;
			if (f > 0)
			{
				if (e >= 0 && e <= f) return 1;
			}

			else
			{
				if (e <= 0 && e >= f) return 1;
			}
		}
	}
}
*/