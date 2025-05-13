using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
	#region singleton
	private static SelectionManager _instance;
	public static SelectionManager Instance { get { return _instance; } }
	void Awake() { UpdateSingleton(); }
	private void OnEnable() { UpdateSingleton(); }
	void UpdateSingleton()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(this);
		}
		else
		{
			_instance = this;
		}
	}
	#endregion


	public bool selectionBoxDragging;

	public int testInterval;
	public int minPixelsMovedForRetest;
	[Tooltip("Makes sure tiny boxes dont select a bunch of stuff by accident")]
	public float minBoxSize;

	public List<Transform> selection;
	public Transform selectionContainer;

	public RectTransform UIBox;

	List<Transform> dragStartSelections;
	Vector2 mousePos;
	Vector2 dragStart;
	bool dragging;
	bool lastMouseDown;
	Vector2 mouseDownStartPos;
	float mouseDownStartTime;
	bool selectionChanged;

	void Update()
	{
		HandleInput();
		HandleContainer();
		UpdateContextManager();
	}

	void HandleInput()
	{
		bool mouseDown = Conatrols.IM.Selection.Drag.IsPressed();
		mousePos = Conatrols.IM.Selection.MousePos.ReadValue<Vector2>();

		// detect mouse down
		if (mouseDown && mouseDown != lastMouseDown)
		{
			dragStart = mousePos;
			dragStartSelections = selection;
			mouseDownStartTime = Time.time;
			mouseDownStartPos = mousePos;
		}

		// detect drag start

		// detect mouse up
		if (!mouseDown && mouseDown != lastMouseDown && !BuildingManager.Instance.TransformTools.hovering)
		{
			if (Time.time - mouseDownStartTime < Conatrols.clickMaxTime &&
				Vector2.Distance(mousePos, mouseDownStartPos) < Conatrols.clickMaxDist)
			{ // counts as a click
				ClickCheck();
			}
			else
				FindObjectsInsideBounds(dragStart, mousePos);
		}

		dragging = mouseDown && !(BuildingManager.Instance.TransformTools.dragging || BuildingManager.Instance.TransformTools.hovering);
		selectionBoxDragging = dragging;
		UIBox.gameObject.SetActive(dragging);

		if (dragging)
		{
			HandleBox();
		}

		lastMouseDown = mouseDown;
	}

	void HandleContainer()
	{
		if (selectionChanged)
		{
			UpdateContainer();
			selectionChanged = false;
		}
	}
	
	void HandleBox()
	{
		Vector2 size = dragStart - mousePos;
		UIBox.position = (dragStart + mousePos) / 2;
		UIBox.sizeDelta = HF.Vector2Abs(size);
	}

	void FindObjectsInsideBounds(Vector2 boundsStart, Vector2 boundsEnd)
	{
		// check size
		if ((boundsStart - boundsEnd).sqrMagnitude < minBoxSize) return;

		// handle multiselection
		if (Conatrols.IM.Selection.Multiselect.IsPressed())
			selection = dragStartSelections;
		else
			selection = new();

		Camera maincamera = Camera.main;
		foreach (Part part in BuildingManager.Instance.Parts)
		{
			if (part == null) continue;

			if (PartIntersectsWithSelectionBox(part, boundsStart, boundsEnd, maincamera) && 
				!selection.Contains(part.transform))
			{
				selection.Add(part.transform);
			}
		}

		selectionChanged = true;
	}

	bool PartIntersectsWithSelectionBox(Part part, Vector2 corner1, Vector2 corner2, Camera maincamera)
	{
		// following code is super fuckin slow if future person can optimize please do idfk what to do it dropst  olike 7 fps
		if (!PartWorldBoundsRectangleIntersect(part, corner1, corner2, maincamera))
			return false;

		Vector3[] tris = (Vector3[])part.basePart.allTriPositions.Clone();

		part.transform.TransformPoints(tris);

		for (int i = 0; i < tris.Length; i += 3)
		{
			Vector3 v1 = tris[i]; //pos + rot * Vector3.Scale(scale, tris[i]);
			Vector3 v2 = tris[i + 1]; //pos + rot * Vector3.Scale(scale, tris[i + 1]);
			Vector3 v3 = tris[i + 2]; //pos + rot * Vector3.Scale(scale, tris[i + 2]);

			Vector2 ss1 = maincamera.WorldToScreenPoint(v1);
			Vector2 ss2 = maincamera.WorldToScreenPoint(v2);
			Vector2 ss3 = maincamera.WorldToScreenPoint(v3);

			bool intersect = RectangleTriangleIntersect(corner1, corner2, ss1, ss2, ss3);

			if (intersect)
				return true;
		}
		return false; /* using the old code again since this is only called once 
		// for now just do a cheap vertex check method
		Vector2 rectMin = Vector2.Min(corner1, corner2);
		Vector2 rectMax = Vector2.Max(corner1, corner2);

		Vector3 pos = part.transform.position;
		Quaternion rot = part.transform.rotation;
		Vector3 scale = part.transform.localScale;


		foreach (Vector3 vert in part.allVerts)
		{
			// convert vertex position to screen space
			Vector2 ss = Camera.main.WorldToScreenPoint(pos + rot * HF.MV3(scale, vert));
			if (ss.x >= rectMin.x && ss.x <= rectMax.x &&
				ss.y >= rectMin.y && ss.y <= rectMax.y) // vert in box
				return true;
		}
		return false;
	*/
	}

	// checks part's world bounds in ss intersection with rectangle
	bool PartWorldBoundsRectangleIntersect(Part part, Vector2 corner1, Vector2 corner2, Camera maincamera)
	{
		Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
		Bounds bounds = mesh.bounds;

		Vector3[] worldCorners = new Vector3[8] {
			new(bounds.min.x, bounds.min.y, bounds.min.z),
			new(bounds.min.x, bounds.min.y, bounds.max.z),
			new(bounds.min.x, bounds.max.y, bounds.min.z),
			new(bounds.min.x, bounds.max.y, bounds.max.z),
			new(bounds.max.x, bounds.min.y, bounds.min.z),
			new(bounds.max.x, bounds.min.y, bounds.max.z),
			new(bounds.max.x, bounds.max.y, bounds.min.z),
			new(bounds.max.x, bounds.max.y, bounds.max.z),
		};

		part.transform.TransformPoints(worldCorners);

		Vector2 screenBoxMin = Vector2.positiveInfinity;
		Vector2 screenBoxMax = Vector2.negativeInfinity;
		foreach (Vector3 corner in worldCorners) 
		{
			Vector2 ss = maincamera.WorldToScreenPoint(corner);
			screenBoxMin = Vector2.Min(screenBoxMin, ss);
			screenBoxMax = Vector2.Max(screenBoxMax, ss);
		}

		Vector2 rectMin = Vector2.Min(corner1, corner2);
		Vector2 rectMax = Vector2.Max(corner1, corner2);

		bool intersecting = 
			!(rectMax.x < screenBoxMin.x || rectMin.x > screenBoxMax.x ||
			rectMax.y < screenBoxMin.y || rectMin.y > screenBoxMax.y);
		return intersecting;
	}

	bool RectangleTriangleIntersect(
		Vector2 rectCorner1,  Vector2 rectCorner2, 
		Vector2 triCorner1, Vector2 triCorner2, Vector2 triCorner3)
	{
		Vector2 rectMin = Vector2.Min(rectCorner1, rectCorner2);
		Vector2 rectMax = Vector2.Max(rectCorner1, rectCorner2);
		
		Vector2 triMin = Vector2.Min(triCorner1, Vector2.Min(triCorner2, triCorner3));
		Vector2 triMax = Vector2.Max(triCorner1, Vector2.Max(triCorner2, triCorner3));

		// do the bounds not intersect/overlap?
		if (rectMax.x < triMin.x || rectMin.x > triMax.x || rectMax.y < triMin.y || rectMin.y > triMax.y)
			return false;

		// any vert of tri in rect (axis aligned)
		Vector2[] triCorners = new Vector2[3] { triCorner1, triCorner2, triCorner3 };
		foreach (Vector2 p in triCorners)
		{
			bool inBounds = 
				p.x >= rectMin.x && p.x <= rectMax.x &&
				p.y >= rectMin.y && p.y <= rectMax.y;

			if (inBounds)
				return true;
		}

		// any vert of rect in tri
		Vector2[] rectCorners = new Vector2[2] { rectCorner1, rectCorner2 };
		foreach (Vector2 p in rectCorners)
		{
			float d1, d2, d3;
			bool has_neg, has_pos;

			d1 = Sign(p, triCorner1, triCorner2);
			d2 = Sign(p, triCorner2, triCorner3);
			d3 = Sign(p, triCorner3, triCorner1);

			has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
			has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);
			if (!(has_neg && has_pos)) // intersecting
				return true;
		}

		// any edges intersect
		// optimization: only need to test 2 edges, at least one must intersect if they do
		for (int i = 0; i < 2; i++) // triangle verts iter
		{
			// test all 4 edges of rectangle against edge
			Vector2 triEdgep0 = triCorners[i];
			Vector2 triEdgep1 = triCorners[(i + 1) % 3];
			if (LineSegmentsIntersect(triEdgep0, triEdgep1,
				new(rectCorner1.x, rectCorner1.y), new(rectCorner1.x, rectCorner2.y)) ||
				LineSegmentsIntersect(triEdgep0, triEdgep1,
				new(rectCorner1.x, rectCorner2.y), new(rectCorner2.x, rectCorner2.y)) ||
				LineSegmentsIntersect(triEdgep0, triEdgep1,
				new(rectCorner2.x, rectCorner2.y), new(rectCorner2.x, rectCorner1.y)) ||
				LineSegmentsIntersect(triEdgep0, triEdgep1,
				new(rectCorner2.x, rectCorner1.y), new(rectCorner1.x, rectCorner1.y)))
				
				return true;
		}

		return false;
	}

	float Sign(Vector3 p1, Vector3 p2, Vector3 p3)
	{
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}

	// modified https://stackoverflow.com/a/1968345
	bool LineSegmentsIntersect(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
	{
		Vector2 s1 = p1 - p0;
		Vector2 s2 = p3 - p2;

		float denom = 1 / (-s2.x * s1.y + s1.x * s2.y);
		float s = (-s1.y * (p0.x - p2.x) + s1.x * (p0.y - p2.y)) * denom;
		float t = (s2.x * (p0.y - p2.y) - s2.y * (p0.x - p2.x)) * denom;

		return s >= 0 && s <= 1 && t >= 0 && t <= 1; // actual detection logic
	}

	void ClickCheck()
	{
		selectionChanged = true;

		Transform selected = null;
		if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out RaycastHit hit))
		{
			Part component = hit.transform.GetComponent<Part>();
			if (component && BuildingManager.Instance.Parts.Contains(component))
				selected = hit.transform;
		}

		if (selected == null)
		{
			if (!Conatrols.IM.Selection.Multiselect.IsPressed())
				selection = new();
			return;
		}

		if (Conatrols.IM.Selection.Multiselect.IsPressed()) 
		{	// toggle object in selection
			if (selection.Contains(selected))
				selection.Remove(selected);
			else
				selection.Add(selected);
		}
		else
			selection = new() { selected };
	}

	void GetMeshVertices(Transform target, ref List<Vector3> allVertices)
	{
		if (target.TryGetComponent(out MeshFilter meshFilter))
		{
			Mesh mesh = meshFilter.sharedMesh;
			if (mesh != null)
			{
				allVertices.AddRange(mesh.vertices); // Add vertices to the combined list
			}
		}

		// Recursively iterate through children
		foreach (Transform child in target.transform)
		{
			GetMeshVertices(child, ref allVertices);
		}
	}

	public void UpdateContainer()
	{
		// remove objects from the container that are no longer in selection 
		// (this is put before return, in case selection is empty then this will not happen
		foreach (Part p in BuildingManager.Instance.Parts)
		{
			Transform t = p.transform;
			if (!selection.Contains(t))
			{
				t.SetParent(BuildingManager.Instance.mainPartsContainer, true);
			}
		}
		
		// then break if the selection is empty
		if (selection.Count == 0)
		{
			BuildingManager.Instance.TransformTools.active = false;
			selectionContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			selectionContainer.transform.localScale = Vector3.one;

			return;
		}
		else
		{
			BuildingManager.Instance.TransformTools.active = true;
		}

		// handle position
		Vector3 totalPosition = Vector3.zero;
		foreach (Transform t in selection)
		{
			t.SetParent(BuildingManager.Instance.mainPartsContainer, true);
			totalPosition += t.position;
		}

		selectionContainer.position = totalPosition / selection.Count;
		BuildingManager.Instance.TransformTools.UpdatePosition();

		// handle rotation (local, single selection, otherwise will act globally)
		if (selection.Count == 1 && BuildingManager.Instance.TransformTools.local)
			selectionContainer.rotation = selection[0].transform.rotation;
		else
			selectionContainer.rotation = Quaternion.identity;

		foreach (Transform t in selection)
		{
			t.SetParent(selectionContainer, true);
		}
	}

	public void UpdateContextManager()
	{
		if (selection.Count == 0)
			ContextManager.Instance.selectionStatus = ContextManager.SelectionStatus.NoSelection;
		else if (selection.Count == 1)
			ContextManager.Instance.selectionStatus = ContextManager.SelectionStatus.SingleSelection;
		else
			ContextManager.Instance.selectionStatus = ContextManager.SelectionStatus.MultipleSelections;
	}
}