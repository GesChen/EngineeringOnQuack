using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveLoadManager : Singleton<SaveLoadManager> {

	static readonly float SaveTextHideDelay = 1.5f;

	int currentlySelectedI = -1;
	string currentlySelectedName;

	List<WindowItem> items;

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

		SaveLoadHelper.SaveCurrentBuild(name);

		SaveLoadMenus.SetSaveText("Saved!");
		StartCoroutine(SaveTextDelay());
	}

	IEnumerator SaveTextDelay() {
		yield return new WaitForSeconds(SaveTextHideDelay);
		SaveLoadMenus.HideSaveIcon(); 
	}

	void UpdateLoadMenu() {
		items =
			SaveLoadHelper.GetSortedAssemblyInfos().
			Select((info, i) => SaveLoadMenus.FileEntry(i, info.Name, info.Parts))
			.ToList();

		currentlySelectedI = -1;

		SaveLoadMenus.LoadOptionsLayout.SubItems = items;
		WindowManager.Instance.realiser.UpdateWindow(SaveLoadMenus.LoadOptionsMenu);
	}

	void LoadOptionSelect(int id) {
		currentlySelectedI = id;
		currentlySelectedName = SaveLoadHelper.GetSortedAssemblyInfos()[id].Name;

		// bad code yes i know im lazy and this isnt called that much

		// set all backgrounds to normal
		// then set the i one to selected
		foreach (var item in items) {
			var button = item.GetComponent<PComponents.Button>().RealComponent as UnityEngine.UI.Button;
			if (button == null) throw new("bad casting to image, check this line");
			
			button.colors = (UnityEngine.UI.ColorBlock)Config.UI.Visual.DefaultColorBlock;
		}

		var block = Config.UI.Visual.DefaultColorBlock;
		block.NormalColor = block.ToggledColor;

		var selbutton = items[id].GetComponent<PComponents.Button>().RealComponent as UnityEngine.UI.Button;
		if (selbutton == null) throw new("bad casting to image, check this line");
		selbutton.colors = (UnityEngine.UI.ColorBlock)block;
	}

	void Load() {
		if (currentlySelectedI == -1) return;

		// hope name conflicts arent a thing
		SaveLoadHelper.LoadFromFile(currentlySelectedName);
	}
}