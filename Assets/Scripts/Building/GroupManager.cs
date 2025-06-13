using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GroupManager : Singleton<GroupManager> {
	public static List<PartGroup> Groups = new();

	public void GroupCurrentSelection() {
		Group(SelectionManager.Instance.PartSelection);
	}

	public void Group(Part[] parts) {
		PartGroup group = new(parts.ToList());
		Groups.Add(group);
	}

	public bool IsPartInGroup(Part part, out PartGroup group) {
		foreach (var partGroup in Groups) {
			if (partGroup.Parts.Contains(part)) {
				group = partGroup;
				return true;
			}
		}

		group = null;
		return false;
	}
}