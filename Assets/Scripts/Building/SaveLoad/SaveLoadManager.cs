using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SaveLoadManager : Singleton<SaveLoadManager> {

	static readonly float SaveTextHideDelay = 1.5f;

	int currentlySelectedI = -1;
	string currentlySelectedName;

	WindowItem[] items;

	protected override void Awake() {
		base.Awake();

		SaveLoadMenus.ClearSave();
		SaveLoadMenus.OnSave += Save;

		SaveLoadMenus.ClearSaveAs();
		SaveLoadMenus.OnSaveAs += SaveAs;

		SaveLoadMenus.ClearLoadRequested();
		SaveLoadMenus.OnLoadRequested += UpdateLoadMenu;

		SaveLoadMenus.ClearLoadEntryChosen();
		SaveLoadMenus.OnLoadEntryChosen += LoadOptionSelect;

		SaveLoadMenus.ClearOnLoad();
		SaveLoadMenus.OnLoad += Load;
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

	void UpdateLoadMenu() {
		items =
			SaveLoadHelper.GetSortedAssemblyInfos().
			Select((info, i) => SaveLoadMenus.FileEntry(i, info.Name, info.Parts))
			.ToArray();

		currentlySelectedI = -1;

		SaveLoadMenus.LoadOptionsLayout.SetSubItems(items);
		WindowManager.Instance.Realiser.UpdateWindow(SaveLoadMenus.LoadOptionsMenu);
	}

	void LoadOptionSelect(int id) {
		currentlySelectedI = id;
		currentlySelectedName = SaveLoadHelper.GetSortedAssemblyInfos()[id].Name;

		OptionSelectionUIHelper.SetColors(items, id);
	}

	void Load() {
		if (currentlySelectedI == -1) return;

		SelectionManager.Instance.SetSelection();

		// hope name conflicts arent a thing
		SaveLoadHelper.LoadFromFile(currentlySelectedName);
	}
}