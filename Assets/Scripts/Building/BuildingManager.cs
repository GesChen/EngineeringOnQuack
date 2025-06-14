using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : Singleton<BuildingManager> {
	public Transform mainPartsContainer;
	public List<BasePart> BaseParts;
	public List<Part> Parts;
	public TransformTools TransformTools;
	public Transform SimulationContainer;
	public static Dictionary<string, BasePart> AllParts = new();
	public GameObject templatePart;

	BuildingClipboard clipboard;

	#region Setup
	protected override void Awake() {
		base.Awake();
		clipboard = new();

		Subscribe();
	}

	// hit that bell for more epic code (this is garbage)
	// i made this at like 12 am with box on call lmao
	void Subscribe() {
		RightClickMenus.ClearEvents();
		MaterialEditingMenu.ClearEvents();

		RightClickMenus.OnNewPartMade += 
			name => MakeNewPart(name, true);
		RightClickMenus.OnDelete += DeleteSelection;
		RightClickMenus.OnCopy += clipboard.Copy;
		RightClickMenus.OnPaste += Paste;
		RightClickMenus.OnDuplicate += Duplicate;
		RightClickMenus.OnGroup += GroupManager.Instance.GroupCurrentSelection;

		GameManager.Instance.OnStartSimulating += StartSimulating;
		GameManager.Instance.OnStopSimulating += StopSimulating;

		MaterialEditingMenu.OnStart += MaterialEditor.SetupComponent;
		MaterialEditingMenu.OnRequestCompositionItems += GenerateWindowItems;
	}

	WindowItem[] GenerateWindowItems() {
		WindowItem[] items = Compositions.All.Select((c, i) =>
			WindowItem.NewButtonCustomImageOverlay(
				"Composition option",
				new (() => MaterialEditingMenu.SelectComposition(i)),
				new (c.Icon),
				WindowItem.LayoutConfig.LayoutElement(new(MaterialEditingMenu.size, MaterialEditingMenu.size))
				).AddDescription(c.Name)
			).ToArray();

		return items;
	}
	#endregion

	void Update() {
		HandleInput();

		// set selection state of parts
		foreach (Part part in Parts) {
			part.Selected = SelectionManager.Instance.PartSelection.Contains(part);
		}
	}

	void HandleInput() {

		if (Conatrols.IM.Building.Delete.WasPressedThisFrame()) {
			DeleteSelection();
			RightClick.Instance.Hide();
		}
	}

	#region Part Functions
	public void UpdateParts() {
		UpdateIds();
	}

	void MakeNewPart(string name, bool select) {
		var newpart = GeneratePart(name);

		// place part
		// the container provides all the functionality i need already
		
		newpart.transform.position = PlacePos();

		if (select)
			SelectionManager.Instance.ManuallySelect(newpart.transform);

		Parts.Add(newpart);

		UpdateParts();
	}

	// function for getting a position for placing parts based on selection and mouse position
	Vector3 PlacePos() {
		Vector3 planeOrigin = SelectionManager.Instance.selectionContainer.position;
		Vector3 planeDir = (Camera.main.transform.position - planeOrigin).normalized;
		Ray ray = Camera.main.ScreenPointToRay(RightClick.Instance.downPos); // use right click pos not current

		Vector3 pos = HF.RayPlaneIntersect(planeOrigin, planeDir, ray.origin, ray.direction);
		return pos;
	}

	void UpdateIds() {
		int id = 0;
		foreach (Part part in Parts) {
			part.ID = id++;
		}
	}

	public void ResetParts() {
		foreach (Part part in Parts) {
			Destroy(part.gameObject);
		}
		Parts.Clear();
	}

	public Part GeneratePart(string basePartName) {
		int bpIndex = BaseParts.FindIndex(bp => bp.partName == basePartName);
		if (bpIndex == -1)
			throw new($"basepart \"{basePartName}\" doesn't exist");

		BasePart bp = BaseParts[bpIndex];
		GameObject newPart = Instantiate(bp.prefab, mainPartsContainer);
		Part part = newPart.GetComponent<Part>();
		part.basePart = bp;

		return part;
	}
	#endregion

	#region Simulation
	public void StartSimulating() {
		SelectionManager.Instance.Clear();
		SelectionManager.Instance.enabled = false;
		TransformTools.active = false;
		//TransformTools.enabled = false;

		DeselectAllParts();
		ReturnAllPartsToMain();
		HideAllPartsForSimulation();
		SimulationManager.Instance.StartSimulating();
	}

	public void StopSimulating() {
		SelectionManager.Instance.enabled = true;
		//TransformTools.enabled = true;

		SimulationManager.Instance.StopSimulating();
		ShowAllPartsAfterSimulation();
	}

	public void ReturnAllPartsToMain() {
		foreach (Part part in Parts) {
			part.transform.parent = mainPartsContainer;
		}
	}
	
	void HideAllPartsForSimulation() {
		foreach (Part part in Parts) {
			part.gameObject.SetActive(false);
		}
	}
	
	void ShowAllPartsAfterSimulation() {
		foreach (Part part in Parts) {
			part.gameObject.SetActive(true);
		}
	}
	#endregion

	#region Selection
	void DeselectAllParts() {
		Parts.ForEach(p => { p.Selected = false; p.gameObject.layer = LayerMask.NameToLayer("Part"); });
	}

	void DeleteSelection() {
		// delete current selection
		foreach (var part in SelectionManager.Instance.PartSelection) {
			if (!Parts.Contains(part)) Debug.LogError("Deleting part that isn't in the parts list");
			else
				Parts.Remove(part);

			Destroy(part.gameObject);
		}

		SelectionManager.Instance.Clear();
		UpdateParts();
	}
	#endregion

	void Paste() {
		var newparts = clipboard.Paste(PlacePos(), true);
		if (newparts == null) return; // failed, no objects to paste

		Parts.AddRange(newparts);

		UpdateParts();
	}

	void Duplicate() {
		clipboard.Copy();
		Paste();
	}
}