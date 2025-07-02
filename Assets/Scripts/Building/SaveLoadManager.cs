using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveLoadManager : Singleton<SaveLoadManager> {

	static readonly float SaveTextHideDelay = 1.5f;
	protected override void Awake() {
		base.Awake();

		BottomBar.ClearSave();
		BottomBar.OnSave += Save;

		BottomBar.ClearSaveAs();
		BottomBar.OnSaveAs += SaveAs;
	}

	void Save() {
		string name = BuildingManager.Instance.CurrentAssemblyName;
		if (string.IsNullOrWhiteSpace(name)) {
			BottomBar.ShowNamePrompt((newName) => {
				BuildingManager.Instance.CurrentAssemblyName = newName;
				SaveFile(newName);
				}
			);
		} else {
			SaveFile(name);
		}
	}

	void SaveAs() {
		BottomBar.ShowNamePrompt((newName) => {
			BuildingManager.Instance.CurrentAssemblyName = newName;
			SaveFile(newName);
			}
		);
	}

	void SaveFile(string name) {
		BottomBar.SaveStatusText.text = "Saving...";

		SaveLoadHelper saveLoad = new();
		saveLoad.SaveCurrentBuild(name);

		BottomBar.SaveStatusText.text = "Saved!";
		StartCoroutine(SaveTextDelay());
	}

	IEnumerator SaveTextDelay() {
		yield return new WaitForSeconds(SaveTextHideDelay);
		BottomBar.HideNamePrompt();
	}
}