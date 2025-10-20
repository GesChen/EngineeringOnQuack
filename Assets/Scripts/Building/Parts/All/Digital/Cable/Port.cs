using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Port : SnapTarget {
	public Part MainPart; // name to be changed later
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

	public Part[] GetParts() {
		var connected = Connectors.Select(c => c.ConnectedPart);

		return connected.Where(p => p != null).ToArray();
	}

	public void CallCommand(string command, object[] args) {
		foreach (var part in GetParts())
			part.HandleCommand(command, args);
	}
}