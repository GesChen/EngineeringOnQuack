using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewSystemTest : MonoBehaviour {
	void Start() {
		PUI_Panel newPanel = new(new() {
			new PUI_Text(new("stuff", description: "idk", layout: new(2, 1))),
			new PUI_Button(null, new("button"))
		},
		new(true, true, true, true));
	}
}