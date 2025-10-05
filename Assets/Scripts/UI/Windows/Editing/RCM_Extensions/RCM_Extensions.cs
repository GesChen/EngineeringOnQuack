using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = PMenu.Window;
using RCM = RightClickMenus;

public static class RCM_Extensions {
	public struct PartExtension {
		public int AssociatedBasePartID;
		public int Width;
		public (W.Item WI, int RCMi)[] NewItems; 
		// WI-the window item, RCMi-index of WI in RCM items

		public PartExtension(int associatedBasePart, int width, params W.Item[] newItems) {
			AssociatedBasePartID = associatedBasePart;
			Width = width;
			NewItems = newItems.Select(i => (i, -1)).ToArray();
		}
	}

	public static void Setup() {
		CPU_UI.Setup();
	}

	public static readonly PartExtension[] PartExtensions = new PartExtension[] {
		new(3, // cpu
200,
new W.Button(() => RCM.Call(CPU_UI.OnEditScript), "edit script", iconName: "edit script")
		)
	};
}