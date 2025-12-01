using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : Singleton<BuildingManager> {
	public Assembly Assembly;

	public event Action OnModified; // only sub to this in start, its cleared in awake
	[HideInNormalInspector] public bool Dirty;
	/// <summary>
	/// Call this method whenever a change to the assembly is made! ANY CHANGE!
	/// </summary>
	public static void SetDirty() { 
		Instance.Dirty = true;
		Instance.OnModified?.Invoke();
	}

	public event Action OnPartCreated;
	public event Action OnNewAssemblyMade;
	
	#region Setup
	protected override void Awake() {
		base.Awake();

		OnModified = null;
		OnPartCreated = null;
		OnNewAssemblyMade = null;

		Assembly = new();

		Subscribe();
	}

	// hit that bell for more epic code (this is garbage)
	// i made this at like 12 am with box on call lmao
	void Subscribe() {
		GameManager.Instance.BM_TryLoadAssembly = TryLoad;
		GameManager.Instance.BM_ClearEditing = ClearEditing;

		RightClickMenus.OnNewPartMade	= name => MakeNewPart(name, true);
		RightClickMenus.OnDelete		= DeleteSelection;
		RightClickMenus.OnCopy			= Copy;
		RightClickMenus.OnPaste			= Paste;
		RightClickMenus.OnDuplicate		= Duplicate;

		Conatrols.IM.Editing_General.Copy		.Subscribe<Contexts.Editing>(Copy);
		Conatrols.IM.Editing_General.Cut		.Subscribe<Contexts.Editing>(Cut);
		Conatrols.IM.Editing_General.Paste		.Subscribe<Contexts.Editing>(Paste);
		Conatrols.IM.Editing_General.Duplicate	.Subscribe<Contexts.Editing>(Duplicate);

		MaterialEditingMenu.OnStart = MaterialEditor.SetupComponent;
		MaterialEditingMenu.OnRequestCompositionItems = GenerateWindowItems;
		GroupManager.Instance.Subscribe();

		BottomBar.OnAssemble = AssemblePressed;

		OperatingMainUI.TopBar.OnReturnToEditing = GameManager.Instance.BeginEditing;

		BottomBar.OnNameChanged = ChangeName;
		BottomBar.OnNameChanged += _ => SetDirty();

		BottomBar.OnNewPressed = New;

		Part_CPU.SetupUI();
		Part_Transceiver.Setup();
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
		if (!ContextManager.CurrentlyInContext<Contexts.Editing>()) return;

		HandleInput();

		// set selection state of parts
		foreach (Part part in Assembly.Parts) {
			part.Selected = SelectionManager.Instance.PartSelection.Contains(part);
		}
	}

	// organize this later
	public string AssemblyToLoadPath;
	void TryLoad() {
		if (!string.IsNullOrWhiteSpace(AssemblyToLoadPath))
			SaveLoadManager.Instance.LoadFromPath(AssemblyToLoadPath);
		else
			Assembly = new();
	}

	void AssemblePressed() {
		GameManager.Instance.AssembleFromEditing();
		GameManager.Instance.Operate();
	}

	void HandleInput() {
		if (Conatrols.IM.Editing_Building.Delete.WasPressedThisFrame() &&
			ContextManager.CurrentlyInContext<Contexts.InWorld>(out _)) {

			DeleteSelection();
			RightClick.Instance.Hide();
		}
	}

	#region Part Functions
	void UpdateParts() {
		// used to update ids but now just a placeholder
	}

	public Part MakeNewPart(int basePartID, bool select, bool addSelection = false) {
		int bpIndex = AllParts.BaseParts.FindIndex(bp => bp.ID == basePartID);
		if (bpIndex == -1)
			throw new($"[INTERNAL] basepart #\"{basePartID}\" doesn't exist");

		BasePart bp = AllParts.BaseParts[bpIndex];
		return MakeNewPart(bp.Name, select, addSelection);
	}

	public Part MakeNewPart(string name, bool select, bool addSelection = false) {
		var newpart = GeneratePart(name);

		// place part
		// the container provides all the functionality i need already

		newpart.transform.position = PlacePos();

		if (select) {
			if (addSelection)
				SelectionManager.Instance.AddSelection(newpart.transform);
			else 
				SelectionManager.Instance.SetSelection(newpart.transform);
		}

		if (newpart.IsNonStaticPart(out var nsp))
			nsp.OnPartCreation();

		Assembly.Parts.Add(newpart);

		UpdateParts();

		SetDirty();

		OnPartCreated?.Invoke();

		return newpart;
	}

	// function for getting a position for placing parts based on selection and mouse position
	Vector3 PlacePos(bool useCurrent = false) {
		Vector3 planeOrigin = SelectionManager.Instance.selectionContainer.position;
		Vector3 planeDir = -Camera.main.transform.forward;
		Ray ray = Camera.main.ScreenPointToRay(
			useCurrent
			? Conatrols.Mouse.Position
			: RightClick.Instance.downPos);

		Vector3 pos = HF.RayPlaneIntersect(planeOrigin, planeDir, ray.origin, ray.direction);

		return pos;
	}

	void NewAssembly() {
		ResetPartsAndGroups();
		Assembly = new();

		EditingOutputManager.Instance.UpdateMenu();
		OutputsMenu.Hide();

		BottomBar.UpdateNameText("");

		SelectionManager.Instance.SetSelection();
		SelectionManager.Instance.UpdateContainer();

		OnNewAssemblyMade?.Invoke();
	}
	public void ResetPartsAndGroups() {
		foreach (Part part in Assembly.Parts) {
			Destroy(part.gameObject);
		}

		Assembly.Parts.Clear();

		Assembly.Groups.Clear();
	}

	// DO NOT USE THESE FOR MAKING NEW PARTS IN CODE!!
	// USE MAKENEWPART INSTEAD
	// idk why i didnt just make these private to begin with
	private Part GeneratePart(int basePartID) {
		int bpIndex = AllParts.BaseParts.FindIndex(bp => bp.ID == basePartID);
		if (bpIndex == -1)
			throw new($"[INTERNAL] basepart #\"{basePartID}\" doesn't exist");

		BasePart bp = AllParts.BaseParts[bpIndex];

		return GeneratePart(bp);
	}

	private Part GeneratePart(string basePartName) {
		int bpIndex = AllParts.BaseParts.FindIndex(bp => bp.Name == basePartName);
		if (bpIndex == -1)
			throw new($"[INTERNAL] basepart \"{basePartName}\" doesn't exist");

		BasePart bp = AllParts.BaseParts[bpIndex];

		return GeneratePart(bp);
	}

	// main generatepart method (notice its private)
	private Part GeneratePart(BasePart bp) {
		GameObject newPart = Instantiate(bp.Prefab, GameManager.Instance.MainPartsContainer);
		Part part = newPart.GetComponent<Part>();
		part.basePart = bp;

		part.ID = HF.UIDHashFunction(); // may change this
		// 10-19-25 changed to random instead of datettime

		return part;
	}
	#endregion

	public void BeginEditing() {
		SelectionManager.Instance.enabled = true;
	}

	public void ClearEditing() {
		SelectionManager.Instance.Clear();
		SelectionManager.Instance.enabled = false;
		TransformTools.Instance.active = false;

		DestroyEditingParts();
	}

	void DestroyEditingParts() {
		foreach (Part part in Assembly.Parts) {
			Destroy(part.gameObject);
		}
	}

	#region Selection
	void DeselectAllParts() {
		Assembly.Parts.ForEach(p => { p.Selected = false; p.gameObject.layer = LayerMask.NameToLayer("Part"); });
	}

	void DeleteSelection() {
		// delete current selection
		List<Transform> additionalToDelete = new();

		foreach (var part in SelectionManager.Instance.PartSelection) {
			// if (part.IsNonStaticPart(out var nsp)) {
			// 	additionalToDelete.AddRange(nsp.LinkedParts.Select(p => p.transform));
			// }

			// manually handle cc deletion for now 
			if (part.IsNonStaticPart(out var nsp)) {
				if (nsp is Part_CableConnection cc) {
					// delete cable and other cc if not selected
					Part_Cable cable = cc.Cable;
					if (Assembly.Parts.Contains(cable.Part)) {
						DeletePart(cable.Part);
						cc.Cable = null;
					}
					
					Part other = cable.OtherCC(cc).Part;
					if (!SelectionManager.Instance.PartSelectionHS.Contains(other)){
						DeletePart(other);
					}
				}
			}

			DeletePart(part);
		}

		// if (additionalToDelete.Length > 0) {
		// 	SelectionManager.Instance.SetSelection(additionalToDelete.ToArray());
		// 	SelectionManager.Instance.HandleContainer(); // force update
		// 	DeleteSelection();
		// }

		SelectionManager.Instance.Clear();
		UpdateParts();

		SetDirty();
	}

	void DeletePart(Part part) {
		if (!Assembly.Parts.Contains(part)) 
			Debug.LogError("Deleting part that isn't in the parts list");
		else
			Assembly.Parts.Remove(part);

		Destroy(part.gameObject);
	}
	#endregion

	public void New() {
		if (!Dirty) NewAssembly();
		else {
			UnsavedWorkMenu.Notify((choice) => {
				switch (choice) {
					case UnsavedWorkMenu.Choice.Save:
						SaveLoadManager.Instance.Save();
						NewAssembly();
						break;
					case UnsavedWorkMenu.Choice.Discard:
						NewAssembly();
						break;
					case UnsavedWorkMenu.Choice.Cancel:
						break; // do nothing
				}
			});
		}
	}

	void Copy() {
		Assembly.Clipboard.Copy(); // uses the most current version of assembly
	}

	void Cut() {
		DeleteSelection();

		Copy();
	}

	void Paste() {
		var newparts = Assembly.Clipboard.Paste(
			PlacePos(true),
			true,
			!Conatrols.Keyboard.Modifiers.Shift);
		if (newparts == null) return; // failed, no objects to paste

		Assembly.Parts.AddRange(newparts);

		UpdateParts();

		SetDirty();
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