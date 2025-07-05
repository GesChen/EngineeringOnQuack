using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AllWindows {

	public static void Init(WindowManager wm) {
		wm.RealiseWindows(RightClickMenus.Windows);
		wm.RealiseWindows(TransformToolsMenu.Windows);
		wm.RealiseWindows(MaterialEditingMenu.Windows);
		wm.RealiseWindows(SaveLoadMenus.Windows);
		wm.RealiseWindows(BottomBar.Windows);
	}
}