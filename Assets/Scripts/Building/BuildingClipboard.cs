using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Assembly;

public class BuildingClipboard {
	public struct Clip {
		public SPart[] Parts;
		public SGroup[] Groups;
	}
	public Clip Clipboard;
	public List<Clip> History = new();

	public void Copy() {
		var clip = new Clip();

		var parts = SelectionManager.Instance.PartSelection;

		clip.Parts = parts.Select(p => ConvertPartToSPart(p)).ToArray();

		/*
		// below code is hella unreadable i wrote it while half asleep
		// listening to fuckin #3 by aphex twin
		// the letters on the screen are popping out at me

		// bro what the fuck is this 8-16-25 
		// how tf am i meant to fix this

		// its meant to find whichever groups are selected wtf

		// turn any selected grouped parts into groupreprs
		
		// chatgpt rewrote that o(n^2) nightmare 
		// didnt even know distinct existed
		*/

		clip.Groups = parts
			.Where(p => p.Group != null)
			.Select(p => p.Group)
			.Distinct() // gets a list of all groups from selected
			.Select(g => (SGroup)g).ToArray();

		History.Add(Clipboard);
		if (History.Count >= Config.Building.ClipboardHistorySize)
			History.RemoveAt(0);

		Clipboard = clip;
	}

	public Part[] Paste(Vector3 position, bool selectNew) {
		if (Clipboard.Parts == null || Clipboard.Parts.Length == 0) return null; // empty

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
		foreach (var p in Clipboard.Parts) center += p.position;
		center /= Clipboard.Parts.Length;

		Vector3 offset = position - center;

		// generate
		Transform[] newTransforms = new Transform[Clipboard.Parts.Length];
		Part[] newParts = new Part[Clipboard.Parts.Length];

		for (int i = 0; i < Clipboard.Parts.Length; i++) {
			var origPart = Clipboard.Parts[i];
			var newPart = BuildingManager.Instance.GeneratePart(origPart.basePartID);
			newParts[i] = newPart;

			var transform = newPart.transform;
			transform.SetPositionAndRotation(origPart.position + offset, origPart.rotation);
			transform.localScale = origPart.scale;

			newTransforms[i] = transform;
			
			if (newPart.IsNonStaticPart(out var nsp)) {
				nsp.FinalizeSPartReconstruction(origPart, newPart);
			}
		}



		return (newParts, newTransforms);
	}

	void GroupPastedParts(Part[] parts) {
		// assuming all parts are in the same order as in the clipboard
		if (Clipboard.Groups.Length == 0) return;
		var partDict = parts.ToDictionary(p => p.ID); // O(M)

		foreach (var gr in Clipboard.Groups) {
			var groupParts = gr.PartIDs.Select(i => partDict[i]).ToList();

			PartGroup realGroup = new(groupParts);
			BuildingManager.Instance.Assembly.Groups.Add(realGroup);

			foreach (var part in groupParts) {
				part.Group = realGroup;
			}
		}
	}
}