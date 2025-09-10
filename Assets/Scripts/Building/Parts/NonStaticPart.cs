using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NonStaticPart : MonoBehaviour {
	public abstract void OnStopSimulating();
	public abstract void OnStartSimulating();
}