using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : Singleton<SimulationManager> {
	public List<Assembler.Assembled> assembledSubassemblies = new();

	public void StartSimulating() {
		Assembler.Instance.Assemble(out assembledSubassemblies);
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
