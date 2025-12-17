using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

// types that want special snapping behaviour should derive from this
public class SnapTarget : MonoBehaviour {
	// not sure if there might be actual code here later
	
	public void OnSnappedTo() {

	}

	public bool CheckSnap(Vector3 otherPos) {
		DebugExtra.DrawPoint(transform.position, Color.red);
		DebugExtra.DrawPoint(otherPos, Color.blue);

		float dist = (transform.position - otherPos).sqrMagnitude;
		return dist < Config.Building.CCConnectionDistance * Config.Building.CCConnectionDistance;
	}
}