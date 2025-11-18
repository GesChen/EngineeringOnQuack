using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : Singleton<SelectionManager> {
	public bool selectionBoxDragging;

	public List<Transform> Selection { get; private set; }
	public Part[] PartSelection { get; private set; } // always in sync with selection
	HashSet<Part> PartSelectionHS;

	public Transform selectionContainer;

	public RectTransform UIBox;

	List<Transform> dragStartSelections;
	Vector2 mousePos;
	Vector2 dragStart;
	Vector2 dragStartPos;
	bool dragging;

	bool selectionChanged = false;
	bool overrideGroupSelect = false;
	
	/*{
		get { return m_changed; }
		set {
			if (value)
				OnSelectionChanged?.Invoke();
			
			m_changed = value; 
		}
	}*/
	[HideInNormalInspector] public float dragStartTime;

	/// <summary>
	/// Subscribe in something called from Buildingmanager after the clear
	/// should be around line 47
	/// </summary>
	public event Action OnSelectionChanged;
	public void ClearSelectionChanged() { OnSelectionChanged = null; }

	void Start() {
		Selection = new();
		PartSelection = new Part[0];

		Subscribe();
	}

	void Subscribe() {
		ContextObserver.Instance.GroupCheck = UpdateGroupContext;
		ContextObserver.Instance.RequestSelectionCount = () => Selection.Count;
		ContextObserver.Instance.GetCurrentSelectionInfo = () =>
			(PartSelection.Select(p => p.transform).ToArray(),
			PartSelection.Select(p => p.basePart.ID).ToArray());

		// do processing in here since ui doesnt depend on language
	}

	bool UpdateGroupContext() {
		// selection guaranteed to be multiple or 1 already 

		bool allPartsOfOneGroup = true;
		bool allGroupedParts = true;
		PartGroup group = null;
		foreach (var part in PartSelection) {
			if (part.Group != null) {
				group ??= part.Group;

				if (group != part.Group) {
					allPartsOfOneGroup = false;
				}
			} else {
				allGroupedParts = false;
			}
		}

		// group should not be null if any were in group
		if (group == null) return false;

		var context = ContextManager.EnterContext<Contexts.GroupSelection>();
		context.AllGroupedParts = allGroupedParts;
		context.AllPartsOfOneGroup = allPartsOfOneGroup;
		context.AllGroupPartsSelected = false;

		if (allPartsOfOneGroup && allGroupedParts) {
			// check if all part in group are selected
			// naive approach is just best KISS
			bool allSelected = true;
			foreach (var part in group.Parts) {
				if (!PartSelection.Contains(part)) {
					allSelected = false;
					break;
				}
			}

			context.AllGroupPartsSelected = allSelected;
		}
		return true;
	}

	void Update() {
		HandleInput();
		UpdatePartSelection();

		// ?????????????
		while (selectionChanged) {
			CheckForGroups();
			CheckForSnaps();
			CheckForLinks();
			RemoveDuplicates();
			UpdatePartSelection();
		}

		HandleContainer(); // selection might have changed from groups and snaps
	}

	void HandleInput() {
		//if (ContextManager.IsInContext<Contexts.OverUI>(out _)) return;
		if (UIHovers.results.Count > 0) return;
		CheckCancel();

		mousePos = Conatrols.Mouse.Position;

		// detect mouse down
		if (Conatrols.Mouse.Left.PressedThisFrame) {
			dragging = !(TransformTools.Instance.dragging || TransformTools.Instance.hovering);

			dragStart = mousePos;
			dragStartSelections = Selection;

			if (dragging) {
				dragStartPos = mousePos;
				dragStartTime = Time.time;
			}
		}

		// no selection right click check
		if ((Conatrols.Mouse.Right.PressedThisFrame
			|| Conatrols.Mouse.Left.PressedThisFrame) && Selection.Count == 0)
			ClickCheck();

		// detect mouse up
		if (!Conatrols.Mouse.Left.Pressed && !TransformTools.Instance.hovering) {
			if (dragging) {
				if (Vector2.Distance(mousePos, dragStartPos) < Config.Input.clickMaxMovement)
					ClickCheck();
				else
					FindObjectsInsideBounds(dragStart, mousePos);
			}
			dragging = false;
/*
			if (Time.time - mouseDownStartTime < Config.Input.clickMaxTimeMs / 1000f &&
				Vector2.Distance(mousePos, mouseDownStartPos) < Config.Input.clickMaxDist) { // counts as a click
				ClickCheck();
			} else*/
		}

		selectionBoxDragging = dragging;
		UIBox.gameObject.SetActive(dragging);

		if (dragging) {
			HandleBox();
		}
	}

	void CheckCancel() {
		if (Conatrols.IM.Building.CancelSelection.WasPressedThisFrame()) {
			Selection.Clear(); // it cant be this simple rights
			selectionChanged = true;
		}
	}

	void UpdatePartSelection() {
		if (!selectionChanged) return;

		PartSelection = Selection.Select(t => t.GetComponent<Part>()).ToArray();
		PartSelectionHS = PartSelection.ToHashSet();
	}

	void HandleContainer() {
		if (!selectionChanged) return;

		UpdateContainer();
		selectionChanged = false;
	}

	void CheckForGroups() {
		if (overrideGroupSelect) return;

		var oldhs = Selection.ToHashSet();

		// if any selected part is in a group, select parts in that group not already in selection
		foreach (var part in PartSelection) {
			if (part.Group != null)
				foreach (var item in part.Group.Parts)
					if (!PartSelectionHS.Contains(item)) {
						Selection.Add(item.transform);
						selectionChanged = true;
					}
		}

		if (!Selection.ToHashSet().SetEquals(oldhs)) selectionChanged = true;
	}

	void CheckForSnaps() {
		// why.
		// add to selection any nonselected parts which meet the checksnap of any 
		// selected part's snaptargets

		var oldhs = Selection.ToHashSet();

		Selection.AddRange( // add to selection
			PartSelection.SelectMany(part => // any selected part's
				part.GetComponentsInChildren<SnapTarget>() // snaptargets
				.SelectMany(cst => 
					BuildingManager.Instance.Assembly.Parts.Where(p => // any 
					p != part // self check for good 
					&& cst.CheckSnap(p.transform))) // which meet the checksnap
			).Select(p => p.transform)
		);

		if (!Selection.ToHashSet().SetEquals(oldhs)) selectionChanged = true;
	}

	void CheckForLinks() {
		// hes doing it again :(
		
		var oldhs = Selection.ToHashSet();

		foreach (var p in PartSelection) {
			if (p.IsNonStaticPart(out var nsp)) {
				// Selection.AddRange(nsp.LinkedParts));
			}
		}
		
		if (!Selection.ToHashSet().SetEquals(oldhs)) selectionChanged = true;
	}

	void RemoveDuplicates() {
		Selection = Selection.Distinct();
	}

	void HandleBox() {
		Vector2 size = dragStart - mousePos;
		UIBox.position = (dragStart + mousePos) / 2;
		UIBox.sizeDelta = HF.Vector2Abs(size);
	}

	void FindObjectsInsideBounds(Vector2 boundsStart, Vector2 boundsEnd) {
		// handle multiselection
		if (Conatrols.IM.Building.Multiselect.IsPressed())
			Selection = dragStartSelections;
		else
			Selection = new();

		Camera maincamera = Camera.main;
		foreach (Part part in BuildingManager.Instance.Assembly.Parts) {
			if (part == null) continue;

			if (PartIntersectsWithSelectionBox(part, boundsStart, boundsEnd, maincamera) &&
				!Selection.Contains(part.transform)) {
				Selection.Add(part.transform);
			}
		}

		selectionChanged = true;
	}

	bool PartIntersectsWithSelectionBox(Part part, Vector2 corner1, Vector2 corner2, Camera maincamera) {
		// following code is super fuckin slow if future person can optimize please do idfk what to do it dropst  olike 7 fps
		if (!PartWorldBoundsRectangleIntersect(part, corner1, corner2, maincamera))
			return false;

		Vector3[] tris = part.basePart.AllTriPositions.ToArray();

		part.transform.TransformPoints(tris);

		for (int i = 0; i < tris.Length; i += 3) {
			Vector3 v1 = tris[i]; //pos + rot * Vector3.Scale(scale, tris[i]);
			Vector3 v2 = tris[i + 1]; //pos + rot * Vector3.Scale(scale, tris[i + 1]);
			Vector3 v3 = tris[i + 2]; //pos + rot * Vector3.Scale(scale, tris[i + 2]);

			Vector2 ss1 = maincamera.WorldToScreenPoint(v1);
			Vector2 ss2 = maincamera.WorldToScreenPoint(v2);
			Vector2 ss3 = maincamera.WorldToScreenPoint(v3);

			bool intersect = Intersections.RectangleTriangle2D(corner1, corner2, ss1, ss2, ss3);

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
	bool PartWorldBoundsRectangleIntersect(Part part, Vector2 corner1, Vector2 corner2, Camera maincamera) {
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
		foreach (Vector3 corner in worldCorners) {
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

	void ClickCheck() {
		selectionChanged = true;
		overrideGroupSelect = false;

		Transform selected = null;
		if (Physics.Raycast(Camera.main.ScreenPointToRay(mousePos), out RaycastHit hit)) {
			Part component = hit.transform.GetComponentInParent<Part>();
			if (component && BuildingManager.Instance.Assembly.Parts.Contains(component))
				selected = component.transform;
		}

		if (selected == null) {
			if (!Conatrols.IM.Building.Multiselect.IsPressed())
				Selection = new();
			return;
		}

		if (Conatrols.IM.Building.Multiselect.IsPressed()) { // toggle object in selection
			if (Selection.Contains(selected))
				Selection.Remove(selected);
			else
				Selection.Add(selected);
		} else {
			bool partOfGroup = selected.GetComponent<Part>().Group != null;
			bool alreadySelected = Selection.Contains(selected);

			if (partOfGroup && alreadySelected)
				overrideGroupSelect = true;

			Selection = new() { selected };
		}
	}

	void GetMeshVertices(Transform target, ref List<Vector3> allVertices) {
		if (target.TryGetComponent(out MeshFilter meshFilter)) {
			Mesh mesh = meshFilter.sharedMesh;
			if (mesh != null) {
				allVertices.AddRange(mesh.vertices); // Add vertices to the combined list
			}
		}

		// Recursively iterate through children
		foreach (Transform child in target.transform) {
			GetMeshVertices(child, ref allVertices);
		}
	}

	public void UpdateContainer() {
		OnSelectionChanged?.Invoke();

		// remove objects from the container that are no longer in selection 
		// (this is put before return, in case selection is empty then this will not happen
		foreach (Part p in BuildingManager.Instance.Assembly.Parts) {
			Transform t = p.transform;
			if (!Selection.Contains(t)) {
				t.SetParent(BuildingManager.Instance.MainPartsContainer, true);
			}
		}

		// then break if the selection is empty
		if (Selection.Count == 0) {
			TransformTools.Instance.active = false;
			selectionContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			selectionContainer.transform.localScale = Vector3.one;

			return;
		} else {
			TransformTools.Instance.active = true;
		}

		// handle position
		Vector3 totalPosition = Vector3.zero;
		foreach (Transform t in Selection) {
			t.SetParent(BuildingManager.Instance.MainPartsContainer, true);
			totalPosition += t.position;
		}

		selectionContainer.position = totalPosition / Selection.Count;
		TransformTools.Instance.UpdatePosition();

		// handle rotation (local, single selection, otherwise will act globally)
		if (Selection.Count == 1 && TransformTools.Instance.local)
			selectionContainer.rotation = Selection[0].transform.rotation;
		else
			selectionContainer.rotation = Quaternion.identity;

		foreach (Transform t in Selection) {
			t.SetParent(selectionContainer, true);
		}

	}

	public void Clear() {
		Selection.Clear();

		selectionChanged = true;
	}

	public void SetSelection(params Transform[] transforms) {
		Selection = transforms.ToList();

		selectionChanged = true;
	}

	public void AddSelection(params Transform[] transforms) {
		foreach (var t in transforms) {
			if (!Selection.Contains(t))
				Selection.Add(t);
		}

		selectionChanged = true;
	}
}