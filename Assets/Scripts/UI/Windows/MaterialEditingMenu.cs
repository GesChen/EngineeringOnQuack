using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using W = PMenu.Window;

public class MaterialEditingMenu : MonoBehaviour {
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
	public static readonly CWindow colorPicker = new(){
		Name = "Color Picker",
		Config = new(){
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
							Config.UI.Visual.WhiteColorBlock),
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

	// also rc version for now
	public static Func<WindowItem[]> OnRequestCompositionItems;
	public static event Action<int> OnCompositionSelection;

	public static void SelectComposition(int index) {
		OnCompositionSelection?.Invoke(index);
	}

	private static CWindow m_compositionPicker;
	public static CWindow CompositionPicker {
		get {
			if (m_compositionPicker == null) {
				var items =
					OnRequestCompositionItems?.Invoke()
					?? throw new InvalidOperationException("No composition items provider registered.");
				m_compositionPicker = new() {
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

			return m_compositionPicker;
		}
	}

	// has to be a property to prevent getting evaluated at type initialization
	private static W m_editor;
	public static W Editor {
		get {
			m_editor ??= new W(
				"Material",
				size,
				new(){
					new W.Flyout(
						colorPicker,
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
							new PComponents.HoverTarget(Config.UI.Visual.WhiteColorBlock), // make it white
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
					TimedEventInvoker.Timing.Start,
					(_) => {
						OnStart?.Invoke(m_editor.CWindow, ref colorPickerButton, ref compositionPickerButton);
					}
				);
			
			return m_editor;
		}
	}

	public static void ShowMenu(WindowItem source) {
		RectTransform rt = source.RealObject;

		Editor.CWindow.RealisedWindow.Show();
		Editor.CWindow.RealisedWindow.PlaceAt(rt, true, true, false);

		//editor.CWindow.RealisedWindow.GetComponent<Flyout>().OverrideStart();
	}

	public static void ShowMenu(Vector2 at) {
		Editor.CWindow.RealisedWindow.Show();
		Editor.CWindow.RealisedWindow.SetWorldCorner(at, 4);
	}

	// has to be property because it needs to be reevaluated at creationtime
	public static CWindow[] Windows => new[] {
		colorPicker,
		CompositionPicker,
		Editor.CWindow
	};

	static RectTransform colorPickerButton;
	static RectTransform compositionPickerButton;
	static void ShowColorPicker() {
		colorPicker.RealisedWindow.Show();
		colorPicker.RealisedWindow.PlaceAt(colorPickerButton, true, true, false);
	}
	static void ShowCompositionPicker() {
		CompositionPicker.RealisedWindow.Show();
		CompositionPicker.RealisedWindow.PlaceAt(compositionPickerButton, true, true, false);
	}

}