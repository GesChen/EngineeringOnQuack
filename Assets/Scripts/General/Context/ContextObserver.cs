using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Contexts;

public class ContextObserver : Singleton<ContextObserver> {
	public string debug_currentContext;

	// tbd and finished all the way cuz this is very temporary
	void Start() {
		ContextManager.EnterContext(new Main());
		ContextManager.EnterContext<Editing>();
	}
	void Update() {
		if (ContextManager.IsInContext<Editing>(out _))
			CheckEditing();

		debug_currentContext = ContextManager.Current.Name;
	}

	[HideInNormalInspector] public int selectionCount;
	public Func<bool> GroupCheck;
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
					if (selectionCount == 1)
						ContextManager.EnterContext<SingleSelection>();
					else
						ContextManager.EnterContext<MultiSelection>();
				}
			}
		}
	}
}