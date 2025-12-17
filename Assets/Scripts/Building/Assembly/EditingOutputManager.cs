using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditingOutputManager : Singleton<EditingOutputManager> {

	string currentName;
	int currentlySelectedI = -1;

	public Action OnOutputsChanged;

	protected override void Awake() {
		base.Awake();

		// -----------editing----------
		BottomBar.OnOutputsOpened = OpenModifyOutputs;
		OutputsMenu.OnItemSelected = (i) => currentlySelectedI = i;
		OutputsMenu.OnNameChanged = (name) => currentName = name;
		OutputsMenu.OnSubtract = OnSubtractPressed;
		OutputsMenu.OnRename = OnRenamePressed;
		OutputsMenu.OnAdd = OnAddPressed;
	}

	#region Editing
	public void OpenModifyOutputs() {
		OutputsMenu.ShowMenu(BottomBar.OutputButton.RealObject());

		UpdateMenu();
	}

	public void OnSubtractPressed() {
		if (currentlySelectedI != -1) {
			RemoveOutput(currentlySelectedI);
		}
	}

	public void OnRenamePressed() {
		bool exists = BuildingManager.Instance.Assembly.Outputs.Contains(currentName);
		if (!string.IsNullOrWhiteSpace(currentName) && currentlySelectedI != -1 && !exists) {
			RenameOutput(currentlySelectedI, currentName);
		}
	}

	public void OnAddPressed() {
		bool exists = BuildingManager.Instance.Assembly.Outputs.Contains(currentName);
		if (!string.IsNullOrWhiteSpace(currentName) && !exists) {
			AddNewOutput(currentName);
		}
	}

	public void RemoveOutput(int i) {
		BuildingManager.Instance.Assembly.Outputs.RemoveAt(i);

		UpdateMenu();
	}

	public void RenameOutput(int i, string name) {
		BuildingManager.Instance.Assembly.Outputs[i] = name;
	
		UpdateMenu();
	}

	public void AddNewOutput(string name) {
		BuildingManager.Instance.Assembly.Outputs.Add(name);
	
		UpdateMenu();
	}

	public void UpdateMenu() {
		OutputsMenu.UpdateMenu(BuildingManager.Instance.Assembly.Outputs);

		OnOutputsChanged?.Invoke();

		BuildingManager.SetDirty();
	}
	#endregion
}