using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class Part_Cable : NonStaticPart {
	public override string PartName => "Cable";

	public Part_CableConnection connectionA;
	public Part_CableConnection connectionB;
	public LineRenderer Line;
	[HideInInspector] public bool SetUp = true;

	public void Update() {
		if (!SetUp) return;

		Line.SetPositions(new[] {
			connectionA.transform.position,
			connectionB.transform.position
		});
	}

	public override void OnPartDeletion() {
		if (BuildingManager.Instance.Assembly.Parts.Contains(connectionA.Part)) 
			BuildingManager.Instance.DeletePart(connectionA.Part, false);

		if (BuildingManager.Instance.Assembly.Parts.Contains(connectionB.Part)) 
			BuildingManager.Instance.DeletePart(connectionB.Part, false);
	}

	public override T_Data GetInternalLanguageDataObject() => Errors.BadCode();

	public override void OnPartCreation() {
		if (SaveLoadManager.Loading) return; // dont do this for loading

		var ccA = BuildingManager.Instance.MakeNewPart("cc", true, true);
		var ccB = BuildingManager.Instance.MakeNewPart("cc", true, true);

		connectionA = ccA.GetComponent<Part_CableConnection>();
		connectionB = ccB.GetComponent<Part_CableConnection>();

		connectionA.Cable = this;
		connectionB.Cable = this;

		ccA.transform.position = transform.position;
		ccB.transform.position = transform.position;

		SelectionManager.Instance.RemoveSelection(transform);
	}

	// connect to simulation ccs
	public void ReconnectToCCs(int aid, int bid, GameObject creation) {
		// might redo this
		var allccs = creation.GetComponentsInChildren<Part_CableConnection>();
		if (!allccs.TryFind(cc => cc.Part.ID == aid, out var simA))
			throw new("couldn't find new connection A on sim start");
		if (!allccs.TryFind(cc => cc.Part.ID == bid, out var simB))
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

		return $"Cable connecting {connectionA.Port.MainNSP.GetType().Name} -- {connectionB.Port.MainNSP.GetType().Name}";
	}

	public class CPart : Construct.Part {
		public int AID;
		public int BID;

		public override void FinalizeInstantiation(GameObject instantiatedPart, GameObject creation) {
			var cable = instantiatedPart.GetComponent<Part_Cable>();

			cable.ReconnectToCCs(AID, BID, creation);
		}
	}
	public override void FinalizeCPartConversion(ref Construct.Part CPart) {
		var cable = new CPart();

		cable.CopyMembers(CPart);
		cable.AID = connectionA.Part.ID;
		cable.BID = connectionB.Part.ID;

		CPart = cable;
	}

	public override void FinalizeCPartReconstruction(Construct.Part originalCPart, Part unfinishedPart, Assembly unfinishedAssembly) {
		var newCable = unfinishedPart.GetComponent<Part_Cable>();
		//newCable.StartCoroutine(DelaySetup(originalCPart, unfinishedPart, unfinishedAssembly));

		var part = (CPart)originalCPart;
		var cA = unfinishedAssembly.Parts.First(p =>
			p.GetNSP<NonStaticPart>() is Part_CableConnection cc && cc.Part.ID == part.AID
		).GetComponent<Part_CableConnection>();

		var cB = unfinishedAssembly.Parts.First(p =>
			p.GetNSP<NonStaticPart>() is Part_CableConnection cc && cc.Part.ID == part.BID
		).GetComponent<Part_CableConnection>();

		newCable.connectionA = cA;
		newCable.connectionB = cB;

		cA.Cable = newCable;
		cB.Cable = newCable;
	}

	IEnumerator DelaySetup(Construct.Part originalCPart, Part unfinishedPart, Assembly unfinishedAssembly) {
		var newCable = unfinishedPart.GetComponent<Part_Cable>();
		newCable.SetUp = false;
		yield return null;
		newCable.SetUp = true;

		var part = (CPart)originalCPart;
		var cA = unfinishedAssembly.Parts.First(p => 
			p.GetNSP<NonStaticPart>() is Part_CableConnection cc && cc.Part.ID == part.AID
		).GetComponent<Part_CableConnection>();

		var cB = unfinishedAssembly.Parts.First(p => 
			p.GetNSP<NonStaticPart>() is Part_CableConnection cc && cc.Part.ID == part.BID
		).GetComponent<Part_CableConnection>();


		newCable.connectionA = cA;
		newCable.connectionB = cB;
	}

	public override void RebindReferences(Dictionary<int, int> Mappings, Part[] PartPool) {
		connectionA = PartPool.First(p => p.ID == Mappings[connectionA.Part.ID]).GetComponent<Part_CableConnection>();
		connectionB = PartPool.First(p => p.ID == Mappings[connectionB.Part.ID]).GetComponent<Part_CableConnection>();
	}
}