using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NonStaticPart : MonoBehaviour {

	public abstract void OnStopSimulating();
	
	public abstract void OnStartSimulating();

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
public abstract void FinalizeSPartConversion(ref Assembly.SPart SPart);
	
	/// <summary>
	/// Method that completes reconstruction of an SP back into a part. The rest of the part has 
	/// already been reconstructed, this method just sets the other members for the NSP component
	/// <para>Implementation can be left blank if nothing needs to be done</para>
	/// </summary>
	/// <param name="reconstructed">The in progress reconstructing assembly. May delete if unneeded</param>
	/// <param name="originalSPart">The original SPart part was created from</param>
	/// <param name="unfinishedPart">The unfinished part object</param>
	/// <param name="component">The NSP component (probably) to setup</param>
	public abstract void FinalizeSPartReconstruction(
		Assembly.SPart originalSPart,
		Part unfinishedPart);

	internal void FinalizeSPartReconstruction(object reconstructed, Assembly.SPart origPart, Part newPart) {
		throw new NotImplementedException();
	}
}