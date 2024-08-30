using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part : MonoBehaviour
{
	public int ID;
	public BasePart basePart;
	public bool Selected;

	void Update()
	{
		if (Selected)
		{
			gameObject.layer = LayerMask.NameToLayer("Selected");
		}
		else
		{
			gameObject.layer = LayerMask.NameToLayer("Part");
		}
	}
}
