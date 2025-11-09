using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sertester : MonoBehaviour {
	private void Update() {
		if (Input.GetKeyDown("s")) {
			ScriptEditorRewritten.CreateWindow();
		}
	}
}