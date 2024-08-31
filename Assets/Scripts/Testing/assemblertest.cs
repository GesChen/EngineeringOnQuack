using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class assemblertest : MonoBehaviour
{
	public Assembler assembler;
	public Saver saver;
	public Loader loader;
	void Update()
	{
		if (Input.GetKeyDown("e"))
		{
			saver.SaveCurrentBuild("test", true);
			saver.BuildingManager.ResetParts();
			loader.LoadFromFile("test");

			//assembler.Assemble(BuildingManager.Instance);
		}
	}
}
