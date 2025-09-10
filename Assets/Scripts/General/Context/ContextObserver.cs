using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Contexts;

public class ContextObserver : Singleton<ContextObserver> {
	public string debug_currentContext;

	// tbd and finished all the way cuz this is very temporary
	void Start() {
		ContextManager.ForceEnterContext(new Main());
		ContextManager.EnterContext<Editing>();

		GameManager.Instance.OnStartSimulating += StartSimulating;
		GameManager.Instance.OnStopSimulating += StartEditing;
	}

	void Update() {
		if (ContextManager.IsInContext<Editing>(out _))
			CheckEditing();

		if (ContextManager.IsInContext<Simulating>(out _))
			CheckSimulating();

		debug_currentContext = ContextManager.Current.GetType().Name;
	}

	public void StartEditing() { ContextManager.EnterContext<Editing>(); }
	public void StartSimulating() { ContextManager.EnterContext<Simulating>(); }

	[HideInNormalInspector] public int selectionCount;
	public Func<bool> GroupCheck;
	public Func<int> GetCurrentSSBasePartID;
	void CheckEditing() {
		if (UIHovers.AnyHovers()) {
			ContextManager.EnterContext<OverUI>();
		} else {
			ContextManager.EnterContext<InWorld>();

			if (selectionCount == 0) ContextManager.EnterContext<NoSelection>();
			else {
				// groupcheck in selectionmanager will enter groupselection 
				// by itself so only if it fails then enter 
				bool isGroup = GroupCheck?.Invoke() ?? throw new("GroupCheck not subscribed to!");

				if (!isGroup) {
					if (selectionCount == 1) {
						var c = ContextManager.EnterContext<SingleSelection>();
						c.SelectedBasePartID = GetCurrentSSBasePartID?.Invoke() ?? throw new("GCSSBPID not subscribed to");
					} else
						ContextManager.EnterContext<MultiSelection>();
				}
			}
		}
	}
	
	void CheckSimulating() {

	}
}