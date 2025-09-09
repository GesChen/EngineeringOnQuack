using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FileExplorer;

public class deleteaftertest : MonoBehaviour {
	private void Update() {
		if (Input.GetKeyDown("e")) {
			FileExplorer.SetEntries(
				new EntryData("test", 5,
					("met", 2),
					("mea", 3)),

				new EntryData("aeea", 5,
					("me123t", 2),
					("m5ea", 3)));
		}
	}
}