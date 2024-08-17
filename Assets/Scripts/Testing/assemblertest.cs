using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class assemblertest : MonoBehaviour
{
	public Assembler assembler;
	void Update()
	{
		if (Input.GetKeyDown("e"))
		{
			assembler.Assemble(BuildingManager.Instance);
		}
	}
}
