using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part_Motor : NonStaticPart {
	public override string PartName => "Motor";

	public Transform AxleEndTarget;
	static readonly float EndSnapDist = .01f;

	public float Strength;
	public double TargetVelocity;

	public Part_Axle Axle;

	public HingeJoint Joint;

	void FixedUpdate() {
		if (IsAssembled) { 
			// outers
			m_IDO.SetThisMember("strength", new Primitive.Number(Strength));
			m_IDO.SetThisMember("currentvelocity", new Primitive.Number(Joint.velocity));

			// inners
			TargetVelocity = (m_IDO.GetMember("targetvelocity") as Primitive.Number).Value;

			Joint.useMotor = true;
			Joint.motor = new() {
				force = Strength,
				freeSpin = false,
				targetVelocity = (float)TargetVelocity,
			};
		}
	}

	void LateUpdate() {
		if (!IsAssembled) {
			// ensure endB of axle stays on target and aligned to target forward

			// destroyed
			if (Axle == null) {
				BuildingManager.Instance.DeletePart(Part, false);
				return;
			}

			Axle.transform.position = 
				Axle.transform.position - Axle.endB.position 
				+ AxleEndTarget.position + EndSnapDist * AxleEndTarget.forward;

			Axle.transform.forward = AxleEndTarget.forward;
		}
	}

	public override void OnPartCreation() {
		if (BuildingManager.Instance.LoadingConstruct) return; // dont do this for loading
		var newAxlePart = BuildingManager.Instance.MakeNewPart("axle", true, true);

		newAxlePart.IsNonStaticPart(out var nsp);
		var axleNSP = (Part_Axle)nsp;

		// store axle
		Axle = axleNSP;
	}
	
	public override void OnPartDeletion() {
		// delete axle
		BuildingManager.Instance.DeletePart(Axle.Part, false);
	}

	public static Type Type_Motor = new(
		"Motor",
		new Dictionary<string, T_Data>() {
			{ "strength",			new Primitive.Number(0) },
			{ "currentvelocity",	new Primitive.Number(0) },
			{ "targetvelocity",		new Primitive.Number(0) },
		}
	);

	readonly T_Data m_IDO = new(Type_Motor);

	public override T_Data GetInternalLanguageDataObject() => m_IDO;

	public class CPart : Construct.Part {
		public float Strength;
		public int AxlePartID;

		public override void FinalizeInstantiation(GameObject instantiatedPart, GameObject creation) {
			var comp = instantiatedPart.GetComponent<Part_Motor>();
			comp.Strength = Strength;

			comp.Axle = creation.GetComponentsInChildren<Part_Axle>().First(a => a.Part.ID == AxlePartID);

			// setup the hinge joint now
			// assuming they didnt weld together
			// wild guess and hope that the parent is always the subassembly

			// actually dont do this is the axle is in the same subassembly 
			// aka shares the same parent cuz that js causes problems
			if (comp.Axle.transform.parent == instantiatedPart.transform.parent) return;

			var joint =  instantiatedPart.transform.parent.gameObject.AddComponent<HingeJoint>();
			joint.connectedBody = HF.GetOrMakeRigidBody(comp.Axle.Part.transform.parent.gameObject);
			joint.anchor = 
				comp.AxleEndTarget.position - 
				instantiatedPart.transform.parent.position;
			joint.axis = comp.AxleEndTarget.forward;

			comp.Joint = joint;
		}
	}

	public override void FinalizeCPartConversion(ref Construct.Part CPart) {
		var motor = new CPart();

		motor.CopyMembers(CPart);
		motor.Strength = Strength;
		motor.AxlePartID = Axle.Part.ID;

		CPart = motor;
	}

	public override void FinalizeCPartReconstruction(Construct.Part originalCPart, Part unfinishedPart, Assembly unfinishedAssembly) {
		var cpa = originalCPart as CPart;
		var newMotor = unfinishedPart.GetComponent<Part_Motor>();

		newMotor.Strength = cpa.Strength;
		newMotor.Axle = unfinishedAssembly.Parts.First(p => p.ID == cpa.AxlePartID).GetNSP<Part_Axle>();
	}

	public override void RebindReferences(Dictionary<int, int> Mappings, Part[] PartPool) {
		Axle = PartPool.First(p => p.ID == Mappings[Axle.Part.ID]).GetComponent<Part_Axle>();
	}
}