using System;
using System.Collections;
using UnityEngine;

public class Part_Cable : NonStaticPart {
	public override string PartName => "Cable";

	public Part_CableConnection connectionA;
	public Part_CableConnection connectionB;
	public LineRenderer Line;

	public void Update() {
		Line.SetPositions(new[] {
			connectionA.transform.position,
			connectionB.transform.position
		});
	}

	public override void OnPartCreation() {
		var ccA = BuildingManager.Instance.MakeNewPart("cc", true, true);
		var ccB = BuildingManager.Instance.MakeNewPart("cc", true, true);

		connectionA = ccA.GetComponent<Part_CableConnection>();
		connectionB = ccB.GetComponent<Part_CableConnection>();

		connectionA.Cable = this;
		connectionB.Cable = this;

		connectionA.CCID = UnityEngine.Random.value.GetHashCode();
		connectionB.CCID = UnityEngine.Random.value.GetHashCode();

		ccA.transform.position = transform.position;
		ccB.transform.position = transform.position;
	}

	public override void FinalizeInstantiation(GameObject instantiatedPart) {
		instantiatedPart.GetComponent<Part_Cable>().ReconnectToCCs();
	}

	// connect to simulation ccs
	public void ReconnectToCCs() {
		var allccs = BuildingManager.Instance.SimulationContainer.GetComponentsInChildren<Part_CableConnection>();
		if (!allccs.TryFind(cc => cc.CCID == connectionA.CCID, out var simA))
			throw new("couldn't find new connection A on sim start");
		if (!allccs.TryFind(cc => cc.CCID == connectionB.CCID, out var simB))
			throw new("couldn't find new connection B on sim start");

		connectionA = simA;
		connectionB = simB;

		connectionA.Cable = this;
		connectionB.Cable = this;
	}

	public Part_CableConnection OtherCC(Part_CableConnection cc) {
		if (cc == connectionA) return connectionB;
		if (cc == connectionB) return connectionA;
		throw new("requested cc wasn't either A or B");
	}

	public override string ToString() {
		if (connectionA == null) return "Cable, cc A disconnected";
		if (connectionB == null) return "Cable, cc B disconnected";

		return $"Cable connecting {connectionA.Port.MainPart.GetType().Name} -- {connectionB.Port.MainPart.GetType().Name}";
	}

	public override void HandleCommand(string command, object[] args) {
		Debug.LogError(UnknownCommand(command));
	}
}