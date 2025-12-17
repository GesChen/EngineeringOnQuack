using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Part_CableConnection : NonStaticPart {
	public override string PartName => "Cable Connection";

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

	// no extra processing needed

	public class CPart : Construct.Part {
		public override void FinalizeInstantiation(GameObject instantiatedPart, GameObject creation) {
			// see if this is very close to any port
			// if so connect to it
			var newCC = instantiatedPart.GetComponent<Part_CableConnection>();

			var ports = creation.GetComponentsInChildren<Port>();

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
		var cpa = new CPart();
		cpa.CopyMembers(CPart);
		CPart = cpa;
	}

	public override void RebindReferences(Dictionary<int, int> Mappings, Part[] PartPool) {
		Cable = PartPool.First(p => p.ID == Mappings[Cable.Part.ID]).GetComponent<Part_Cable>();
	}
}