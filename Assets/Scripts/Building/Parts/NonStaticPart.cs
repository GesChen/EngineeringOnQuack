using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * nsp file layout
 * 1. partname => ""
 * 2. all custom code for the part
 * 3. language things
 * - including internal methods
 * 4. custom cpart/saving then overrides
 */
public abstract class NonStaticPart : MonoBehaviour {
	[HideInNormalInspector] public Part Part;
	[HideInNormalInspector] public int CreationID;

	protected bool IsAssembled;
	public void BecomeAssembled() { IsAssembled = true; }

	public Port[] Ports;

	// public List<Part> LinkedParts;

	public abstract string PartName { get; }

	// dont forget to call base.awake in derived classes
	// think about how this works with creation during assembly copying
	protected void Awake() {
		Part = GetComponent<Part>();
	}

	// throw unknowncommand at the end of this function
	public abstract T_Data GetInternalLanguageDataObject();

	/// <summary>
	/// During editing, inside MakeNewPart
	/// </summary>
	public virtual void OnPartCreation() { } // this codebase gets worse by the minute

	/// <summary>
	/// During editing
	/// </summary>
	public virtual void OnPartDeletion() { }

	/// <summary>
	/// <para>While editing, clipboard and saving</para>
	/// <para>Method that converts an CPa object into a type deriving from CPa for more specificity in 
	/// serialization/saving and vice versa.</para>
	/// <para>CPa param already set up beforehand, only need to copy over the old values into the subtype and add more members
	/// and reassign the cpart param</para>
	/// <para>Implementation can be left blank if nothing needs to be done</para>
	/// </summary>
	/// <param name="CPart">Part to be reassigned into the more specific subclass</param>
	/// <example>
	/// <code>
	/// public class CPart : Construct.Part {
	/// 	public override NonStaticPart NSP => base.NSP;
	/// 	public NonStaticPart _nsp;
	/// 	public CPart(NonStaticPart main) { _nsp = main; }
	/// 
	/// 	public Construct.SVector3 Color;
	/// 	public float Intensity;
	/// }
	/// 
	/// public override void FinalizeCPartConversion(ref Construct.Part CPart) {
	/// 	var led = new CPart(this);
	/// 
	/// 	led.CopyMembers(CPart);
	/// 	led.Color = Color;
	/// 	led.Intensity = Intensity;
	/// 
	/// 	CPart = led;
	/// }
	/// </code>
	/// </example>
	public abstract void FinalizeCPartConversion(ref Construct.Part CPart);
	// made it abstract 11-29 for construct reassembly purposes all nsps
	// need distinct classes from cpart

	/// <summary>
	/// <para>While Editing, clipboard and saving</para>
	/// <para>Not necessary for public and serializable variables</para>
	/// Method that completes reconstruction of an CPa back into a part. The rest of the part has 
	/// already been reconstructed, this method just sets the other members for the NSP component
	/// <para>Implementation can be left blank if nothing needs to be done</para>
	/// </summary>
	/// <param name="reconstructed">The in progress reconstructing assembly. May delete if unneeded</param>
	/// <param name="originalCPart">The original CPart part was created from</param>
	/// <param name="unfinishedPart">The unfinished part object</param>
	/// <param name="component">The NSP component (probably) to setup</param>
	public virtual void FinalizeCPartReconstruction(
		Construct.Part originalCPart,
		Part unfinishedPart, Assembly unfinishedAssembly) { }

	/// <summary>
	/// used with the buildingclipboard to remap id references in things
	/// </summary>
	/// <param name="Mappings">old id -> new id</param>
	/// <param name="PartPool">list of new parts to look through to rebind</param>
	public virtual void RebindReferences(Dictionary<int, int> Mappings, Part[] PartPool) { }
}