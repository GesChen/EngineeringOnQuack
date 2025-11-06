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
 * 4. custom spart/saving then overrides
 */
public abstract class NonStaticPart : MonoBehaviour {
	public Part Part;

	public abstract string PartName { get; }

	// dont forget to call base.awake in derived classes
	// think about how this works with creation during assembly copying
	protected void Awake() {
		Part = GetComponent<Part>();
	}

	// throw unknowncommand at the end of this function
	public abstract T_Data GetInternalLanguageDataObject();

	public virtual void OnStopSimulating() { }
	public virtual void OnStartSimulating() { }
	public virtual void OnPartCreation() { } // this codebase gets worse by the minute

	#region Serialization
	/// <summary>
	/// <para>Method that converts an SP object into a type deriving from SP for more specificity in 
	/// serialization/saving and vice versa.</para>
	/// <para>SP param already set up beforehand, only need to copy over the old values into the subtype and add more members
	/// and reassign the spart param</para>
	/// <para>Implementation can be left blank if nothing needs to be done</para>
	/// </summary>
	/// <param name="SPart">Part to be reassigned into the more specific subclass</param>
	/// <example>
	/// <code>
	/// SPart = new SPart_CPU {
	/// 	basePartID = SPart.basePartID,
	/// 	id = SPart.id,
	/// 	position = SPart.position,
	/// 	rotation = SPart.rotation,
	/// 	scale = SPart.scale,
	/// 	color = SPart.color,
	/// 	compositionID = SPart.compositionID,
	/// };
	/// </code>
	/// </example>
	public virtual void FinalizeSPartConversion(ref Assembly.SPart SPart) { }
	
	/// <summary>
	/// Method that completes reconstruction of an SP back into a part. The rest of the part has 
	/// already been reconstructed, this method just sets the other members for the NSP component
	/// <para>Implementation can be left blank if nothing needs to be done</para>
	/// </summary>
	/// <param name="reconstructed">The in progress reconstructing assembly. May delete if unneeded</param>
	/// <param name="originalSPart">The original SPart part was created from</param>
	/// <param name="unfinishedPart">The unfinished part object</param>
	/// <param name="component">The NSP component (probably) to setup</param>
	public virtual void FinalizeSPartReconstruction(
		Assembly.SPart originalSPart,
		Part unfinishedPart, Assembly unfinishedAssembly) { }
	#endregion

	#region Assembly
	/// <summary>
	/// Called after all parts have been instantiated, no 1 frame wait needed
	/// The caller for this is still the original object from building. 
	/// Copy over private fields (its allowed?) and finish instantiating.
	/// Purpose: copy nonserializable and private fields that instantiate 
	/// cant get
	/// </summary>
	public virtual void FinalizeInstantiation(GameObject instantiatedPart) { }
	#endregion
}