using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class DebugExtraDrawer : Singleton<DebugExtraDrawer> {

	// weird stuff https://discussions.unity.com/t/onpostrender-is-not-called/213533/2
	void OnEnable() {
		RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
	}

	void OnDisable() {
		RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
	}

	private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera camera) {
		OnPostRender();
	}


	struct Line3D {
		public Vector3 ps, pe;
		public Color cs, ce;
		public bool depth;
		public float expire;
	}

	struct Line2D {
		public Vector2 ps, pe;
		public Color cs, ce;
		public bool depth;
		public float expire;
	}

	List<Line3D> q3d = new();
	List<Line2D> q2d = new();

	static Material depthOn;
	static Material depthOff;

	void EnsureMats() {
		if (depthOn) return;
		var s = Shader.Find("Hidden/Internal-Colored");

		depthOn = new Material(s);
		depthOn.hideFlags = HideFlags.HideAndDontSave;
		depthOn.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		depthOn.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		depthOn.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		depthOn.SetInt("_ZWrite", 0);
		depthOn.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);

		depthOff = new Material(s);
		depthOff.hideFlags = HideFlags.HideAndDontSave;
		depthOff.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		depthOff.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		depthOff.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		depthOff.SetInt("_ZWrite", 0);
		depthOff.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
	}

	public void DrawLine3D(Vector3 start, Vector3 end, Color cs, Color ce, float duration, bool depthTest) {
		q3d.Add(new Line3D {
			ps = start,
			pe = end,
			cs = cs,
			ce = ce,
			depth = depthTest,
			expire = Time.time + duration
		});
	}

	public void DrawLine2D(Vector2 start, Vector2 end, Color cs, Color ce, float duration, bool depthTest) {
		q2d.Add(new Line2D {
			ps = start,
			pe = end,
			cs = cs,
			ce = ce,
			depth = depthTest,
			expire = Time.time + duration
		});
	}

	void CullExpired() {
		float t = Time.time;
		q3d.RemoveAll(l => l.expire < t);
		q2d.RemoveAll(l => l.expire < t);
	}

	void Draw3DBatch(Material m, bool depthFlag) {
		//Debug.Log($"Drawing 3D lines with depthFlag={depthFlag}, using material {m.name}, total lines={q3d.Count}");
		m.SetPass(0);
		GL.Begin(GL.LINES);
		foreach (var l in q3d) {
			if (l.depth != depthFlag) continue;
			//Debug.Log($"3D Line from {l.ps} to {l.pe}, color start={l.cs}, color end={l.ce}, depth={l.depth}");
			GL.Color(l.cs); GL.Vertex(l.ps);
			GL.Color(l.ce); GL.Vertex(l.pe);
		}
		GL.End();
	}

	void Draw2DBatch(Material m, bool depthFlag) {
		//Debug.Log($"Drawing 2D lines with depthFlag={depthFlag}, using material {m.name}, total lines={q2d.Count}");
		m.SetPass(0);
		GL.Begin(GL.LINES);
		foreach (var l in q2d) {
			if (l.depth != depthFlag) continue;
			//Debug.Log($"2D Line from {l.ps} to {l.pe}, color start={l.cs}, color end={l.ce}, depth={l.depth}");
			GL.Color(l.cs); GL.Vertex(new Vector3(l.ps.x, l.ps.y, 0));
			GL.Color(l.ce); GL.Vertex(new Vector3(l.pe.x, l.pe.y, 0));
		}
		GL.End();
	}

	private void OnPostRender() {
		EnsureMats();
		var cam = GetComponent<Camera>();

		GL.PushMatrix();

		GL.LoadProjectionMatrix(cam.projectionMatrix);
		GL.modelview = cam.worldToCameraMatrix;
		Draw3DBatch(depthOn, true);
		Draw3DBatch(depthOff, false);

		GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, Screen.width, 0, Screen.height, -1, 1));
		GL.modelview = Matrix4x4.identity;
		Draw2DBatch(depthOn, true);
		Draw2DBatch(depthOff, false);

		GL.PopMatrix();

		CullExpired();
	}
}