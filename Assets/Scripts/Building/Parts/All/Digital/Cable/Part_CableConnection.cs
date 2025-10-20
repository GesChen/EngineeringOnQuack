using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Part_CableConnection : NonStaticPart {
	public override string PartName => "Cable Connection";

	public int CCID = -1; // needed for cable to reconnect on simulation start

	public Part_Cable Cable;
	public Part Part;

	public Part_CableConnection(Part_Cable cable, Part part) {
		Cable = cable;
		Part = part;
	}

	public Part ConnectedPart => Cable.OtherCC(this).Part; // may change this

	// no extra processing needed
	public override void FinalizeInstantiation(GameObject instantiatedPart) {
		// see if this is very close to any port
		// if so connect to it

		var ports = BuildingManager.Instance.SimulationContainer.GetComponentsInChildren<Port>();

		foreach (var port in ports) {
			float dist = (transform.position - port.transform.position).magnitude;
			if (dist < Config.Building.CCConnectionDistance) {
				// connect up
				port.Connectors.Add(this);

				Part = port.GetComponentInParent<Part>();
			}
		}
	}

	public override void HandleCommand(string command, object[] parameters) {
		throw UnknownCommand(command);
	}
}