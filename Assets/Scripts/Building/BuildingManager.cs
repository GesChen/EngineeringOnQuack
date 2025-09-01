using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : Singleton<BuildingManager> {
	public Assembly Assembly;

	public Transform MainPartsContainer;
	public TransformTools TransformTools;
	public Transform SimulationContainer;

	#region Setup
	protected override void Awake() {
		base.Awake();

		Assembly = new();

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
		RightClickMenus.OnCopy += Copy;
		RightClickMenus.OnPaste += Paste;
		RightClickMenus.OnDuplicate += Duplicate;

		GameManager.Instance.OnStartSimulating += StartSimulating;
		GameManager.Instance.OnStopSimulating += StopSimulating;

		MaterialEditingMenu.OnStart += MaterialEditor.SetupComponent;
		MaterialEditingMenu.OnRequestCompositionItems += GenerateWindowItems;
		GroupManager.Instance.Subscribe();

		BottomBar.ClearAssemble();
		BottomBar.OnAssemble += GameManager.Instance.StartSimulating;

		SimulatingMainUI.TopBar.ClearReturnToEditing();
		SimulatingMainUI.TopBar.OnReturnToEditing += GameManager.Instance.StopSimulating;

		BottomBar.ClearNameChanged();
		BottomBar.OnNameChanged += ChangeName;
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
		foreach (Part part in Assembly.Parts) {
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
	void UpdateParts() {
		// used to update ids but now just a placeholder
	}

	void MakeNewPart(string name, bool select) {
		var newpart = GeneratePart(name);

		// place part
		// the container provides all the functionality i need already
		
		newpart.transform.position = PlacePos();

		if (select)
			SelectionManager.Instance.ManuallySelect(newpart.transform);

		Assembly.Parts.Add(newpart);

		UpdateParts();
	}

	// function for getting a position for placing parts based on selection and mouse position
	Vector3 PlacePos() {
		Vector3 planeOrigin = SelectionManager.Instance.selectionContainer.position;
		Vector3 planeDir = -Camera.main.transform.forward;
		Ray ray = Camera.main.ScreenPointToRay(RightClick.Instance.downPos); // use right click pos not current

		Vector3 pos = HF.RayPlaneIntersect(planeOrigin, planeDir, ray.origin, ray.direction);

		return pos;
	}

	public void NewAssembly() {
		ResetPartsAndGroups();
		Assembly = new();
	}
	public void ResetPartsAndGroups() {
		foreach (Part part in Assembly.Parts) {
			Destroy(part.gameObject);
		}
		
		Assembly.Parts.Clear();

		Assembly.Groups.Clear();
	}

	public Part GeneratePart(int basePartID) {
		int bpIndex = AllParts.BaseParts.FindIndex(bp => bp.ID == basePartID);
		if (bpIndex == -1)
			throw new($"[INTERNAL] basepart #\"{basePartID}\" doesn't exist");

		BasePart bp = AllParts.BaseParts[bpIndex];

		return GeneratePart(bp);
	}

	public Part GeneratePart(string basePartName) {
		int bpIndex = AllParts.BaseParts.FindIndex(bp => bp.Name == basePartName);
		if (bpIndex == -1)
			throw new($"[INTERNAL] basepart \"{basePartName}\" doesn't exist");

		BasePart bp = AllParts.BaseParts[bpIndex];

		return GeneratePart(bp);
	}

	private Part GeneratePart(BasePart bp) {
		GameObject newPart = Instantiate(bp.Prefab, MainPartsContainer);
		Part part = newPart.GetComponent<Part>();
		part.basePart = bp;

		part.ID = DateTime.UtcNow.GetHashCode(); // may change this

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
	}

	public void StopSimulating() {
		SelectionManager.Instance.enabled = true;
		//TransformTools.enabled = true;

		ShowAllPartsAfterSimulation();
	}

	public void ReturnAllPartsToMain() {
		foreach (Part part in Assembly.Parts) {
			part.transform.parent = MainPartsContainer;
		}
	}
	
	void HideAllPartsForSimulation() {
		foreach (Part part in Assembly.Parts) {
			part.gameObject.SetActive(false);
		}
	}
	
	void ShowAllPartsAfterSimulation() {
		foreach (Part part in Assembly.Parts) {
			part.gameObject.SetActive(true);
		}
	}
	#endregion

	#region Selection
	void DeselectAllParts() {
		Assembly.Parts.ForEach(p => { p.Selected = false; p.gameObject.layer = LayerMask.NameToLayer("Part"); });
	}

	void DeleteSelection() {
		// delete current selection
		foreach (var part in SelectionManager.Instance.PartSelection) {
			if (!Assembly.Parts.Contains(part)) Debug.LogError("Deleting part that isn't in the parts list");
			else
				Assembly.Parts.Remove(part);

			Destroy(part.gameObject);
		}

		SelectionManager.Instance.Clear();
		UpdateParts();
	}
	#endregion

	void Copy() {
		Assembly.Clipboard.Copy(); // uses the most current version of assembly
	}

	void Paste() {
		var newparts = Assembly.Clipboard.Paste(PlacePos(), true);
		if (newparts == null) return; // failed, no objects to paste

		Assembly.Parts.AddRange(newparts);

		UpdateParts();
	}

	void Duplicate() {
		Assembly.Clipboard.Copy();
		Paste();
	}

	public void ChangeName(string name) {
		Assembly.Name = name;

		BottomBar.UpdateNameText(name);
	}
}