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
		float minX = Mathf.Min(mousePos.x, dragStart.x);
		float maxX = Mathf.Max(mousePos.x, dragStart.x);
		float minY = Mathf.Min(mousePos.y, dragStart.y);
		float maxY = Mathf.Max(mousePos.y, dragStart.y);

		if (controls.Selection.Multiselect.IsPressed())
		{
			selection = dragStartSelections;
		}
		else
		{
			selection = new();
		}

		Part[] parts = FindObjectsOfType<Part>();
		foreach (Part part in parts)
		{
			List<Vector3> allVerts = new();
			GetMeshVertices(part.transform, ref allVerts);

			bool allIn = true;
			foreach (Vector3 v in allVerts)
			{
				Vector2 screenPoint = Camera.main.WorldToScreenPoint(part.transform.TransformPoint(v)); // offset vert by world space pos 
				bool inBounds = screenPoint.x < maxX && screenPoint.y < maxY && screenPoint.x > minX && screenPoint.y > minY;
				if (!inBounds) { allIn = false; break; }
			}

			if (allIn && !selection.Contains(part.transform)) 
				selection.Add(part.transform);
		}
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