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
		if (ContextManager.IsInContext<Editing>())
			CheckEditing();

		debug_currentContext = ContextManager.Current.Name;
	}

	[HideInNormalInspector] public int selectionCount; 
	void CheckEditing() {
		if (UIHovers.AnyHovers()) {
			ContextManager.EnterContext<OverUI>();
		} else {
			ContextManager.EnterContext<InWorld>();

			if (selectionCount == 0) ContextManager.EnterContext<NoSelection>();
			else if (selectionCount == 1) ContextManager.EnterContext<SingleSelection>();
			else ContextManager.EnterContext<MultiSelection>();
		}
	}
}