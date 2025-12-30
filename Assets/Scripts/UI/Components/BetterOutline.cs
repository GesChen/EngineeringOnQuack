using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class BetterOutline : MaskableGraphic {
	public float InnerWidth;
	public float OuterWidth = 10f;

	protected override void OnRectTransformDimensionsChange() {
		base.OnRectTransformDimensionsChange();
		SetVerticesDirty();
		SetMaterialDirty();
	}

	// force no raycast target
	public override bool raycastTarget { get => false; set { } }

	protected override void OnPopulateMesh(VertexHelper vh) {
		vh.Clear();

		Rect r = rectTransform.rect;

		// inner rect inset
		float il = r.xMin - InnerWidth;
		float ir = r.xMax + InnerWidth;
		float ib = r.yMin - InnerWidth;
		float it = r.yMax + InnerWidth;

		// outer rect expanded
		float ol = r.xMin - OuterWidth;
		float orr = r.xMax + OuterWidth;
		float ob = r.yMin - OuterWidth;
		float ot = r.yMax + OuterWidth;

		UIVertex v = UIVertex.simpleVert;
		v.color = color;

		// outer
		Add(vh, ref v, ol, ob, 0, 0);
		Add(vh, ref v, ol, ot, 0, 1);
		Add(vh, ref v, orr, ot, 1, 1);
		Add(vh, ref v, orr, ob, 1, 0);

		// inner
		Add(vh, ref v, il, ib, 0, 0);
		Add(vh, ref v, il, it, 0, 1);
		Add(vh, ref v, ir, it, 1, 1);
		Add(vh, ref v, ir, ib, 1, 0);

		// left
		vh.AddTriangle(0, 1, 5);
		vh.AddTriangle(0, 5, 4);

		// top
		vh.AddTriangle(1, 2, 6);
		vh.AddTriangle(1, 6, 5);

		// right
		vh.AddTriangle(2, 3, 7);
		vh.AddTriangle(2, 7, 6);

		// bottom
		vh.AddTriangle(3, 0, 4);
		vh.AddTriangle(3, 4, 7);
	}

	static void Add(VertexHelper vh, ref UIVertex v, float x, float y, float u, float v0) {
		v.position = new Vector3(x, y);
		v.uv0 = new Vector2(u, v0);
		vh.AddVert(v);
	}
}