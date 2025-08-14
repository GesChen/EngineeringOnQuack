using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Geometry {

	/// <summary>
	/// Triangle struct, assumed wound CW
	/// </summary>
	/// <remarks>
	/// <para>Contains several utility methods:</para>
	/// <para>FromVertexArray: returns a triangle list from several formats</para>
	/// </remarks>
	public struct Triangle { 
		public Vector3 p1;
		public Vector3 p2;
		public Vector3 p3;

		public Triangle(Vector3 a, Vector3 b, Vector3 c) {
			p1 = a;
			p2 = b;
			p3 = c;
		}

		public static Triangle[] FromMesh(Mesh mesh) =>
			FromVertexArray(mesh.vertices, mesh.triangles);

		public static Triangle[] FromVertexArray(Vector3[] verts, int[] tris) {
			Vector3[] triPoses = tris.Select(i => verts[i]).ToArray();
			return FromVertexArray(triPoses);
		}

		public static Triangle[] FromVertexArray(Vector3[] triPoses) {
			// every 3 is one triangle, adding a bit of error catching just in case

			if (triPoses.Length % 3 != 0)
				throw new("Invalid length for triangle positions array, should be divisible by 3");

			Triangle[] tris = new Triangle[triPoses.Length / 3];
			for (int i = 0; i < triPoses.Length; i += 3) {
				int index = i / 3;
				tris[index] = new Triangle(
					triPoses[i],
					triPoses[i + 1],
					triPoses[i + 2]
					);
			}

			return tris;
		}

		public static explicit operator Triangle2D(Triangle triangle)
			=> new( // using the performance ctor because reasons
				triangle.p1.x, triangle.p1.y, 
				triangle.p2.x, triangle.p2.y, 
				triangle.p3.x, triangle.p3.y);

		public readonly AABB Bounds => new(p1, p2, p3);

	}

	/// <summary>
	/// 2D triangle struct with helper methods
	/// </summary>
	public struct Triangle2D {
		public Vector2 p1;
		public Vector2 p2;
		public Vector2 p3;

		public Triangle2D(Vector2 a, Vector2 b, Vector2 c) {
			p1 = a;
			p2 = b;
			p3 = c;
		}

		// this constructor is PURELY for performance reasons.
		// because fsr the v3->v2 implicit conversion and subsequent v2 ctor
		// is SLOW AS FUCK
		public Triangle2D(
			float aX, float aY,
			float bX, float bY,
			float cX, float cY) {
			p1.x = aX; p1.y = aY;
			p2.x = bX; p2.y = bY;
			p3.x = cX; p3.y = cY;
		}
		public static implicit operator Triangle(Triangle2D triangle)
			=> new(triangle.p1, triangle.p2, triangle.p3);

		// ctor may be slower than manually? idk but this is worth a shot.
		//public readonly AABB2D Bounds => new(p1, p2, p3);
		public readonly AABB2D Bounds {
			get {
				// manuals might be faster than the bounds ctor, this is only 8 branches? idk
				// 20 comparisons? good to test idk tho. 

				// manual minimum but compiler mighta optimized this already
				float minX = 
					(p1.x < p2.x && p1.x < p3.x) // p1x smallest?
					? p1.x
					: (p2.x < p3.x) // p2x smallest? (p1x is not smallest, no check needed)
						? p2.x
						: p3.x;
				float minY = 
					(p1.y < p2.y && p1.y < p3.y) // p1y smallest?
					? p1.y
					: (p2.y < p3.y) // p2y smallest? (p1y is not smallest, no check needed)
						? p2.y
						: p3.y;

				// manual maximum but compiler mighta optimized this already
				float maxX =
					(p1.x > p2.x && p1.x > p3.x) // p1x biggest?
					? p1.x
					: (p2.x > p3.x) // p2x biggest? (p1x is not biggest, no check needed)
						? p2.x
						: p3.x;
				float maxY = 
					(p1.y > p2.y && p1.y > p3.y) // p1y biggest?
					? p1.y
					: (p2.y > p3.y) // p2y biggest? (p1y is not biggest, no check needed)
						? p2.y
						: p3.y;

				return new AABB2D(minX, minY, maxX, maxY);
			}
		}
	}

	public struct AABB {
		public Vector3 Min;
		public Vector3 Max;

		public AABB(Vector3 min, Vector3 max) {
			Min = min;
			Max = max;
		}

		public AABB(params Vector3[] points) {
			Vector3 min = Vector3.positiveInfinity;
			Vector3 max = Vector3.negativeInfinity;

			foreach (var point in points) {
				min = Vector3.Min(min, point);
				max = Vector3.Max(max, point);
			}

			Min = min;
			Max = max;
		}

		public readonly bool Test(Vector3 point) =>
			point.x > Min.x &&
			point.y > Min.y &&
			point.z > Min.z &&
			point.x < Max.x &&
			point.y < Max.y &&
			point.z < Max.z;
	}

	public struct AABB2D {
		public Vector2 Min;
		public Vector2 Max;

		public AABB2D(Vector2 min, Vector2 max) {
			Min = min;
			Max = max;
		}

		// another performance ctor to prevent need to ctor 2 v2s in this ctor
		public AABB2D(
			float minX, float minY,
			float maxX, float maxY) {
			Min.x = minX;
			Min.y = minY;
			Max.x = maxX;
			Max.y = maxY;
		}

		public AABB2D(params Vector2[] points) {
			Vector2 min = Vector2.positiveInfinity;
			Vector2 max = Vector2.negativeInfinity;

			foreach (var point in points) {
				min = Vector2.Min(min, point);
				max = Vector2.Max(max, point);
			}

			Min = min;
			Max = max;
		}

		public readonly bool Test(Vector2 point) =>
			point.x > Min.x &&
			point.y > Min.y &&
			point.x < Max.x &&
			point.y < Max.y;
	}
}