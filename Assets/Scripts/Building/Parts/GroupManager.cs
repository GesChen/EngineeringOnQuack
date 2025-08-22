using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GroupManager : Singleton<GroupManager> {

	public void Subscribe() {
		RightClickMenus.OnGroup				+= GroupCurrentSelection;
		RightClickMenus.OnUnGroup			+= UngroupCurrentSelection;
		RightClickMenus.OnAddToGroup		+= AddToGroupCurrentSelection;
		RightClickMenus.OnRemoveFromGroup	+= RemoveFromGroupCurrentSelection;
		RightClickMenus.OnCombineGroups		+= CombineGroupsCurrentSelection;
	}

	private Assembly A => BuildingManager.Instance.Assembly;

	// dry these if needed
	void GroupCurrentSelection() {
		Part[] parts = SelectionManager.Instance.PartSelection;
		var list = parts.ToList();
		PartGroup group = new(list);

		list.ForEach(p => p.Group = group);

		A.Groups.Add(group);
	}

	void UngroupCurrentSelection() {
		Part[] parts = SelectionManager.Instance.PartSelection;
		List<PartGroup> groups = new();

		// get all groups
		foreach (var part in parts) {
			if (part.Group != null && !groups.Contains(part.Group))
				groups.Add(part.Group);
		}

		// ungroup them all
		foreach (var group in groups) {
			Ungroup(group);
		}
	}

	void Ungroup(PartGroup group) {
		foreach (var part in group.Parts) {
			part.Group = null;
		}

		A.Groups.Remove(group);
	}

	void AddToGroupCurrentSelection() {
		Part[] parts = SelectionManager.Instance.PartSelection;

		// expected to only be one group selected + other parts
		PartGroup group = parts.First(p => p.Group != null).Group;
		
		foreach (var part in parts) {
			if (part.Group != group) {
				group.Parts.Add(part);
				part.Group = group;
			}
		}
	}

	void RemoveFromGroupCurrentSelection() {
		Part[] parts = SelectionManager.Instance.PartSelection;

		// 1 group selected with certain parts from it
		PartGroup group = parts.First(p => p.Group != null).Group;

		foreach (var part in parts) {
			group.Parts.Remove(part);
			part.Group = null;
		}

		PreventSoloGroups();
	}

	void CombineGroupsCurrentSelection() {
		Part[] parts = SelectionManager.Instance.PartSelection;

		// can be multiple groups and non grouped parts
		List<PartGroup> groups = new();
		List<Part> nonGroupParts = new();
		foreach (var part in parts) {
			if (part.Group != null) {
				if (!groups.Contains(part.Group))
					groups.Add(part.Group);
			} else // no group
				nonGroupParts.Add(part);
		}

		// make unified group
		PartGroup unified = PartGroup.CombineGroups(groups);
		foreach (var part in nonGroupParts) { // add rest of parts
			unified.Parts.Add(part);
		}

		// replace old groups and set groups
		foreach (var oldGroup in groups)
			A.Groups.Remove(oldGroup);
		A.Groups.Add(unified);

		foreach (var part in unified.Parts) {
			part.Group = unified;
		}
	}

	void PreventSoloGroups() {
		List<int> toRemove = new();
		for (int i = 0; i < A.Groups.Count; i++) {
			PartGroup group = A.Groups[i];
			if (group.Parts.Count == 1) {
				group.Parts[0].Group = null;
				toRemove.Add(i);
			}
		}
		toRemove.Reverse(); // should be sorted low to high already from the loop

		foreach (var part in toRemove) {
			A.Groups.RemoveAt(part);
		}
	}
}