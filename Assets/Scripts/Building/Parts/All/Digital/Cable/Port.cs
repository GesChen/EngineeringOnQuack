using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Port : SnapTarget {
	public string Alias;
	public List<Part_CableConnection> Connectors = new();

	public Part[] GetOtherParts() {
		return Connectors.Select(c => c.ConnectedPart).ToArray();
	}

	// implement custom build snapping behaviour here later
	public override void OnSnappedTo() {
		// ok so were just gonna handle this in cc nonstaticpart 
		// on simulation start 
		// its better that way cuz i dont have to 
	}
}