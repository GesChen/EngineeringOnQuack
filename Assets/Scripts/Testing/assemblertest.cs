using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class assemblertest : MonoBehaviour
{
	public Assembler assembler;
	public Saver saver;
	void Update()
	{
		if (Input.GetKeyDown("e"))
		{
			saver.SaveCurrentBuild("test", true);
			//assembler.Assemble(BuildingManager.Instance);
		}
	}
}
