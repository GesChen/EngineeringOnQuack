using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

// types that want special snapping behaviour should derive from this
public class SnapTarget : MonoBehaviour {
	// not sure if there might be actual code here later
	
	public void OnSnappedTo() {

	}

	public bool CheckSnap(Transform other) {
		float dist = (transform.position - other.position).sqrMagnitude;
		return dist < Config.Building.CCConnectionDistance * Config.Building.CCConnectionDistance;
	}
}