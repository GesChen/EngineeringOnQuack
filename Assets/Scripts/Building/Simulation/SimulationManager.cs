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

		SimulatingMainUI.TopBar.Outputs.OnRequestOutputs += UpdateOutputs;
	}

	public void StartSimulating() {
		Assembler.Instance.Assemble(out assembledSubassemblies);
	}

	void SetupTopBar() {
		SimulatingMainUI.TopBar.SetName(BuildingManager.Instance.Assembly.Name);

		
	}

	void UpdateOutputs() {
		var outputNames = BuildingManager.Instance.Assembly.Outputs.Select(o => o.Name).ToArray();

		SimulatingMainUI.TopBar.Outputs.UpdateOutputs(outputNames);
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