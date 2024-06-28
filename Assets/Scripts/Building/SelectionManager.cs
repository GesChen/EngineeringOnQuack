using Codice.Client.BaseCommands;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
	public BuildingManager main;

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
	int lastSelectionCount;

	#region inputmaster
	InputMaster controls;

	void Awake()
	{
		controls = new InputMaster();
	}
	void OnEnable()
	{
		controls ??= new InputMaster();
		controls.Enable();
	}
	void OnDisable()
	{
		controls.Disable();
	}
	#endregion

	void Update()
	{
		HandleInput();
		HandleContainer();
	}

	void HandleInput()
	{
		bool mouseDown = controls.Selection.Drag.IsPressed();
		mousePos = controls.Selection.MousePos.ReadValue<Vector2>();

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
		if (!mouseDown && mouseDown != lastMouseDown && !main.transformTools.hovering)
		{
			if (Time.time - mouseDownStartTime < Controls.clickMaxTime &&
				Vector2.Distance(mousePos, mouseDownStartPos) < Controls.clickMaxDist)
			{ // counts as a click
				ClickCheck();
			}
		}

		dragging = mouseDown && !(main.transformTools.dragging || main.transformTools.hovering);
		UIBox.gameObject.SetActive(dragging);

		if (dragging)
		{
			HandleBox();
			FindObjectsInsideBounds();
		}

		lastMouseDown = mouseDown;
	}

	void HandleContainer()
	{
		if (selection.Count != lastSelectionCount)
		{
			UpdateContainer();
		}
		lastSelectionCount = selection.Count;
	}
	
	void HandleBox()
	{
		Vector2 size = dragStart - mousePos;
		UIBox.position = (dragStart + mousePos) / 2;
		UIBox.sizeDelta = HF.Vector2Abs(size);
	}

	void FindObjectsInsideBounds()
	{
		// handle multiselection
		if (controls.Selection.Multiselect.IsPressed())
			selection = dragStartSelections;
		else
			selection = new();

		foreach (Part part in main.Parts)
		{
			if (part == null) continue;

			if (PartIntersectsWithSelectionBox(part, mousePos, dragStart) && 
				!selection.Contains(part.transform))
			{
				selection.Add(part.transform);
			}
		}
	}

	bool PartIntersectsWithSelectionBox(Part part, Vector2 corner1, Vector2 corner2)
	{
		/* following code is super fuckin slow if future person can optimize please do idfk what to do it dropst  olike 7 fps
		Vector3[] tris = part.allTris;
		for (int i = 0; i < tris.Length; i += 3)
		{
			Vector3 v1 = part.transform.position + (Vector3)(part.transform.localToWorldMatrix * tris[i]);
			Vector3 v2 = part.transform.position + (Vector3)(part.transform.localToWorldMatrix * tris[i + 1]);
			Vector3 v3 = part.transform.position + (Vector3)(part.transform.localToWorldMatrix * tris[i + 2]);

			Vector2 ss1 = Camera.main.WorldToScreenPoint(v1);
			Vector2 ss2 = Camera.main.WorldToScreenPoint(v2);
			Vector2 ss3 = Camera.main.WorldToScreenPoint(v3);

			bool intersect = RectangleTriangleIntersect(corner1, corner2, ss1, ss2, ss3);

			if (intersect)
				return true;
		}
		*/
		// for now just do a cheap vertex check method
		Vector2 rectMin = Vector2.Min(corner1, corner2);
		Vector2 rectMax = Vector2.Max(corner1, corner2);

		foreach (Vector3 vert in part.allVerts)
		{
			// convert vertex position to screen space
			Vector2 ss = Camera.main.WorldToScreenPoint(part.transform.position + (Vector3)(part.transform.localToWorldMatrix * vert));
			if (ss.x >= rectMin.x && ss.x <= rectMax.x &&
				ss.y >= rectMin.y && ss.y <= rectMax.y) // vert in box
				return true;
		}
		return false;
	}

	bool RectangleTriangleIntersect(
		Vector2 rectCorner1,  Vector2 rectCorner2, 
		Vector2 triCorner1, Vector2 triCorner2, Vector2 triCorner3)
	{
		Vector2 rectMin = Vector2.Min(rectCorner1, rectCorner2);
		Vector2 rectMax = Vector2.Max(rectCorner1, rectCorner2);

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
		Transform selected = null;
		if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out RaycastHit hit))
		{
			Part component = hit.transform.GetComponent<Part>();
			if (component && main.Parts.Contains(component))
				selected = hit.transform;
		}

		if (selected == null) return;

		if (controls.Selection.Multiselect.IsPressed()) 
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
		foreach (Part p in main.Parts)
		{
			Transform t = p.transform;
			if (!selection.Contains(t))
			{
				t.SetParent(main.mainPartsContainer, true);
			}
		}
		
		// then break if the selection is empty
		if (selection.Count == 0)
		{
			main.transformTools.active = false;
			selectionContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			selectionContainer.transform.localScale = Vector3.one;

			return;
		}
		else
		{
			main.transformTools.active = true;
		}

		// handle position
		Vector3 totalPosition = Vector3.zero;
		foreach (Transform t in selection)
		{
			t.SetParent(main.mainPartsContainer, true);
			totalPosition += t.position;
		}

		selectionContainer.position = totalPosition / selection.Count;
		main.transformTools.UpdatePosition();

		// handle rotation (local, single selection, otherwise will act globally)
		if (selection.Count == 1 && main.transformTools.local)
			selectionContainer.rotation = selection[0].transform.rotation;
		else
			selectionContainer.rotation = Quaternion.identity;

		foreach (Transform t in selection)
		{
			t.SetParent(selectionContainer, true);
		}
	}
}