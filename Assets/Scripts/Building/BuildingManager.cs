using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

struct Assembly
{
	public string name;
	public List<PartInfo> parts;
	public bool didPrecomputations;
	public List<SerializableSubassembly> precomputedSubassemblies;
	// to add onto
}
struct PureVector3
{
	public float x, y, z;
}
struct PureQuaternion
{
	public float x, y, z, w;
}
struct PartInfo
{
	public string basePartName;
	public int id;
	public PureVector3 position;
	public PureQuaternion rotation;
	public PureVector3 scale;
}
struct SerializableSubassembly
{
	public List<int> partIds;
}

public class BuildingManager : MonoBehaviour
{
	#region singleton
	private static BuildingManager _instance;
	public static BuildingManager Instance { get { return _instance; } }
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

	public Transform mainPartsContainer;
	public List<BasePart> BaseParts;
	public List<Part> Parts;
	List<Part> lastParts;
	public SelectionManager SelectionManager;
	public TransformTools TransformTools;
	public Assembler Assembler;
	public static Dictionary<string, BasePart> AllParts = new();
	public GameObject templatePart;

	void Start()
	{

	}

	void Update()
	{
		Parts = FindObjectsOfType<Part>().OrderBy(part => part.ID).ToList(); // sort by id to make sure current stays in the same order

		foreach(Part part in Parts)
		{
			part.Selected = SelectionManager.selection.Contains(part.transform);
		}

		if (lastParts != Parts)
		{
			PartsUpdated();
		}
		lastParts = Parts;
	}

	public void PartsUpdated()
	{
		UpdateIds();
	}

	void UpdateIds()
	{
		int id = 0;
		foreach (Part part in Parts)
		{
			part.ID = id++;
		}
	}

	public void ResetParts()
	{
		foreach (Part part in Parts)
		{
			Destroy(part.gameObject);
		}
		Parts.Clear();
	}

	public Part newPart(string basePartName)
	{
		int bpIndex = BaseParts.FindIndex(bp => bp.partName == basePartName);
		if (bpIndex == -1)
			throw new($"basepart \"{basePartName}\" doesn't exist");

		BasePart bp = BaseParts[bpIndex];
		GameObject newPart = Instantiate(bp.prefab, mainPartsContainer);
		Part part = newPart.GetComponent<Part>();
		part.basePart = bp;

		return part;
	}
}
