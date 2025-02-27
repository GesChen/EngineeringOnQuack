using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryPart : Part {
	public Memory component;
	public CableConnection InterpreterCC;

	private void Start() {
		//Initialize(InterpreterCC);
	}

	public void Initialize(CableConnection interpreterCC) {
		InterpreterCC = interpreterCC;

		component = new(InterpreterCC);
		component.Initialize();
	}
}