using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cable : MonoBehaviour
{
	public CableConnection connectionA;
	public CableConnection connectionB;

	public CableConnection otherCC(CableConnection cc) {
		if (cc == connectionA) return connectionB;
		if (cc == connectionB) return connectionA;
		throw new("requested cc wasn't either A or B");
	}
}