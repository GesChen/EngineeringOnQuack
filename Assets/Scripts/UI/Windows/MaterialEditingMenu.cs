using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using W = MenuUtil.Window;

public class MaterialEditingMenu : MonoBehaviour {
	public delegate void StartEvent(CWindow cw,
		ref RectTransform colorPickerButton,
		ref RectTransform materialPickerButton);

	public static event StartEvent OnStart;

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
	static readonly float size = 100;

	public static readonly W editor = new W(
		"Material",
		size,
		new(){
			new W.Flyout(
				colorPicker,
				"Color"
				).AddSubItems(
					WindowItem.NewImage( // color preview
						new(),
						WindowItem.LayoutConfig.FillLayout // solid fill of color
					)),
			new W.CustomItem(
				WindowItem.NewFlyoutTrigger(
					new(materialPicker),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.TopLeft),
						new (size, size)
						)
					)
				)
		},
		movable: true,
		isFlyout: false,
		closable: true
		).AddEventToCW(
			CWindow.Configuration.Timings.Start,
			(cw) => {
				OnStart?.Invoke(cw, ref colorPickerButton, ref materialPickerButton);
			}
		);

	public static void ShowMenu(WindowItem source) {
		RectTransform rt = source.RealObject;

		editor.CWindow.RealisedWindow.PlaceAt(rt);
		editor.CWindow.RealisedWindow.gameObject.SetActive(true);

		//editor.CWindow.RealisedWindow.GetComponent<Flyout>().OverrideStart();
	}

	public static CWindow[] Windows => new[] {
		colorPicker,
		materialPicker,
		editor.CWindow
	};

	static RectTransform colorPickerButton;
	static RectTransform materialPickerButton;
	static void ShowColorPicker() {
		colorPicker.RealisedWindow.PlaceAt(colorPickerButton);
		colorPicker.RealisedWindow.gameObject.SetActive(true);
	}
	static void ShowMaterialPicker() {
		materialPicker.RealisedWindow.PlaceAt(materialPickerButton);
		materialPicker.RealisedWindow.gameObject.SetActive(true);
	}

}