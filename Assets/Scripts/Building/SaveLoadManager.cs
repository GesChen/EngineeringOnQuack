using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveLoadManager : Singleton<SaveLoadManager> {

	protected override void Awake() {
		base.Awake();

		BottomBar.ClearSave();
		BottomBar.OnSave += Save;

		BottomBar.ClearSaveAs();
		BottomBar.OnSaveAs += SaveAs;
	}

	void Save() {
		SaveLoadHelper saveLoad = new();

		string name = BuildingManager.Instance.CurrentAssemblyName;
		if (string.IsNullOrWhiteSpace(name)) {
			BottomBar.ShowNamePrompt((newName) => 
				saveLoad.SaveCurrentBuild(newName)
				);
		} else {
			saveLoad.SaveCurrentBuild(name);
		}
	}

	void SaveAs() {
		SaveLoadHelper saveLoad = new();

		BottomBar.ShowNamePrompt((newName) =>
			saveLoad.SaveCurrentBuild(newName)
			);
	}
}