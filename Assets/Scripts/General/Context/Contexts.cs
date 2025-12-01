using System;
using UnityEngine;

/*
 * main 
 * editing
 * inworld
 * noselection
 * singleselection
 * multiselection
 * overui
 */

namespace Contexts {
	public class Main : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => null;
		public Main() { }
	}

	public class Playing : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => typeof(Main);
		public Playing(IContext parent) => ((IContext)this).SetParent(parent);
		public Playing() { }
	}

	public class Operating : IContext {
		public string Name => "Operating";
		public IContext Parent { get; set; }
		public Type ParentType => typeof(Playing);
		public Operating(IContext parent) => ((IContext)this).SetParent(parent);
		public Operating() { }
	}

	public class Editing : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => typeof(Playing);
		public Editing(IContext parent) => ((IContext)this).SetParent(parent);
		public Editing() { }
	}

	public class InWorld : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => typeof(Editing);
		public InWorld(IContext parent) => ((IContext)this).SetParent(parent);
		public InWorld() { }
	}

	public class NoSelection : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => typeof(InWorld);
		public NoSelection(IContext parent) => ((IContext)this).SetParent(parent);
		public NoSelection() { }
	}

	public class SingleSelection : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => typeof(InWorld);
		public SingleSelection(IContext parent) => ((IContext)this).SetParent(parent);
		public SingleSelection() { }
		public Transform Selected;
		public int SelectedBasePartID;
	}

	public class MultiSelection : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => typeof(InWorld);
		public MultiSelection(IContext parent) => ((IContext)this).SetParent(parent);
		public MultiSelection() { }

		public Transform[] Selected;
		public int[] SelectedBasePartIDs;
	}

	public class GroupSelection : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => typeof(InWorld);
		public GroupSelection(IContext parent) => ((IContext)this).SetParent(parent);
		public GroupSelection() { }

		public bool AllGroupedParts; // all selected parts are part of a group
		public bool AllPartsOfOneGroup; // or has parts from other groups in it
		public bool AllGroupPartsSelected; // only applies when both above are true
		/*
		 * AGP F & APOOG F - multiple groups and also non grouped parts 
		 * AGP T & APOOG F - multiple groups
		 * AGP F & APOOG T - one group with other non group parts
		 * AGP T & APOOG T 
		 *   AGPS T - all parts in one group 
		 *   AGPS F - invididual member(s) of only one group 
		 */
	}

	public class OverUI : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => typeof(Editing);
		public OverUI(IContext parent) => ((IContext)this).SetParent(parent);
		public OverUI() { }
	}
}