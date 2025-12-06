using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class OperatingManager : Singleton<OperatingManager> {
	// games gonna stay single player for now ig
	public Creation CurrentlyOperating;

	// update this value 
	public Construct ToAssemble;
	public Action SetAssemble;
	public List<Creation> Creations = new();

	protected override void Awake() {
		base.Awake();

		GameManager.Instance.OM_Assemble = Assemble;
		GameManager.Instance.OM_AssembleFromEditing = AssembleFromEditing;
		GameManager.Instance.OM_BeginOperating = BeginOperating;
		GameManager.Instance.OM_DestroyCreation = DestroyCurrentCreation;
		GameManager.Instance.OM_SetCurrentAsLoadTarget = () => BuildingManager.Instance.ConstructToLoad = CurrentlyOperating.Construct;

		OperatingMainUI.TopBar.OnBarCreated = SetupTopBar;
		OperatingMainUI.TopBar.OnExitPressed = () => StopOperating(false);
		OperatingMainUI.TopBar.OnEditPressed = GameManager.Instance.BeginEditing;
		OperatingMainUI.TopBar.OnDestroyPressed = () => StopOperating(true);

		SubscribeToShortcuts();

		SetupOutputting();
	}

	void SubscribeToShortcuts() {

	}

	void SetupOutputting() {
		Part_Transceiver.OnPrintRequested = (outname, message) => 
			Instance.CurrentlyOperating.Outputs.FirstOrDefault(o => o.Name == outname)
			.Print(message);
		

		static void UpdateAllOutputStates() {
			OperatingMainUI.TopBar.Outputs.UpdateOutputStates(
				Instance.CurrentlyOperating.Outputs.Select(
					o => (o.Name, o.Visible)
				).ToArray()
			);
		}

		OperatingMainUI.TopBar.Outputs.OnRequestOutputs = UpdateOutputs;

		// accessing is a bit long lmao
		OperatingMainUI.TopBar.Outputs.OnItemToggled = (n) => {
			var output = Instance.CurrentlyOperating.Outputs.FirstOrDefault(o => o.Name == n);
			output.Visible = !output.Visible;

			UpdateAllOutputStates();
		};

		OperatingMainUI.TopBar.Outputs.OnHideAll = () => {
			foreach (var output in Instance.CurrentlyOperating.Outputs)
				output.Visible = false;

			UpdateAllOutputStates();
		};

		OperatingMainUI.TopBar.Outputs.OnShowAll = () => {
			foreach (var output in Instance.CurrentlyOperating.Outputs)
				output.Visible = true;

			UpdateAllOutputStates();
		};

		OperatingMainUI.TopBar.Outputs.RequestOutputWindowsGeneration = GenerateAllOutputWindows;
	}

	private static List<string> GetOutputNames() =>
		Instance.CurrentlyOperating.Outputs.Select(o => o.Name).ToList();

	void UpdateOutputs() {
		OperatingMainUI.TopBar.Outputs.UpdateOutputs(GetOutputNames().ToArray());
	}

	void GenerateAllOutputWindows() {
		var outs = Instance.CurrentlyOperating.Outputs;

		foreach (var output in outs) {
			var window =
				OperatingMainUI.TopBar.Outputs.GenerateOutputWindow(output.Name, 0);

			output.Setup(window);
		}
	}

	void SetupTopBar() {
		OperatingMainUI.TopBar.SetName(BuildingManager.Instance.Assembly.Name);
	}

	
	void Update() {
		if (!ContextManager.CurrentlyInContext<Contexts.Operating>()) return;


	}

	void StopOperating(bool destroy = false) {
		GameManager.Instance.ReturnToPlaying(destroy);

		CurrentlyOperating = null;
	}

	void BeginOperating() {

	}

	public void AssembleFromEditing() {
		ToAssemble = BuildingManager.Instance.Assembly.ConvertToConstruct();
		Assemble();
		CurrentlyOperating = Creations[^1];
	}

	public void Assemble() {
		InternalFunctions.ClearOnPrintCalled();
		PartInternalFunctions.ClearSubscriptions();
		Memory.ClearCPUGet();

		if (ToAssemble == null) throw new("forgot to set it bud");
		Assembler.Instance.Assemble(ToAssemble, out var assembled);

		Creations.Add(assembled);
	}

	public void DestroyCurrentCreation() {
		DestroyCreation(CurrentlyOperating);
		
		CurrentlyOperating = null;
	}

	public void DestroyCreation(Creation creation) {
		Creations.Remove(creation);

		Destroy(creation.gameObject);
	}
}