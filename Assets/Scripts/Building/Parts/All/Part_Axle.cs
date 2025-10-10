using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// temporary class to label an axle as an axle until i think of a better solution
/// currently maybe a custom tag script so i dont have to create extra tags in the project
/// and can just set them as strings idk
/// </summary>
public class Part_Axle : NonStaticPart {
	public Transform endA; // scales well i guess? fast way to keep track of this stuff i guess
	public Transform endB;

	public override void OnStopSimulating() {
		// nah
	}
	public override void OnStartSimulating() {
		// nothing for now
	}
	public override void FinalizeSPartConversion(ref Assembly.SPart SPart) { }
	public override void FinalizeSPartReconstruction(Assembly.SPart originalSPart, Part unfinishedPart) {
		
	}
	public override void FinalizeInstantiation(GameObject instantiatedPart) {
		// dont need
	}
}