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
		var list = parts.ToList();
		PartGroup group = new(list);

		list.ForEach(p => p.Group = group);

		Groups.Add(group);
	}

	private bool IsPartInGroup(Part part, out PartGroup group) {
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