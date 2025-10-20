using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : Singleton<SimulationManager> {
	public List<Assembler.Assembled> assembledSubassemblies = new();

	protected override void Awake() {
		base.Awake();

		GameManager.Instance.OnStartSimulating += StartSimulating;
		GameManager.Instance.OnStopSimulating += StopSimulating;

		SimulatingMainUI.TopBar.ClearBarCreated();
		SimulatingMainUI.TopBar.OnBarCreated += SetupTopBar;

	}

	public void StartSimulating() {
		InternalFunctions.ClearOnPrintCalled();

		Assembler.Instance.Assemble(out assembledSubassemblies);
	}

	void SetupTopBar() {
		SimulatingMainUI.TopBar.SetName(BuildingManager.Instance.Assembly.Name);

		
	}

	public void StopSimulating() {
		foreach (var asm in assembledSubassemblies) {
			/*foreach (Transform obj in asm.Source) {
				Destroy(obj.gameObject);
			}*/

			Destroy(asm.Parent.gameObject);
		}
	}
}