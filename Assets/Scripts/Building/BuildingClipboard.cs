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
	public class GroupRepr {
		public List<int> clipboardPartIndices = new();
	}

	public (PartRepr[] parts, GroupRepr[] groups) Clipboard;

	public void Copy() {
		var parts = SelectionManager.Instance.PartSelection;
		var partsWithIndices = parts.Select((p, i) => (p, i));

		Clipboard.parts = new PartRepr[parts.Length];

		for (int i = 0; i < parts.Length; i++) {
			Clipboard.parts[i] = PartToRepr(parts[i]);
		}

		// below code is hella unreadable i wrote it while half asleep
		// listening to fuckin #3 by aphex twin
		// the letters on the screen are popping out at me

		// turn any selected grouped parts into groupreprs
		List<PartGroup> allGroups = new();
		foreach (var part in parts) {
			if (part.Group != null && !allGroups.Contains(part.Group))
				allGroups.Add(part.Group);
		}

		// just assigns each group to its index in the list
		Dictionary<PartGroup, int> groupIndexes = 
			allGroups.Select((g, i) => (g, i)).ToDictionary(gi => gi.g, gi => gi.i);
		GroupRepr[] groupReprs = Enumerable.Repeat(new GroupRepr(), allGroups.Count).ToArray();

		for (int i = 0; i < parts.Length; i++) {
			Part part = parts[i];
			if (part.Group != null)
				groupReprs[groupIndexes[part.Group]].clipboardPartIndices.Add(i);
		}
		Clipboard.groups = groupReprs;
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
		if (Clipboard.parts == null || Clipboard.parts.Length == 0) return null; // empty

		var (newParts, newTransforms) = GeneratePasteParts(position, selectNew);

		GroupPastedParts(newParts);

		// select
		if (selectNew)
			SelectionManager.Instance.ManuallySelect(newTransforms);

		return newParts;
	}

	(Part[], Transform[]) GeneratePasteParts(Vector3 position, bool selectNew) {

		// get middle
		Vector3 center = Vector3.zero;
		foreach (var p in Clipboard.parts) center += p.position;
		center /= Clipboard.parts.Length;

		Vector3 offset = position - center;

		// generate
		Transform[] newTransforms = new Transform[Clipboard.parts.Length];
		Part[] newParts = new Part[Clipboard.parts.Length];

		for (int i = 0; i < Clipboard.parts.Length; i++) {
			var origPart = Clipboard.parts[i];
			var newPart = BuildingManager.Instance.GeneratePart(origPart.bp.partName);
			newParts[i] = newPart;

			var transform = newPart.transform;
			transform.SetPositionAndRotation(origPart.position + offset, origPart.rotation);
			transform.localScale = origPart.scale;

			newTransforms[i] = transform;
		}

		return (newParts, newTransforms);
	}

	void GroupPastedParts(Part[] parts) {
		// assuming all parts are in the same order as in the clipboard
		if (Clipboard.groups.Length == 0) return;

		foreach (var gr in Clipboard.groups) {
			var groupParts = gr.clipboardPartIndices.Select(i => parts[i]).ToList();

			PartGroup realGroup = new(groupParts);
			GroupManager.Instance.Groups.Add(realGroup);

			foreach (var part in groupParts) {
				part.Group = realGroup;
			}
		}
	}
}