using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = MenuUtil.Window;

public class RightClickMenus : MonoBehaviour {
	// will have to add more later for other contexts but for now this is enough
	static W digital;
	static W mechanical;
	static W structural;
	static W newPart;
	static W mainPanel;

	static W editingNormalPanel = new(
		"Editing",
		200,
		new() {
			new W.Flyout(newPart, "new part", "makes a new part", "plus"),
			
		})
	public static CWindow[] GetWindows()
		=> MenuUtil.ConvertWindows();
}