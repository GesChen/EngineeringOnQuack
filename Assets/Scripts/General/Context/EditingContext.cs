using System;

namespace Contexts {
	public class Main : IContext {
		public string Name => "Main";
		public IContext Parent { get; set; }
		public Type ParentType => null;
		public Main() { }
	}

	public class Editing : IContext {
		public string Name => "Editing";
		public IContext Parent { get; set; }
		public Type ParentType => typeof(Main);
		public Editing(IContext parent) => ((IContext)this).SetParent(parent);
		public Editing() { }
	}

	public class InWorld : IContext {
		public string Name => "InWorld";
		public IContext Parent { get; set; }
		public Type ParentType => typeof(Editing);
		public InWorld(IContext parent) => ((IContext)this).SetParent(parent);
		public InWorld() { }
	}

	public class NoSelection : IContext {
		public string Name => "NoSelection";
		public IContext Parent { get; set; }
		public Type ParentType => typeof(InWorld);
		public NoSelection(IContext parent) => ((IContext)this).SetParent(parent);
		public NoSelection() { }
	}

	public class SingleSelection : IContext {
		public string Name => "SingleSelection";
		public IContext Parent { get; set; }
		public Type ParentType => typeof(InWorld);
		public SingleSelection(IContext parent) => ((IContext)this).SetParent(parent);
		public SingleSelection() { }
	}

	public class MultiSelection : IContext {
		public string Name => "MultiSelection";
		public IContext Parent { get; set; }
		public Type ParentType => typeof(InWorld);
		public MultiSelection(IContext parent) => ((IContext)this).SetParent(parent);
		public MultiSelection() { }
	}

	public class GroupSelection : IContext {
		public string Name => "GroupSelection";
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
		public string Name => "OverUI";
		public IContext Parent { get; set; }
		public Type ParentType => typeof(Editing);
		public OverUI(IContext parent) => ((IContext)this).SetParent(parent);
		public OverUI() { }
	}


	/*
	 * main 
	 * editing
	 * inworld
	 * noselection
	 * singleselection
	 * multiselection
	 * overui
	 */
}