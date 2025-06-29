using System;
using System.Collections.Generic;
using UnityEngine;
using Geometry;

public class DebugExtra {
	public static void DrawEmpty(Vector3 pos, float size, Color? color = null) {
		Color col = color ?? Color.white;

		Debug.DrawLine(pos - size * Vector3.up,			pos + size * Vector3.up,		col);
		Debug.DrawLine(pos - size * Vector3.right,		pos + size * Vector3.right,		col);
		Debug.DrawLine(pos - size * Vector3.forward,	pos + size * Vector3.forward,	col);
	}

	public static void DrawSphere(Vector3 pos, float radius, int resolution = 40, Color? color = null) {
		Color col = color ?? Color.white;
		float sin0 = Mathf.Sin(0);
		float cos0 = Mathf.Cos(0);
		
		Vector3 lastX = new Vector3(0, sin0, cos0) * radius + pos;
		Vector3 lastY = new Vector3(sin0, 0, cos0) * radius + pos;
		Vector3 lastZ = new Vector3(sin0, cos0, 0) * radius + pos;
		
		for (int i = 0; i < resolution + 1; i++) {
			float j = i / (float) resolution * 2 * Mathf.PI;
			float sin = Mathf.Sin(j);
			float cos = Mathf.Cos(j);
			Vector3 xPoint = new Vector3(0, sin, cos) * radius + pos;
			Vector3 yPoint = new Vector3(sin, 0, cos) * radius + pos;
			Vector3 zPoint = new Vector3(sin, cos, 0) * radius + pos;

			Debug.DrawLine(lastX, xPoint, col);
			Debug.DrawLine(lastY, yPoint, col);
			Debug.DrawLine(lastZ, zPoint, col);

			lastX = xPoint;
			lastY = yPoint;
			lastZ = zPoint;
		}
	}

	public static void DrawPoint(Vector3 pos, float size = .1f, Color? color = null) {
		Vector3 px = pos + size * Vector3.right;
		Vector3 nx = pos + size * Vector3.left;
		Vector3 py = pos + size * Vector3.up;
		Vector3 ny = pos + size * Vector3.down;
		Vector3 pz = pos + size * Vector3.forward;
		Vector3 nz = pos + size * Vector3.back;

		Color col = color ?? Color.white;

		Debug.DrawLine(px, py, col);
		Debug.DrawLine(px, ny, col);
		Debug.DrawLine(px, pz, col);
		Debug.DrawLine(px, nz, col);

		Debug.DrawLine(nx, py, col);
		Debug.DrawLine(nx, ny, col);
		Debug.DrawLine(nx, pz, col);
		Debug.DrawLine(nx, nz, col);

		//Debug.DrawLine(px, nx, color);
		//Debug.DrawLine(py, ny, color);
		//Debug.DrawLine(pz, nz, color);

		Debug.DrawLine(py, pz, col);
		Debug.DrawLine(py, nz, col);
		Debug.DrawLine(ny, pz, col);
		Debug.DrawLine(ny, nz, col);
	}

	public static void DrawGrid(Vector3 pos, Vector3 normal, int gridSize, int cellSize) {
		// Calculate the right and forward vectors based on the normal
		Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
		Vector3 forward = Vector3.Cross(normal, right).normalized;

		// Calculate the size of the grid
		float gridSizeX = gridSize * cellSize;
		float gridSizeY = gridSize * cellSize;

		// Draw horizontal lines
		for (int i = 0; i <= gridSize; i++) {
			Vector3 start = pos + i * cellSize * forward - 0.5f * gridSizeX * forward;
			Vector3 end = start + gridSizeX * right;
			Debug.DrawLine(start, end, Color.white);
		}

		// Draw vertical lines
		for (int i = 0; i <= gridSize; i++) {
			Vector3 start = pos + i * cellSize * right - 0.5f * gridSizeY * right;
			Vector3 end = start + gridSizeY * forward;
			Debug.DrawLine(start, end, Color.white);
		}
	}

	public static void DrawPlane(Vector3 pos, Vector3 normal, float size, int resolution = 10, Color? color = null) {
		Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
		Vector3 up = Vector3.Cross(normal, right).normalized;

		Color col = color ?? Color.white;
		for (int i = 0; i < resolution; i++) {
			float d = (i - (resolution - 1) / 2f) / resolution * 2f * size;
			Debug.DrawLine(pos + right * d - up * size, pos + right * d + up * size, col);
			Debug.DrawLine(pos + up * d - right * size, pos + up * d + right * size, col);
		}
	}

	public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color? color = null) {
		Color col = color ?? Color.white;
		Debug.DrawLine(a, b, col);
		Debug.DrawLine(b, c, col);
		Debug.DrawLine(c, a, col);
	}

	public static void DrawTriangle(Triangle tri, Color? color = null) {
		Color col = color ?? Color.white;
		Debug.DrawLine(tri.p1, tri.p2, col);
		Debug.DrawLine(tri.p2, tri.p3, col);
		Debug.DrawLine(tri.p3, tri.p1, col);
	}

	public static void DrawTriangleFilled(Vector3 a, Vector3 b, Vector3 c, int density = 10, Color? color = null) {
		Color col = color ?? Color.white;

		for (int i = 0; i < density; i++) {
			float t = (float)i / density;
			Debug.DrawLine(a, Vector3.Lerp(b, c, t), col);
			Debug.DrawLine(b, Vector3.Lerp(a, c, t), col);
			Debug.DrawLine(c, Vector3.Lerp(a, b, t), col);
		}
	}

	public static void DrawCube(Vector3 a, Vector3 b, Color? color = null) {
		Vector3 A = new(a.x, a.y, a.z);
		Vector3 B = new(a.x, a.y, b.z);
		Vector3 C = new(b.x, a.y, a.z);
		Vector3 D = new(b.x, a.y, b.z);
		Vector3 E = new(a.x, b.y, a.z);
		Vector3 F = new(a.x, b.y, b.z);
		Vector3 G = new(b.x, b.y, a.z);
		Vector3 H = new(b.x, b.y, b.z);

		Color col = color ?? Color.white;

		Debug.DrawLine(A, B, col);
		Debug.DrawLine(A, C, col);
		Debug.DrawLine(A, E, col);
		Debug.DrawLine(D, B, col);
		Debug.DrawLine(D, C, col);
		Debug.DrawLine(D, H, col);
		Debug.DrawLine(G, H, col);
		Debug.DrawLine(G, E, col);
		Debug.DrawLine(G, C, col);
		Debug.DrawLine(F, H, col);
		Debug.DrawLine(F, E, col);
		Debug.DrawLine(F, B, col);
	}

	public static void DrawCone(Vector3 p, Vector3 d, float radius, float height, Color color, int resolution) {
		Vector3 tip = p + d * height;
		Debug.DrawLine(p, tip, color);
		Quaternion r = Quaternion.LookRotation(d);

		Vector3 lastPoint = r * Vector3.forward * radius;
		for (int i = 0; i < resolution; i++) {
			float t = (i + 1) / resolution * 2f * Mathf.PI;
			Vector3 point = r * new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t)) * radius;
			Debug.DrawLine(point, lastPoint, color);
			Debug.DrawLine(point, tip, color);
		}
	}

	public static void DrawArrow(Vector3 pos, Vector3 dir, float length = 1, float tipLength = .1f, Color? color = null) {
		Color col = color ?? Color.white;

		dir.Normalize();
		Vector3 tip = pos + dir * length;
		Debug.DrawLine(pos, tip, col);

		Quaternion r = Quaternion.LookRotation(dir);
		Debug.DrawLine(tip, tip + (r * new Vector3(0, .4472135955f, -.894427191f) * tipLength), col);
		Debug.DrawLine(tip, tip + (r * new Vector3(0, -.4472135955f, -.894427191f) * tipLength), col);
		Debug.DrawLine(tip, tip + (r * new Vector3(.4472135955f, 0, -.894427191f) * tipLength), col);
		Debug.DrawLine(tip, tip + (r * new Vector3(-.4472135955f, 0, -.894427191f) * tipLength), col);
	}
	
	public static void DrawMesh(Vector3[] verts, int[] tris, Color? color = null) {
		var edges = new HashSet<(int a, int b)>();

		for (int i = 0; i < tris.Length; i += 3) {
			int a = tris[i];
			int b = tris[i + 1];
			int c = tris[i + 2];

			// Normalize the edge to avoid duplicates like (1, 2) and (2, 1)
			edges.Add((Math.Min(a, b), Math.Max(a, b)));
			edges.Add((Math.Min(b, c), Math.Max(b, c)));
			edges.Add((Math.Min(c, a), Math.Max(c, a)));
		}

		foreach (var (a, b) in edges) {
			Debug.DrawLine(verts[a], verts[b], color ?? Color.white);
		}
	}

	public static void DrawMesh(Triangle[] tris, Color? color = null) {
		// i cant figure out how to do the edge duplication removal this time
		// so this is just naive :( sorry performance 2x lines ig

		foreach (var tri in tris) {
			Debug.DrawLine(tri.p1, tri.p2, color ?? Color.white);
			Debug.DrawLine(tri.p2, tri.p3, color ?? Color.white);
			Debug.DrawLine(tri.p3, tri.p1, color ?? Color.white);
		}
	}
}