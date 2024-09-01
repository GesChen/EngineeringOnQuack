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

	public void StartSimulating()
	{
		Assembler.Instance.Assemble(out List<Assembler.Subassembly>  computed);
		CalculateTotalMasses(computed);
	}

	public void StopSimulating()
	{
		foreach (var asm in assembledSubassemblies)
		{
			foreach (Transform obj in asm.parts)
			{
				Destroy(obj.gameObject);
			}

			Destroy(asm.parentContainer.gameObject);
		}
	}
	
	void CalculateTotalMasses(List<Assembler.Subassembly> subassemblies)
	{
		for (int i = 0; i < subassemblies.Count; i++)
		{
			assembledSubassemblies[i].parentContainer.GetComponent<Rigidbody>().mass = totalMass(subassemblies[i]);
		}
	}

	float totalMass(Assembler.Subassembly asm)
	{
		float total = 0;
		foreach (Part part in asm.parts)
		{
			total += massOfObject(part);
		}
		return total;
	}

	float massOfObject(Part part)
	{
		float total = 0;
		// iterate through tris
		for (int i = 0; i < part.basePart.allTriPositions.Length; i += 3)
		{
			Vector3 p1 = part.basePart.allTriPositions[i + 0];
			Vector3 p2 = part.basePart.allTriPositions[i + 1];
			Vector3 p3 = part.basePart.allTriPositions[i + 2];

			total += Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6f;
		}

		return total;
	}
}
