using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class assemblertest : MonoBehaviour
{
	public Assembler assembler;
	public SaveLoad saveload;
	void Update()
	{
		if (Input.GetKeyDown("e"))
		{
			saveload.SaveCurrentBuild("test", true);
			saveload.BuildingManager.ResetParts();
			saveload.LoadFromFile("test");

			//assembler.Assemble(BuildingManager.Instance);
		}
	}
}
