using JetBrains.Annotations;
using UnityEngine;

public class DebugExtra
{
	public static void DrawEmpty(Vector3 pos, float size, Color color)
	{
		Debug.DrawLine(pos - size * Vector3.up, pos + size * Vector3.up, color);
		Debug.DrawLine(pos - size * Vector3.right, pos + size * Vector3.right, color);
		Debug.DrawLine(pos - size * Vector3.forward, pos + size * Vector3.forward, color);
	}
	public static void DrawEmpty(Vector3 pos, float size)
	{
		DrawEmpty(pos, size, Color.white);
	}

	public static void DrawSphere(Vector3 pos, float radius, int resolution, Color color)
	{
		float sin0 = Mathf.Sin(0);
		float cos0 = Mathf.Cos(0);
		Vector3 lastX = new Vector3(0, sin0, cos0) * radius + pos;
		Vector3 lastY =	new Vector3(sin0, 0, cos0) * radius + pos;
		Vector3 lastZ = new Vector3(sin0, cos0, 0) * radius + pos;
		for (int i = 0; i < resolution + 1; i++)
		{
			float j = i / (float) resolution * 2 * Mathf.PI;
			float sin = Mathf.Sin(j);
			float cos = Mathf.Cos(j);
			Vector3 xPoint = new Vector3(0, sin, cos) * radius + pos;
			Vector3 yPoint = new Vector3(sin, 0, cos) * radius + pos;
			Vector3 zPoint = new Vector3(sin, cos, 0) * radius + pos;

			Debug.DrawLine(lastX, xPoint, color);
			Debug.DrawLine(lastY, yPoint, color);
			Debug.DrawLine(lastZ, zPoint, color);

			lastX = xPoint;
			lastY = yPoint;
			lastZ = zPoint;
		}
	}
	public static void DrawSphere(Vector3 pos, float radius, int resolution)
	{
		DrawSphere(pos, radius, resolution, Color.white);
	}
	public static void DrawSphere(Vector3 pos, float radius, Color color)
	{
		DrawSphere(pos, radius, 40, color);
	}
	public static void DrawSphere(Vector3 pos, float radius)
	{
		DrawSphere(pos, radius, 40, Color.white);
	}

	public static void DrawPoint(Vector3 pos, float size, Color color)
	{
		Vector3 px = pos + size * Vector3.right;
		Vector3 nx = pos + size * Vector3.left;
		Vector3 py = pos + size * Vector3.up;
		Vector3 ny = pos + size * Vector3.down;
		Vector3 pz = pos + size * Vector3.forward;
		Vector3 nz = pos + size * Vector3.back;

		Debug.DrawLine(px, py, color);
		Debug.DrawLine(px, ny, color);
		Debug.DrawLine(px, pz, color);
		Debug.DrawLine(px, nz, color);

		Debug.DrawLine(nx, py, color);
		Debug.DrawLine(nx, ny, color);
		Debug.DrawLine(nx, pz, color);
		Debug.DrawLine(nx, nz, color);
		
		//Debug.DrawLine(px, nx, color);
		//Debug.DrawLine(py, ny, color);
		//Debug.DrawLine(pz, nz, color);

		Debug.DrawLine(py, pz, color);
		Debug.DrawLine(py, nz, color);
		Debug.DrawLine(ny, pz, color);
		Debug.DrawLine(ny, nz, color);
	}
	public static void DrawPoint(Vector3 pos, float size)
	{
		DrawPoint(pos, size, Color.white);
	}
	public static void DrawPoint(Vector3 pos, Color color)
	{
		DrawPoint(pos, .1f, color);
	}
	public static void DrawPoint(Vector3 pos)
	{
		DrawPoint(pos, .1f);
	}
	public static void DrawPoint(Vector3 pos, Color color, float size)
	{
		DrawPoint(pos, size, color);
	}


	public static void DrawGrid(Vector3 pos, Vector3 normal, int gridSize, int cellSize)
	{
		// Calculate the right and forward vectors based on the normal
		Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
		Vector3 forward = Vector3.Cross(normal, right).normalized;

		// Calculate the size of the grid
		float gridSizeX = gridSize * cellSize;
		float gridSizeY = gridSize * cellSize;

		// Draw horizontal lines
		for (int i = 0; i <= gridSize; i++)
		{
			Vector3 start = pos + i * cellSize * forward - 0.5f * gridSizeX * forward;
			Vector3 end = start + gridSizeX * right;
			Debug.DrawLine(start, end, Color.white);
		}

		// Draw vertical lines
		for (int i = 0; i <= gridSize; i++)
		{
			Vector3 start = pos + i * cellSize * right - 0.5f * gridSizeY * right;
			Vector3 end = start + gridSizeY * forward;
			Debug.DrawLine(start, end, Color.white);
		}
	}
	public static void DrawPlane(Vector3 pos, Vector3 normal, float size, int resolution, Color color)
	{
		Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
		Vector3 up = Vector3.Cross(normal, right).normalized;

		for (int i = 0; i < resolution; i++)
		{
			float d = (i - (resolution - 1) / 2f) / resolution * 2f * size;
			Debug.DrawLine(pos + right * d - up * size, pos + right * d + up * size, color);
			Debug.DrawLine(pos + up * d - right * size, pos + up * d + right * size, color);
		}
	}

	public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color, float duration)
	{
		Debug.DrawLine(a, b, color, duration);
		Debug.DrawLine(b, c, color, duration);
		Debug.DrawLine(c, a, color, duration);
	}
	public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c)
	{
		DrawTriangle(a, b, c, Color.white, 0);
	}
	public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, float duration)
	{
		DrawTriangle(a, b, c, Color.white, duration);
	}
	public static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
	{
		DrawTriangle(a, b, c, color, 0);
	}
	
	public static void DrawTriangleFilled(Vector3 a, Vector3 b, Vector3 c, int density, Color color, float duration)
	{
		for (int i = 0; i < density; i++)
		{
			float t = (float)i / density;
			Debug.DrawLine(a, Vector3.Lerp(b, c, t), color, duration);
			Debug.DrawLine(b, Vector3.Lerp(a, c, t), color, duration);
			Debug.DrawLine(c, Vector3.Lerp(a, b, t), color, duration);
		}
	}
	public static void DrawTriangleFilled(Vector3 a, Vector3 b, Vector3 c, int density = 10)
	{
		DrawTriangleFilled(a, b, c, density, Color.white, 0);
	}
	public static void DrawTriangleFilled(Vector3 a, Vector3 b, Vector3 c, Color color)
	{
		DrawTriangleFilled(a, b, c, 10, color, 0);
	}
	public static void DrawTriangleFilled(Vector3 a, Vector3 b, Vector3 c, Color color, float duration)
	{
		DrawTriangleFilled(a, b, c, 10, color, duration);
	}

	public static void DrawCube(Vector3 a, Vector3 b, Color color)
	{
		Vector3 A = new(a.x, a.y, a.z);
		Vector3 B = new(a.x, a.y, b.z);
		Vector3 C = new(b.x, a.y, a.z);
		Vector3 D = new(b.x, a.y, b.z);
		Vector3 E = new(a.x, b.y, a.z);
		Vector3 F = new(a.x, b.y, b.z);
		Vector3 G = new(b.x, b.y, a.z);
		Vector3 H = new(b.x, b.y, b.z);

		Debug.DrawLine(A, B, color);
		Debug.DrawLine(A, C, color);
		Debug.DrawLine(A, E, color);
		Debug.DrawLine(D, B, color);
		Debug.DrawLine(D, C, color);
		Debug.DrawLine(D, H, color);
		Debug.DrawLine(G, H, color);
		Debug.DrawLine(G, E, color);
		Debug.DrawLine(G, C, color);
		Debug.DrawLine(F, H, color);
		Debug.DrawLine(F, E, color);
		Debug.DrawLine(F, B, color);
	}
	public static void DrawCube(Vector3 a, Vector3 b)
	{
		DrawCube(a, b, Color.white);
	}

	public static void DrawCone(Vector3 p, Vector3 d, float radius, float height, Color color, int resolution)
	{
		Vector3 tip = p + d * height;
		Debug.DrawLine(p, tip, color);
		Quaternion r = Quaternion.LookRotation(d);

		Vector3 lastPoint = r * Vector3.forward * radius;
		for (int i = 0; i < resolution; i++)
		{
			float t = (i + 1) / resolution * 2f * Mathf.PI;
			Vector3 point = r * new Vector3(Mathf.Sin(t), 0, Mathf.Cos(t)) * radius;
			Debug.DrawLine(point, lastPoint, color);
			Debug.DrawLine(point, tip, color);
		}
	}

	public static void DrawArrow(Vector3 pos, Vector3 dir, float length, float tipLength, Color color)
	{
		dir.Normalize();
		Vector3 tip = pos + dir * length;
		Debug.DrawLine(pos, tip, color);

		Quaternion r = Quaternion.LookRotation(dir);
		Debug.DrawLine(tip, tip + (r * new Vector3(0,  .4472135955f, -.894427191f) * tipLength), color);
		Debug.DrawLine(tip, tip + (r * new Vector3(0, -.4472135955f, -.894427191f) * tipLength), color);
		Debug.DrawLine(tip, tip + (r * new Vector3( .4472135955f, 0, -.894427191f) * tipLength), color);
		Debug.DrawLine(tip, tip + (r * new Vector3(-.4472135955f, 0, -.894427191f) * tipLength), color);
	}
	public static void DrawArrow(Vector3 pos, Vector3 dir, float length, Color color)
	{
		DrawArrow(pos, dir, length, .1f, color);
	}
	public static void DrawArrow(Vector3 pos, Vector3 dir, float length)
	{
		DrawArrow(pos, dir, length, Color.white);
	}
	public static void DrawArrow(Vector3 pos, Vector3 dir, Color color)
	{
		DrawArrow(pos, dir, 1, color);
	}
	public static void DrawArrow(Vector3 pos, Vector3 dir)
	{
		DrawArrow(pos, dir, Color.white);
	}


}
