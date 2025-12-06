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
		ContextManager.EnterContext<Playing>();
	}

	void Update() {
		if (ContextManager.CurrentlyInContextStrict<Playing>())
			CheckPlaying();

		if (ContextManager.CurrentlyInContext<Editing>())
			CheckEditing();


		debug_currentContext = ContextManager.Current.GetType().Name;
	}

	public Func<int> RequestSelectionCount;
	public Func<bool> GroupCheck;
	public Func<(Transform[] selectedTransforms, int[] BPids)> GetCurrentSelectionInfo;
	void CheckEditing() {
		var (selectedTransforms, BPids) = GetCurrentSelectionInfo?.Invoke() ?? throw new("GCSSBPID not subscribed to");
		int count = selectedTransforms.Length;

		if (UIHovers.AnyHovers()) {
			ContextManager.EnterContext<OverUI>();
		} else {
			ContextManager.EnterContext<InWorld>();

			if (count == 0) ContextManager.EnterContext<NoSelection>();
			else {
				// groupcheck in selectionmanager will enter groupselection 
				// by itself so only if it fails then enter 
				bool isGroup = GroupCheck?.Invoke() ?? throw new("GroupCheck not subscribed to!");

				if (!isGroup) {
					if (count == 1) {
						var c = ContextManager.EnterContext<SingleSelection>();
						c.Selected = selectedTransforms[0];
						c.SelectedBasePartID = BPids[0];
					} else {
						var c = ContextManager.EnterContext<MultiSelection>();
						c.Selected = selectedTransforms;
						c.SelectedBasePartIDs = BPids;
					}
				}
			}
		}
	}
	
	void CheckPlaying() {

	}
}