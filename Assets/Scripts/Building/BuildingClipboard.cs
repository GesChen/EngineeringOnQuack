using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingClipboard {
	public class PartRepr {
		public BasePart bp;
		public Vector3 position;
		public Quaternion rotation;
		public Vector3 scale;
	}

	public PartRepr[] Clipboard;

	public void Copy() {
		var parts = SelectionManager.Instance.selection;

		Clipboard = new PartRepr[parts.Count];

		for (int i = 0; i < parts.Count; i++) {
			var part = parts[i].GetComponent<Part>();
			Clipboard[i] = PartToRepr(part);
		}
	}

	PartRepr PartToRepr(Part part) {
		PartRepr repr = new(){
			bp = part.basePart,
			position = part.transform.position,
			rotation = part.transform.rotation,
			scale = part.transform.localScale
		};

		return repr;
	}

	public Part[] Paste(Vector3 position, bool selectNew) {
		if (Clipboard == null || Clipboard.Length == 0) return null; // empty

		// get middle
		Vector3 center = Vector3.zero;
		foreach (var p in Clipboard) center += p.position;
		center /= Clipboard.Length;

		Vector3 offset = position - center;

		// generate
		Transform[] newTransforms = new Transform[Clipboard.Length];
		Part[] newParts = new Part[Clipboard.Length];

		for (int i = 0; i < Clipboard.Length; i++) {
			var origPart = Clipboard[i];
			var newPart = BuildingManager.Instance.GeneratePart(origPart.bp.partName);
			newParts[i] = newPart;

			var transform = newPart.transform;
			transform.SetPositionAndRotation(origPart.position + offset, origPart.rotation);
			transform.localScale = origPart.scale;

			newTransforms[i] = transform;
		}

		// select
		if (selectNew)
			SelectionManager.Instance.Select(newTransforms);

		return newParts;
	}
}