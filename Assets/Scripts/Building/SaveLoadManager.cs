using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveLoadManager : Singleton<SaveLoadManager> {

	static readonly float SaveTextHideDelay = 1.5f;
	protected override void Awake() {
		base.Awake();

		SaveLoadMenus.ClearSave();
		SaveLoadMenus.OnSave += Save;

		SaveLoadMenus.ClearSaveAs();
		SaveLoadMenus.OnSaveAs += SaveAs;
	}

	void Save() {
		string name = BuildingManager.Instance.CurrentAssemblyName;
		if (string.IsNullOrWhiteSpace(name)) {
			SaveLoadMenus.ShowNamePrompt((newName) => {
				BuildingManager.Instance.CurrentAssemblyName = newName;
				SaveFile(newName);
				}
			);
		} else {
			SaveFile(name);
		}
	}

	void SaveAs() {
		SaveLoadMenus.ShowNamePrompt((newName) => {
			BuildingManager.Instance.CurrentAssemblyName = newName;
			SaveFile(newName);
			}
		);
	}

	void SaveFile(string name) {
		SaveLoadMenus.HideNamePrompt();
		SaveLoadMenus.ShowSaveIcon();
		SaveLoadMenus.SetSaveText("Saving...");

		SaveLoadHelper saveLoad = new();
		saveLoad.SaveCurrentBuild(name);

		SaveLoadMenus.SetSaveText("Saved!");
		StartCoroutine(SaveTextDelay());
	}

	IEnumerator SaveTextDelay() {
		yield return new WaitForSeconds(SaveTextHideDelay);
		SaveLoadMenus.HideSaveIcon(); 
	}
}