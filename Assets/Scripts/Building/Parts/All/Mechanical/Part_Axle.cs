using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// temporary class to label an axle as an axle until i think of a better solution
/// currently maybe a custom tag script so i dont have to create extra tags in the project
/// and can just set them as strings idk
/// </summary>
public class Part_Axle : NonStaticPart {
	public override string PartName => "Axle";

	public Transform endA; // scales well i guess? fast way to keep track of this stuff i guess
	public Transform endB;

	public class CPart : Construct.Part {
	}

	public override void FinalizeCPartConversion(ref Construct.Part CPart) {
		var axle = new CPart();

		axle.CopyMembers(CPart);

		CPart = axle;
	}

	public override T_Data GetInternalLanguageDataObject() => Errors.BadCode();
}