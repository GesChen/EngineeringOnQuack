using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutputManager : Singleton<OutputManager> {

	string currentName;
	int currentlySelectedI = -1;

	public Action OnOutputsChanged;

	protected override void Awake() {
		base.Awake();

		BottomBar.ClearOutputs();
		BottomBar.OnOutputsOpened += OpenModifyOutputs;

		OutputsMenu.ClearItemSelected();
		OutputsMenu.OnItemSelected += (i) => currentlySelectedI = i;

		OutputsMenu.ClearNameChanged();
		OutputsMenu.OnNameChanged += (name) => currentName = name;

		OutputsMenu.ClearSubtract();
		OutputsMenu.OnSubtract += OnSubtractPressed;

		OutputsMenu.ClearRename();
		OutputsMenu.OnRename += OnRenamePressed;

		OutputsMenu.ClearAdd();
		OutputsMenu.OnAdd += OnAddPressed;

		// ----------simulating------
		
		static void UpdateAllOutputStates() {
			SimulatingMainUI.TopBar.Outputs.UpdateOutputStates(
				BuildingManager.Instance.Assembly.Outputs.Select(
					(o, i) => (i, o.Visible)
				).ToArray()
			);
		}

		SimulatingMainUI.TopBar.Outputs.OnRequestOutputs = UpdateOutputs;

		// accessing is a bit long lmao
		SimulatingMainUI.TopBar.Outputs.OnItemToggled = (i) => {
			BuildingManager.Instance.Assembly.Outputs[i].Visible =
			!BuildingManager.Instance.Assembly.Outputs[i].Visible;

			UpdateAllOutputStates();
		};

		SimulatingMainUI.TopBar.Outputs.OnHideAll = () => {
			foreach (var output in BuildingManager.Instance.Assembly.Outputs)
				output.Visible = false;
			
			UpdateAllOutputStates();
		};

		SimulatingMainUI.TopBar.Outputs.OnShowAll = () => {
			foreach (var output in BuildingManager.Instance.Assembly.Outputs)
				output.Visible = true;
			
			UpdateAllOutputStates();
		};
	
		SimulatingMainUI.TopBar.Outputs.RequestOutputWindowsGeneration = GenerateAllOutputWindows;
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
		bool exists = GetOutputNames().Contains(currentName);
		if (!string.IsNullOrWhiteSpace(currentName) && currentlySelectedI != -1 && !exists) {
			RenameOutput(currentlySelectedI, currentName);
		}
	}

	public void OnAddPressed() {
		bool exists = GetOutputNames().Contains(currentName);
		if (!string.IsNullOrWhiteSpace(currentName) && !exists) {
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
		OutputsMenu.UpdateMenu(GetOutputNames());

		OnOutputsChanged?.Invoke();

		BuildingManager.SetDirty();
	}
	#endregion

	#region Simulating
	void UpdateOutputs() {
		SimulatingMainUI.TopBar.Outputs.UpdateOutputs(GetOutputNames().ToArray());
	}
	
	void GenerateAllOutputWindows() {
		for (int i = 0; i < BuildingManager.Instance.Assembly.Outputs.Count; i++) {
			Output output = BuildingManager.Instance.Assembly.Outputs[i];
			var window = 
				SimulatingMainUI.TopBar.Outputs.GenerateOutputWindow(i, output.Name, 0);

			output.SetWindow(window);
		}
	}

	#endregion

	private static List<string> GetOutputNames() => 
		BuildingManager.Instance.Assembly.Outputs.Select(o => o.Name).ToList();
}