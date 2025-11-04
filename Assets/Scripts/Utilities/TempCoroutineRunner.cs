using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// self explanatory
public class TempCoroutineRunner : MonoBehaviour {
	/// <summary>
	/// destroys itself upon coroutine completion
	/// </summary>
	public new void StartCoroutine(IEnumerator routine) {
		base.StartCoroutine(Run(routine));
	}

	IEnumerator Run(IEnumerator routine) {
		yield return base.StartCoroutine(routine);
		Destroy(this);
	}
}