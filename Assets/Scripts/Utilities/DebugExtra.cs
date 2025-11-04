using System;
using System.Collections.Generic;
using UnityEngine;
using Geometry;

public class DebugExtra {
	public static void DrawEmpty(Vector3 pos, float size, Color? color = null) {
		Color col = color ?? Color.white;

		Debug.DrawLine(pos - size * Vector3.up, pos + size * Vector3.up, col);
		Debug.DrawLine(pos - size * Vector3.right, pos + size * Vector3.right, col);
		Debug.DrawLine(pos - size * Vector3.forward, pos + size * Vector3.forward, col);
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

		for (int i = 0; i < density + 1; i++) {
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

	static readonly Dictionary<char, Vector3> GridPoints = new(){
		{ 'r', new(	.0f, 01f) },
		{ 't', new(	.5f, 01f) },
		{ 'y', new(	01f, 01f) },
		{ 'f', new(	.0f, .5f) },
		{ 'g', new(	.5f, .5f) },
		{ 'h', new(	01f, .5f) },
		{ 'v', new(	.0f, .0f) },
		{ 'b', new(	.5f, .0f) },
		{ 'n', new(	01f, .0f) }
	};

	static readonly Dictionary<int, string> CharShapes = new(){
		{ 0, "rynvrvnyvyr" },
		{ 1, "rtbvnbt" },
		{ 2, "ryhfvnvfhy" },
		{ 3, "ryhghnvny" },
		{ 4, "rfhynhf" },
		{ 5, "yrfhnvnhfr" },
		{ 6, "yrvnhfhnvr" },
		{ 7, "ryvy" },
		{ 8, "rynyrvnvfhf" },
		{ 9, "rynyrfhf" },
		{ 101, "rynvfvnhfhy" },
		{ 102, "rvnhfhnv" },
		{ 103, "ytfbnbft" },
		{ 104, "ynvfhfvn" },
		{ 105, "yrvnvfhyhfr" },
		{ 106, "ytbtgfhgt" },
		{ 107, "rtgnvngfgtrf" },
		{ 108, "rvfhnhf" },
		{ 109, "bg" },
		{ 110, "ynvn" },
		{ 111, "rvfnfhf" },
		{ 112, "tbnb" },
		{ 113, "vrynytgbtr" },
		{ 114, "vrhnhr" },
		{ 115, "fthbfbht" },
		{ 116, "thfvfhtf" },
		{ 117, "fthfhnht" },
		{ 118, "yrvr" },
		{ 119, "htfnvnft" },
		{ 120, "tbnbtgfhg" },
		{ 121, "rfnynnf" },
		{ 122, "rfbhyhbf" },
		{ 123, "rvbgbnynv" },
		{ 124, "rfgvgnghyhf" },
		{ 125, "rfhnvnyhf" },
		{ 126, "ryhvnvhy" },
		{ 201, "vrynyrfhf" },
		{ 202, "rthtrfhfvngnv" },
		{ 203, "yrvnvr" },
		{ 204, "rthbvbhtrv" },
		{ 205, "yrfgfvnvfgfr" },
		{ 206, "yrfgfvr" },
		{ 207, "yrvnhghnvr" },
		{ 208, "rvfhnyhf" },
		{ 209, "rytbvnbt" },
		{ 210, "rytbvbt" },
		{ 211, "rfyfnfv" },
		{ 212, "rvnv" },
		{ 213, "vrgynygr" },
		{ 214, "vrnynr" },
		{ 215, "rynvrvny" },
		{ 216, "ryhfvfhyrf" },
		{ 217, "yrvnyngnvr" },
		{ 218, "yhgngfrvfhyr" },
		{ 219, "yrfgnvngfr" },
		{ 220, "rytbt" },
		{ 221, "rvnynv" },
		{ 222, "rbyb" },
		{ 223, "rvgnyngv" },
		{ 224, "rngvyg" },
		{ 225, "rgbgyg" },
		{ 226, "ryvnvy" },
		{ 301, "tbvnbt" },
		{ 302, "ryngbgfvbvrfgnbny" },
		{ 303, "fhgtbghynhnvnh" },
		{ 304, "ytbtrfhnvnhfr" },
		{ 305, "vgfrfghnhgy" },
		{ 306, "ftht" },
		{ 307, "hbvfgyrnrygfvbh" },
		{ 308, "vgfgrgtgygngbghg" },
		{ 309, "tfbf" },
		{ 310, "thbh" },
		{ 311, "fh" },
		{ 312, "rynvny" },
		{ 313, "vn" },
		{ 314, "fhgtbg" },
		{ 315, "ytbnbt" },
		{ 316, "rtbvbt" },
		{ 317, "rn" },
		{ 318, "ytgfgbnbt" },
		{ 319, "rtghgbvbt" },
		{ 320, "tb" },
		{ 321, "thgbvbgh" },
		{ 322, "tg" },
		{ 323, "thgnbngh" },
		{ 324, "gtyhyt" },
		{ 325, "gv" },
		{ 326, "vb" },
		{ 327, "vy" },
		{ 328, "yfnf" },
		{ 329, "rhvh" },
		{ 330, "fryhgbghyrf" },
		{ 331, "th" },
		{ 332, "frtghyhgtr" }
	};

	static readonly Dictionary<char, int> CharIntPairing = new(){
		{ '0',	0	},
		{ '1',	1	},
		{ '2',	2	},
		{ '3',	3	},
		{ '4',	4	},
		{ '5',	5	},
		{ '6',	6	},
		{ '7',	7	},
		{ '8',	8	},
		{ '9',	9	},
		{ 'a',	101	},
		{ 'b',	102	},
		{ 'c',	103	},
		{ 'd',	104	},
		{ 'e',	105	},
		{ 'f',	106	},
		{ 'g',	107	},
		{ 'h',	108	},
		{ 'i',	109	},
		{ 'j',	110	},
		{ 'k',	111	},
		{ 'l',	112	},
		{ 'm',	113	},
		{ 'n',	114	},
		{ 'o',	115	},
		{ 'p',	116	},
		{ 'q',	117	},
		{ 'r',	118	},
		{ 's',	119	},
		{ 't',	120	},
		{ 'u',	121	},
		{ 'v',	122	},
		{ 'w',	123	},
		{ 'x',	124	},
		{ 'y',	125	},
		{ 'z',	126	},
		{ 'A',	201	},
		{ 'B',	202	},
		{ 'C',	203	},
		{ 'D',	204	},
		{ 'E',	205	},
		{ 'F',	206	},
		{ 'G',	207	},
		{ 'H',	208	},
		{ 'I',	209	},
		{ 'J',	210	},
		{ 'K',	211	},
		{ 'L',	212	},
		{ 'M',	213	},
		{ 'N',	214	},
		{ 'O',	215	},
		{ 'P',	216	},
		{ 'Q',	217	},
		{ 'R',	218	},
		{ 'S',	219	},
		{ 'T',	220	},
		{ 'U',	221	},
		{ 'V',	222	},
		{ 'W',	223	},
		{ 'X',	224	},
		{ 'Y',	225	},
		{ 'Z',	226	},
		{ '!',	301	},
		{ '@',	302	},
		{ '#',	303	},
		{ '$',	304	},
		{ '%',	305	},
		{ '^',	306	},
		{ '&',	307	},
		{ '*',	308	},
		{ '(',	309	},
		{ ')',	310	},
		{ '-',	311	},
		{ '=',	312	},
		{ '_',	313	},
		{ '+',	314	},
		{ '[',	315	},
		{ ']',	316	},
		{ '\\',	317	},
		{ '{',	318	},
		{ '}',	319	},
		{ '|',	320	},
		{ ';',	321	},
		{ '\'',	322	},
		{ ':',	323	},
		{ '"',	324	},
		{ ',',	325	},
		{ '.',	326	},
		{ '/',	327	},
		{ '<',	328	},
		{ '>',	329	},
		{ '?',	330	},
	};

	public static void DrawText(string text, Vector3 position, float scale, Color color, float duration = 0) {
		if (string.IsNullOrEmpty(text)) return;

		float charWidth = .6f * scale;
		float charHeight = 1f * scale;
		float spacing = .2f * scale;

		Vector3 cursor = position;
		foreach (char ch in text) {
			if (!CharIntPairing.TryGetValue(ch, out int shapeId)) {
				cursor.x += charWidth + spacing;
				continue;
			}

			if (!CharShapes.TryGetValue(shapeId, out string shape)) {
				cursor.x += charWidth + spacing;
				continue;
			}

			Vector3 last = Vector3.zero;
			for (int i = 0; i < shape.Length; i++) {
				var p = GridPoints[shape[i]];
				p.x *= charWidth;
				p.y *= charHeight;
				
				if (i == 0) last = p;

				Debug.DrawLine(cursor + p, cursor + last, color, duration);
				last = p;
			}

			cursor.x += charWidth + spacing;
		}
	}

}