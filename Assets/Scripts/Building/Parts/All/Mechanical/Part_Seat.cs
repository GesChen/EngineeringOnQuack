using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part_Seat : NonStaticPart {
	public override string PartName => "Seat";

	public Transform SitTarget;

	public class CPart : Construct.Part {

	}

	public override void FinalizeCPartConversion(ref Construct.Part CPart) {
		var cpa = new CPart();

		cpa.CopyMembers(CPart);
		CPart = cpa;
	}

	// might actually add one later potentialy.. 
	// maybe for multiplayer or other options
	public override T_Data GetInternalLanguageDataObject() => Errors.BadCode();
}