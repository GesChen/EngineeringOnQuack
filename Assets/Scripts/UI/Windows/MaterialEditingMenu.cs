using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
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
			new W.Button(
				, 
				"Color"
				).AddSubItems(
					WindowItem.NewImage( // color preview
						new(),
						WindowItem.LayoutConfig.FillLayout // solid fill of color
					)),
			new W.CustomItem(
				WindowItem.NewButton(
					new(),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.TopLeft),
						new (size, size)
						)
					)
				)
		}).AddEventToCW(
			CWindow.Configuration.Timings.Start,
			() => {
				// add materialeditor and set up 

			}
		);


	public static void ShowColorPicker() {
		colorPicker.RealisedWindow.PlaceAt()
	}

}