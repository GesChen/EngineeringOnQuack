using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Geometry;

public class geometrytesting : MonoBehaviour {
	public Transform gridstart;
	public Transform gridend;
	public int div;
	public float pointsize;

	public Transform meshobj;

	Triangle[] tris;
	Vector3[] points;
	bool[] results;

	private void Start() {
		tris = Triangle.FromMesh(meshobj.GetComponent<MeshFilter>().mesh);
	}

	void UpdatePoints() {
		points = new Vector3[div * div * div];
		results = new bool[div * div * div];

		Vector3 start = gridstart.position;
		Vector3 end = gridend.position;
		int i = 0;
		for (int x = 0; x < div; x++) {
			for (int y = 0; y < div; y++) {
				for (int z = 0; z < div; z++) {
					points[i] = new(
						Mathf.Lerp(start.x, end.x, x / (float)div),
						Mathf.Lerp(start.y, end.y, y / (float)div),
						Mathf.Lerp(start.z, end.z, z / (float)div)
						);
					i++;
				}
			}
		}
	}

	void Update() {
		UpdatePoints();
		HF.Test(test);
		Render();
	}

	void test() {
		for (int i = 0; i < points.Length; i++) {
			bool check = Intersections.PointInMesh(points[i], tris, meshobj);
			results[i] = check;
		}
	}

	void Render() {
		for (int i = 0; i < points.Length; i++) {
			Color color = 
				results[i] 
				? Color.green 
				: Color.red;

			DebugExtra.DrawPoint(points[i], color, pointsize);
		}
	}
}