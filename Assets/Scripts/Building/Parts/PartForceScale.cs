using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartForceScale : MonoBehaviour {
	public bool forceX;
	public bool forceY;
	public bool forceZ;

	public Vector3 forceDims;

	void LateUpdate() {
		Vector3 ls = transform.localScale;
		Vector3 ps = transform.parent.localScale;

		transform.localScale = new(
			forceX ? forceDims.x / ps.x : ls.x,
			forceY ? forceDims.y / ps.y : ls.y,
			forceZ ? forceDims.z / ps.z : ls.z
			);
	}
}