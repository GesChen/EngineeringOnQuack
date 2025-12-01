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

	public class CPart : Construct.Part {
		public int CCID;

		public override void FinalizeInstantiation(GameObject instantiatedPart) {
			// see if this is very close to any port
			// if so connect to it
			var newCC = instantiatedPart.GetComponent<Part_CableConnection>();
			newCC.CCID = CCID; 

			var ports = GameManager.Instance.CreationsContainer.GetComponentsInChildren<Port>();

			foreach (var port in ports) {
				if (port.SnapTarget.CheckSnap(position)) {
					// connect up

					port.Connector = newCC;
					newCC.Port = port;
					break;
				}
			}

			// Cable moves its own reference in the cable field over to new
		}
	}

	public override void FinalizeCPartConversion(ref Construct.Part CPart) {
		var cc = new CPart();

		cc.CopyMembers(CPart);
		cc.CCID = CCID;

		CPart = cc;
	}

	public override void FinalizeCPartReconstruction(Construct.Part originalCPart, Part unfinishedPart, Assembly unfinishedAssembly) {
		var cc = unfinishedPart.GetComponent<Part_CableConnection>();
		var part = (CPart)originalCPart;

		cc.CCID = part.CCID;
	}
}