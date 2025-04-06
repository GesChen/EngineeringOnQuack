using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryPart : Part {
	public Memory component;

	public void Initialize() {
		component.Initialize();
	}
}