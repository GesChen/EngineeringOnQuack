using System.Collections.Generic;
using UnityEngine;

struct Assembly {
	public string name;
	public List<PartInfo> parts;
	public bool didPrecomputations;
	public List<SerializableSubassembly> precomputedSubassemblies;
	// to add onto
}

public class BuildingManager : Singleton<BuildingManager> {
	public Transform mainPartsContainer;
	public List<BasePart> BaseParts;
	public List<Part> Parts;
	public TransformTools TransformTools;
	public Transform SimulationContainer;
	public static Dictionary<string, BasePart> AllParts = new();
	public GameObject templatePart;

	void Start() {
		Subscribe();
	}

	// hit that bell for more epic code (this is garbage)
	// i made this at like 12 am with box on call lmao
	void Subscribe() {
		RightClickMenus.ClearEvent();
		RightClickMenus.OnNewPartMade += MakeNewPart;

		GameManager.Instance.OnStartSimulating += StartSimulating;
		GameManager.Instance.OnStopSimulating += StopSimulating;
	}

	void MakeNewPart(string name) {
		var newpart = GeneratePart(name);

		// place part
		// the container provides all the functionality i need already
		Vector3 planeOrigin = SelectionManager.Instance.selectionContainer.position;
		Vector3 planeDir = (Camera.main.transform.position - planeOrigin).normalized;
		Ray ray = Camera.main.ScreenPointToRay(RightClick.Instance.downPos); // use right click pos not current

		Vector3 pos = HF.RayPlaneIntersect(planeOrigin, planeDir, ray.origin, ray.direction);
		newpart.transform.position = pos;

		Parts.Add(newpart);

		PartsUpdated();

	}

	void Update() {
		//Parts = mainPartsContainer.GetComponentsInChildren<Part>().OrderBy(part => part.ID).ToList(); // sort by id to make sure current stays in the same order

		foreach (Part part in Parts) {
			part.Selected = SelectionManager.Instance.selection.Contains(part.transform);
		}
	}

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

	public void PartsUpdated() {
		UpdateIds();
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

	void DeselectAllParts() {
		Parts.ForEach(p => { p.Selected = false; p.gameObject.layer = LayerMask.NameToLayer("Part"); });
	}
}