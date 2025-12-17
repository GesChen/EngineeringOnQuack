using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Port : MonoBehaviour {
	public NonStaticPart MainNSP; // name to be changed later
	public string Alias;
	public Part_CableConnection Connector;
	public SnapTarget SnapTarget;

	public Part OtherPart => Connector != null ? Connector.ConnectedPart : null;

	void Start() {
		BuildingManager.Instance.OnModified += UpdateSnapTarget;
	}

	// also called on scene cleanup and the instance would therefdore no longer extist
	private void OnDestroy() {
		if (BuildingManager.InstanceExists)
			BuildingManager.Instance.OnModified -= UpdateSnapTarget;
	}

	void UpdateSnapTarget() {
		// snap exclusivity

		foreach (var p in BuildingManager.Instance.Assembly.Parts) {
			if (SnapTarget.CheckSnap(p.transform.position)) {
				SnapTarget.enabled = false;
				return;
			}
		}
		SnapTarget.enabled = true;
	}
}