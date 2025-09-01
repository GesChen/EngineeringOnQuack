using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using W = PMenu.Window;

public class MaterialEditingMenu {
	public static void ClearEvents() {
		OnStart = null;
		OnColorSelection = null;
		OnRequestCompositionItems = null;
		OnCompositionSelection = null;
	}

	public static readonly float size = 100;

	public delegate void StartEvent(CWindow cw,
		ref RectTransform colorPickerButton,
		ref RectTransform compositionPickerButton);

	public static event StartEvent OnStart;

	public static Action<Color> OnColorSelection;

	// right click version for now
	public static CWindow ColorPicker;
	static void SetColorPicker() {
		ColorPicker = new() {
			Name = "Color Picker",
			Config = new() {
				Movable = false,
				Resizable = false,
				ContentDynamic = true,
				DynamicPadding = new(5),
				Closable = false
			},
			Items = new WindowItem[] {
				WindowItem.NewLayout(
					PComponents.Layout.Horizontal.Dynamic(),
					WindowItem.LayoutConfig.FillLayout,
					Config.Building.Colors.Select(c => 

						// make the color button for each one
						WindowItem.NewButtonCustomImageComponent(
							"Color option",
							new (
								() => OnColorSelection?.Invoke(c),
								Config.UI.ColorBlock.WhiteBlock),
							new (c), // might make the color part an inner element
							WindowItem.LayoutConfig.LayoutElement(
								Config.Building.ColorPickerItemSize,
								new(0)
								)
							)
						).ToList()
					)
			}
		};
	}
	// also rc version for now
	public static Func<WindowItem[]> OnRequestCompositionItems;
	public static event Action<int> OnCompositionSelection;

	public static void SelectComposition(int index) {
		OnCompositionSelection?.Invoke(index);
	}

	public static CWindow CompositionPicker;
	static void SetCompositionPicker() { 
		var items =
			OnRequestCompositionItems?.Invoke()
			?? throw new InvalidOperationException("No composition items provider registered.");
		
		CompositionPicker = new() {
			Name = "Composition Picker",
			Config = new() {
				Movable = false,
				Resizable = false,
				ContentDynamic = true,
				DynamicPadding = new(5),
				Closable = false
			},
			Items = new[] {
				WindowItem.NewLayout(
					PComponents.Layout.Horizontal.Dynamic(),
					WindowItem.LayoutConfig.FillLayout,
					items.ToList() // too lazy to refactor this
					)
			}
		};
	}

	// has to be a property to prevent getting evaluated at type initialization
	public static W Editor;
	static void SetEditor() { 
		Editor = new W(
			"Material",
			size,
			new(){
				new W.Flyout(
					ColorPicker,
					"",
					addIndicator: false
					).AddSubItems(
						WindowItem.NewImage( // color preview
							new(),
							WindowItem.LayoutConfig.FillLayout // solid fill of color
						)),

				new W.CustomItem(
					WindowItem.NewFlyoutTrigger(
						new PComponents.FlyoutTrigger(CompositionPicker),
						WindowItem.LayoutConfig.FixedLayout(
							UIPosition.AnchoredAt(UIPosition.TopLeft),
							new (size, size)
							),
						new PComponents.HoverTarget(Config.UI.ColorBlock.WhiteBlock) // make it white
						)
					)
			},
			movable: true,
			isFlyout: false,
			closable: true
		).SetCWEvents(
			new TimedEventInvoker.TimedEvent(
				TimedEventInvoker.Timing.Start,
				(_) => {
					OnStart?.Invoke(Editor.CWindow, ref colorPickerButton, ref compositionPickerButton);
				}
			)
		);
	}

	public static void ShowMenu(WindowItem source) {
		RectTransform rt = source.RealObject();

		Editor.CWindow.RealisedWindow.Show();
		Editor.CWindow.RealisedWindow.PlaceAt(rt, 1, false);

		//editor.CWindow.RealisedWindow.GetComponent<Flyout>().OverrideStart();
	}

	public static void ShowMenu(Vector2 at) {
		Editor.CWindow.RealisedWindow.Show();
		Editor.CWindow.RealisedWindow.SetWorldCorner(at, 4);
	}

	public static void Set() {
		SetColorPicker();
		SetCompositionPicker();
		SetEditor();
	}
	public static CWindow[] Windows => new[] {
		ColorPicker.SetGroup("tools/materialeditor"),
		CompositionPicker.SetGroup("tools/materialeditor"),
		Editor.CWindow.SetGroup("tools/materialeditor")
	};
	public static W[] Menus => new[] {
		Editor
	};

	static RectTransform colorPickerButton;
	static RectTransform compositionPickerButton;
	static void ShowColorPicker() {
		ColorPicker.RealisedWindow.Show();
		ColorPicker.RealisedWindow.PlaceAt(colorPickerButton, 1, false);
	}
	static void ShowCompositionPicker() {
		CompositionPicker.RealisedWindow.Show();
		CompositionPicker.RealisedWindow.PlaceAt(compositionPickerButton, 1, false);
	}
}