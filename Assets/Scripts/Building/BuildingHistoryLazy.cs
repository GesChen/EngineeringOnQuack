using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// just keeping track of part history for now
// i really dont want to do outputs and whatnot
public class BuildingHistory {
	public class Change {
		public List<Construct.Part> Additions;
		public List<Construct.Part> Deletions;
		public List<(Construct.Part part, List<(string name, object newValue)>)> Modifications;
	}

	public List<Change> Changes;
	public Construct LastVersion;

	int undos = 0;

	public void Reset() {
		undos = 0;
		Changes.Clear();
		LastVersion = new() {
			Parts = new()
		};
	}

	// figure out the changes
	public void RecordChange() {
		// find additions
		var assem = BuildingManager.Instance.Assembly;
		var newParts = assem.Parts.Where(p => !LastVersion.Parts.Any(lp => lp.id == p.ID))
			.Select(np => Assembly.ConvertToCPart(np)).ToList();

		// find deletions
		var delParts = LastVersion.Parts.Where(p => !assem.Parts.Any(lp => lp.ID == p.id))
			.ToList();

		// find modifications
		// things where id is the same but the properties arent
		var modParts = assem.Parts.Where(p => LastVersion.Parts.Any(lp => lp.id == p.ID))
			.Select(p => Assembly.ConvertToCPart(p))
			.Select(p => 
				GetChangesForObject(
					LastVersion.Parts.FirstOrDefault(op => op.id == p.id), // matching cpa in last version
					p))
			.Where(changes => changes.Count > 0)
			.ToList();

		LastVersion = assem.ConvertToConstruct();
	}

	static List<(string Property, object Value)> GetChangesForObject(Construct.Part oldObj, Construct.Part newObj) {
		// base type: use provided default comparison
		if (newObj.GetType() == typeof(Construct.Part))
			return DefaultCompare(oldObj, newObj);

		// derived type: reflect all readable/writable properties
		var t = newObj.GetType();
		var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				 .Where(p => p.CanRead);

		var changes = new List<(string, object)>();

		foreach (var p in props) {
			object oldVal = p.GetValue(oldObj);
			object newVal = p.GetValue(newObj);

			if (!Equals(oldVal, newVal))
				changes.Add((p.Name, newVal));
		}

		return changes;
	}

	static List<(string Property, object Value)> DefaultCompare(Construct.Part oldObj, Construct.Part newObj) {
		var changes = new List<(string, object)>();

		if(oldObj.basePartID		!= newObj.basePartID)		changes.Add(("basePartID",		newObj.basePartID		));
		if(oldObj.id				!= newObj.id)				changes.Add(("id",				newObj.id				));
		if(oldObj.position			!= newObj.position)			changes.Add(("position",		newObj.position			));
		if(oldObj.rotation			!= newObj.rotation)			changes.Add(("rotation",		newObj.rotation			));
		if(oldObj.scale				!= newObj.scale)			changes.Add(("scale",			newObj.scale			));
		if(oldObj.color				!= newObj.color)			changes.Add(("color",			newObj.color			));
		if(oldObj.compositionID		!= newObj.compositionID)	changes.Add(("compositionID",	newObj.compositionID	));

		return changes;
	}


	public void Undo() {

	}
}