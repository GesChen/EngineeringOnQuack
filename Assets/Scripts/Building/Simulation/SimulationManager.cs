using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
	#region singleton
	private static SimulationManager _instance;
	public static SimulationManager Instance { get { return _instance; } }
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

	public List<Assembler.AssembledSubassembly> assembledSubassemblies = new();
	[HideInNormalInspector] public List<Part> partsBeforeStart;

	public void StartSimulating()
	{
		partsBeforeStart = BuildingManager.Instance.Parts;
		Assembler.Instance.Assemble();
	}

	public void StopSimulating()
	{
		BuildingManager.Instance.Parts = partsBeforeStart;

		foreach (var asm in assembledSubassemblies)
		{
			foreach (Transform obj in asm.parts)
			{
				Destroy(obj.gameObject);
			}

			Destroy(asm.parentContainer.gameObject);
		}
	}
}
