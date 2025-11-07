using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Part_CableConnection : NonStaticPart {
	public override string PartName => "Cable Connection";

	// distinction between part.id 
	public int CCID = -1; // needed for cable to reconnect on simulation start
	// might just use part.id?? idk why not

	public Part_Cable Cable;
	public Port Port;

	public override T_Data GetInternalLanguageDataObject() => Errors.BadCode();

	public Part_CableConnection(Part_Cable cable, Port port) {
		Cable = cable;
		Port = port;
	}

	public Part ConnectedPart {
		get {
			var other = Cable.OtherCC(this);
			if (other == null) return null;

			return other.Port.MainNSP.Part; // may change this
		}
	}

	public void RandomizeID() {
		CCID = HF.UIDHashFunction();
	}

	// no extra processing needed
	public override void FinalizeInstantiation(GameObject instantiatedPart) {
		// see if this is very close to any port
		// if so connect to it

		var ports = BuildingManager.Instance.SimulationContainer.GetComponentsInChildren<Port>();

		foreach (var port in ports) {
			float dist = (transform.position - port.transform.position).sqrMagnitude;
			if (dist < Config.Building.CCConnectionDistance * Config.Building.CCConnectionDistance) {
				// connect up
				var newCC = instantiatedPart.GetComponent<Part_CableConnection>();

				port.Connector = newCC; 
				newCC.Port = port;
				break;
			}
		}

		// Cable moves its own reference in the cable field over to new
	}

	public class SPart_CC : Assembly.SPart {
		public int CCID;
	}

	public override void FinalizeSPartConversion(ref Assembly.SPart SPart) {
		var sp = new SPart_CC {
			CCID = CCID,

			basePartID = SPart.basePartID,
			id = SPart.id,
			position = SPart.position,
			rotation = SPart.rotation,
			scale = SPart.scale,
			color = SPart.color,
			compositionID = SPart.compositionID,
		};

		SPart = sp;
	}

	public override void FinalizeSPartReconstruction(Assembly.SPart originalSPart, Part unfinishedPart, Assembly unfinishedAssembly) {

		var cc = unfinishedPart.GetComponent<Part_CableConnection>();
		var part = (SPart_CC)originalSPart;

		cc.CCID = part.CCID;
	}
}