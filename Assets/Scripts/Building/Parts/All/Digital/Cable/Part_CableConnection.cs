using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Part_CableConnection : NonStaticPart {
	public override string PartName => "Cable Connection";

	public int CCID = -1; // needed for cable to reconnect on simulation start

	public Part_Cable Cable;
	public Port Port;

	public Part_CableConnection(Part_Cable cable, Port port) {
		Cable = cable;
		Port = port;
	}

	public Part ConnectedPart => Cable.OtherCC(this).Port.MainPart; // may change this

	// no extra processing needed
	public override void FinalizeInstantiation(GameObject instantiatedPart) {
		// see if this is very close to any port
		// if so connect to it

		var ports = BuildingManager.Instance.SimulationContainer.GetComponentsInChildren<Port>();

		foreach (var port in ports) {
			float dist = (transform.position - port.transform.position).magnitude;
			if (dist < Config.Building.CCConnectionDistance) {
				// connect up
				var newCC = instantiatedPart.GetComponent<Part_CableConnection>();

				port.Connectors.Add(newCC); 
				newCC.Port = port;
			}
		}

		// Cable moves its own reference in the cable field over to new
	}

	public override void HandleCommand(string command, object[] args) {
		Debug.LogError(UnknownCommand(command));
	}
}