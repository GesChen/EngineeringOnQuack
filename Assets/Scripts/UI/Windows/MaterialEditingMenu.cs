using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = MenuUtil.Window;

public class MaterialEditingMenu : MonoBehaviour {
	// right click version for now
	public static readonly CWindow colorPicker = new(){
		Name = "Color Picker",
		Config = new(){
			Movable = false,
			Resizable = false,
			ContentDynamic = true,
			DynamicPadding = new(5)
		},
		Items = new WindowItem[] {

		}
	};

	// also rc version for now
	public static readonly CWindow materialPicker = new(){
		Name = "Material Picker",
		Config = new(){
			Movable = false,
			Resizable = false,
			ContentDynamic = true,
			DynamicPadding = new(5)
		},
		Items = new WindowItem[] {

		}
	};
	public static float size = 150;

	public static readonly W window = new W(
		"Material",
		size,
		new(){
			new W.Flyout(colorPicker, "Color"),
			new W.CustomItem(
				WindowItem.NewFlyoutTrigger(
					new(materialPicker),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.TopLeft),
						new (size, size)
						)
					)
				)
		}).AddUpdateToCW(()=>{
			// update color
			window.CWindow.Items[0].GetComponent<PComponents.>
		});
}