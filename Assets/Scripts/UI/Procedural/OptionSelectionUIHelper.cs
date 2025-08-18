using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// dont know how to describe this
/// <summary>
/// Helper class to manage the color state of multi-button
/// layouts 
/// </summary>
public static class OptionSelectionUIHelper {
	public static void SetColors(WindowItem[] items, params int[] selection) {
		// bad code yes i know im lazy and this isnt called that much

		// set all backgrounds to normal
		// then set the i one to selected

		var selectedBlock = Config.UI.ColorBlock.DefaultBlock;
		selectedBlock.NormalColor = selectedBlock.ToggledColor;

		for (int i = 0; i < items.Length; i++) {
			WindowItem item = items[i];
			var button = item.GetComponent<PComponents.Button>().RealComponent as UnityEngine.UI.Button;
			if (button == null) throw new("bad casting to image, check this line");
			
			button.colors = (UnityEngine.UI.ColorBlock)
				(selection.Contains(i)
				? selectedBlock
				: Config.UI.ColorBlock.DefaultBlock);
		}
	}
}