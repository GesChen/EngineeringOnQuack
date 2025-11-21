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

		var parts = SelectionManager.Instance.PartSelection.ToList();
		var moreParts = new List<Part>();

		#region cable handling
		// handle cables
		// always contain both ccs in the clipboard and also the main cable part
		HashSet<int> handledIDS = new();
		foreach (var part in parts) {
			if (handledIDS.Contains(part.ID)) continue;

			if (part.IsNonStaticPart(out var nsp)) {
				if (nsp is Part_CableConnection cc) {

					// select other cc if not already
					var othercc = cc.Cable.OtherCC(cc);
					if (!(handledIDS.Contains(othercc.Part.ID) ||
						parts.Contains(othercc.Part))) {
						moreParts.Add(othercc.Part);

						handledIDS.Add(part.ID);
						handledIDS.Add(othercc.Part.ID);
					}

					// select the main cable
					if (!parts.Contains(cc.Cable.Part)) {
						moreParts.Add(cc.Cable.Part);
					}
				}
			}
		}

		parts.AddRange(moreParts);

		clip.Parts = parts.Select(p => ConvertPartToSPart(p)).ToArray();

		// rerandomize the ccs in the board
		var cbCCs = clip.Parts
			.Select(p => p as Part_CableConnection.SPart_CC)
			.Where(cc => cc != null);
		var cbCables = clip.Parts
			.Select(p => p as Part_Cable.SPart_Cable)
			.Where(c => c != null);

		// randomize
		var CCIDs = cbCCs.Select(cc => cc.CCID);
		Dictionary<int, int> RemappedCCIDs = 
			CCIDs.ToDictionary(id => id, _ => HF.UIDHashFunction());

		// remap new ids
		foreach (var cc in cbCCs) cc.CCID = RemappedCCIDs[cc.CCID];
		foreach (var c in cbCables) {
			c.AID = RemappedCCIDs[c.AID];
			c.BID = RemappedCCIDs[c.BID];
		}
		#endregion

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

	public Part[] Paste(Vector3 position, bool selectNew, bool overrideSelection) {
		if (Clipboard.Parts == null || Clipboard.Parts.Length == 0) return null; // empty

		var (newParts, newTransforms) = GeneratePasteParts(position, selectNew, overrideSelection);

		GroupPastedParts(newParts);

		return newParts;
	}

	(Part[], Transform[]) GeneratePasteParts(Vector3 position, bool selectNew, bool overrideSelection) {

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
			var newPart = BuildingManager.Instance.MakeNewPart(origPart.basePartID, selectNew, !overrideSelection);
			newParts[i] = newPart;

			var transform = newPart.transform;
			transform.SetPositionAndRotation(origPart.position + offset, origPart.rotation);
			transform.localScale = origPart.scale;

			newTransforms[i] = transform;
			
			if (newPart.IsNonStaticPart(out var nsp)) {
				nsp.FinalizeSPartReconstruction(
					origPart,
					newPart,
					BuildingManager.Instance.Assembly);
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