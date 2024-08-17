using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
	public List<Part> Parts;
	public SelectionManager SelectionManager;
	public TransformTools transformTools;

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		Parts = FindObjectsOfType<Part>().ToList();

		foreach(Part part in Parts)
		{
			part.Selected = SelectionManager.selection.Contains(part.transform);
		}
	}
}
