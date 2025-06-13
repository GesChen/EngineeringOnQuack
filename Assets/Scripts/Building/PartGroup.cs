using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// added "part" to make it less vague cuz group could mean anything
public class PartGroup {

	// might switch to array later if list features not needed
	public List<Part> Parts;

	public PartGroup(List<Part> parts) {
		Parts = parts;
	}

	public void AddPart(Part part) {
		Parts.Add(part);
	}

	public void RemovePart(Part part) {
		Parts.Remove(part);
	}

	public static PartGroup MergeGroups(List<PartGroup> groups) {
		List<Part> allParts = new();

		// might want to add duplicate checking but not sure how that would happen
		foreach (var group in groups) {
			allParts.AddRange(group.Parts);
		}

		PartGroup newGroup = new(allParts);

		return newGroup;
	}
}