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

	public Func<int> RequestSelectionCount;
	private int selectionCount;
	public Func<bool> GroupCheck;
	public Func<(Transform[] selectedTransforms, int[] BPids)> GetCurrentSelectionInfo;
	void CheckEditing() {
		selectionCount = RequestSelectionCount?.Invoke() ?? throw new("RequestSelectionCount not subscribed to");

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
					var (selectedTransforms, BPids) = GetCurrentSelectionInfo?.Invoke() ?? throw new("GCSSBPID not subscribed to"); ;

					if (selectionCount == 1) {
						var c = ContextManager.EnterContext<SingleSelection>();
						c.Selected = selectedTransforms[0];
						c.SelectedBasePartID = BPids[0];
							
					} else {
						var c = ContextManager.EnterContext<MultiSelection>();
						c.SelectedBasePartIDs = BPids;
					}
				}
			}
		}
	}
	
	void CheckSimulating() {

	}
}