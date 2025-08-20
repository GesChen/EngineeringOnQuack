using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutputManager : Singleton<OutputManager> {

	string currentName;
	int currentlySelectedI = -1;

	protected override void Awake() {
		base.Awake();

		BottomBar.OnOutputsOpened += OpenModifyOutputs;

		OutputsMenu.OnItemSelected += (i) => currentlySelectedI = i;

		OutputsMenu.OnNameChanged += (name) => currentName = name;

		OutputsMenu.OnSubtract += OnSubtractPressed;

		OutputsMenu.OnRename += OnRenamePressed;

		OutputsMenu.OnAdd += OnAddPressed;
	}

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
		bool exists = Outputs().Contains(currentName);
		if (currentName != null && currentlySelectedI != -1 && !exists) {
			RenameOutput(currentlySelectedI, currentName);
		}
	}

	public void OnAddPressed() {
		bool exists = Outputs().Contains(currentName);
		if (currentName != null && !exists) {
			AddNewOutput(currentName);
		}
	}

	public void RemoveOutput(int i) {
		BuildingManager.Instance.Assembly.Outputs.RemoveAt(i);

		UpdateMenu();
	}

	public void RenameOutput(int i, string name) {
		BuildingManager.Instance.Assembly.Outputs[i].Name = name;
	
		UpdateMenu();
	}

	public void AddNewOutput(string name) {
		BuildingManager.Instance.Assembly.Outputs.Add(new() { Name = name });
	
		UpdateMenu();
	}

	public void UpdateMenu() {
		OutputsMenu.UpdateMenu(Outputs());
	}

	private static List<string> Outputs() {
		return BuildingManager.Instance.Assembly.Outputs.Select(o => o.Name).ToList();
	}
}