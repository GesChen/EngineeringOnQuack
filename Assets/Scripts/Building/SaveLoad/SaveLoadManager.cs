using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SaveLoadManager : Singleton<SaveLoadManager> {
	static readonly float SaveTextHideDelay = 1.5f;

	public event Action OnLoaded;

	protected override void Awake() {
		base.Awake();

		SaveLoadMenus.OnSave = Save;
		SaveLoadMenus.OnSaveAs = SaveAs;
		SaveLoadMenus.OnLoad = LoadFromPath;

		OnLoaded = null;
	}

	public void Save() {
		string name = BuildingManager.Instance.Assembly.Name;
		bool nameUnset = string.IsNullOrWhiteSpace(name);
		if (nameUnset)
			SaveAs();
		else
			SaveFile();
	}

	void SaveAs() {
		SaveLoadMenus.ShowNamePrompt((newName) => {
			BuildingManager.Instance.ChangeName(newName);
			SaveFile();
			}
		);
	}

	void SaveFile() {
		SaveLoadMenus.HideNamePrompt();
		SaveLoadMenus.ShowSaveIcon();
		SaveLoadMenus.SetSaveText("Saving...");

		//await Task.Run(() => SaveLoadHelper.SaveCurrentBuild());
		SaveLoadHelper.SaveCurrentBuild();

		SaveLoadMenus.SetSaveText("Saved!");
		StartCoroutine(SaveTextDelay());

		BuildingManager.Instance.Dirty = false;
	}

	IEnumerator SaveTextDelay() {
		yield return new WaitForSeconds(SaveTextHideDelay);
		SaveLoadMenus.HideSaveIcon();
	}

	public void LoadFromPath(string path) {
		SelectionManager.Instance.Clear();

		// hope name conflicts arent a thing
		string name = System.IO.Path.GetFileNameWithoutExtension(path);
		BuildingManager.Instance.LoadingConstruct = true;
		SaveLoadHelper.LoadFromFile(name);
		BuildingManager.Instance.LoadingConstruct = false;

		BottomBar.UpdateNameText(BuildingManager.Instance.Assembly.Name);

		OnLoaded?.Invoke();
	}
}