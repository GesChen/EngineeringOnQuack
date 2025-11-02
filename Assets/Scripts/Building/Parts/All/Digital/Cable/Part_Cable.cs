using System;
using System.Linq;
using System.Collections;
using UnityEngine;

public class Part_Cable : NonStaticPart {
	public override string PartName => "Cable";

	public Part_CableConnection connectionA;
	public Part_CableConnection connectionB;
	public LineRenderer Line;
	[HideInInspector] public bool SetUp;

	public void Update() {
		if (!SetUp) return;

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

		connectionA.RandomizeID();
		connectionB.RandomizeID();

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

	public class SPart_Cable : Assembly.SPart {
		public int AID;
		public int BID;
	}
	public override void FinalizeSPartConversion(ref Assembly.SPart SPart) {
		var sp = new SPart_Cable {
			AID = connectionA.CCID,
			BID = connectionB.CCID,

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
		var newCable = unfinishedPart.GetComponent<Part_Cable>();
		newCable.StartCoroutine(DelaySetup(originalSPart, unfinishedPart, unfinishedAssembly));
	}

	IEnumerator DelaySetup(Assembly.SPart originalSPart, Part unfinishedPart, Assembly unfinishedAssembly) {
		yield return null;

		var part = (SPart_Cable)originalSPart;
		var cA = unfinishedAssembly.Parts.First(p => {
			p.IsNonStaticPart(out var nsp);
			return nsp is Part_CableConnection cc && cc.CCID == part.AID;
		}).GetComponent<Part_CableConnection>();

		var cB = unfinishedAssembly.Parts.First(p => {
			p.IsNonStaticPart(out var nsp);
			return nsp is Part_CableConnection cc && cc.CCID == part.BID;
		}).GetComponent<Part_CableConnection>();

		var newCable = unfinishedPart.GetComponent<Part_Cable>();

		newCable.connectionA = cA;
		newCable.connectionB = cB;
	}
}