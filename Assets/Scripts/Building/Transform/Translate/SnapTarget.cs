using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// types that want special snapping behaviour should derive from this
public class SnapTarget : MonoBehaviour {
	// not sure if there might be actual code here later
	
	public virtual void OnSnappedTo() {

	}
}
