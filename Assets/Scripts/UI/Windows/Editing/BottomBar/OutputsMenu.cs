using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class OutputsMenu {
	public static WindowItem LayoutContainer;

	public static Action<string> OnNameChanged;
	public static Action OnSubtract;
	public static Action OnRename;
	public static Action OnAdd;
	public static Action<int> OnItemSelected;
	public static void Select(int i) {
		OptionSelectionUIHelper.SetColors(LayoutContainer.SubItems.ToArray(), i);

		OnItemSelected?.Invoke(i);
	}

	public static void Show() { Window.RealisedWindow.Show(); }
	public static void Hide() { Window.RealisedWindow.Hide(); }

	public static CWindow Window;
	public static void Set() {
		Window = new() {
			Name = "Manage Outputs",
			Config = new() {
				Size = CWindow.Configuration.FreeSize(new(220, 220))
			},
			Items = new[] {
				WindowItem.NewText(
					new PComponents.Text(
						"Manage Outputs",
						TMPro.TextAlignmentOptions.Center
					),
					WindowItem.LayoutConfig.Custom(
						position: new(1, 0, 0, 0),
						sizeDelta: new(0, Config.UI.Menu.TitleHeight),
						fixedPosition: new() {
							Pivot = UIPosition.TopCenter
						}
					)
				),
				WindowItem.NewScrollView(
					new PComponents.ScrollView(
						horizontalScrolling: false
					),
					WindowItem.LayoutConfig.DynamicLayout(
						new(
							Config.UI.Menu.TitleHeight, 0,
							(Config.UI.Menu.ItemHeight + Config.UI.Menu.ItemPadding) * 2, 0)
					),
					new(){
						WindowItem.NewLayout(
							PComponents.Layout.Vertical.Fixed(
								false,
								true
							),
							WindowItem.LayoutConfig.Custom(
								position: new(1, 0, 0, 0),
								sizeDelta: new(0, 0)
							),
							new(){}
						).OnRealized((_, wi) =>
							LayoutContainer = wi
						)
					}
				),
				WindowItem.NewInputField(
					new PComponents.InputField(
						(name) => OnNameChanged?.Invoke(name),
						placeholderText: "Name for Output..."
					),
					WindowItem.LayoutConfig.Custom(
						position: new(0, 0, 1, 0),
						sizeDelta: new(0, Config.UI.Menu.ItemHeight),
						fixedPosition: new() {
							Pivot = UIPosition.BottomCenter,
							Position = new(0, Config.UI.Menu.ItemHeight + Config.UI.Menu.ItemPadding)
						}
					)
				),
				WindowItem.NewLayout(
						PComponents.Layout.Horizontal.Fixed(
							true,
							true),
						WindowItem.LayoutConfig.Custom(
							position: new(0, 0, 1, 0),
							sizeDelta: new(0, Config.UI.Menu.ItemHeight),
							fixedPosition: new() {
								Pivot = UIPosition.BottomCenter
							}
						),
						new() {
							WindowItem.NewButtonCustomImageOverlay( // -
								new PComponents.Button(
									() => OnSubtract?.Invoke()
								),
								new PComponents.Image(
									Config.Locations.IconsFolder + "subtract"
								),
								WindowItem.LayoutConfig.LayoutElementDynamic(
									new(Config.UI.Menu.ItemPadding)
								)
							).AddComponents(
								new PComponents.LayoutElement(1)
							),
							WindowItem.NewButtonCustomText( // rename
								new PComponents.Button(
									() => OnRename.Invoke()
								),
								new PComponents.Text(
									"Rename",
									TMPro.TextAlignmentOptions.Center,
									fontSize: Config.UI.Menu.FontSize
								),
								WindowItem.LayoutConfig.LayoutElementDynamic(
									new(Config.UI.Menu.ItemPadding)
								)
							).AddComponents(
								new PComponents.LayoutElement(4)
							),
							WindowItem.NewButtonCustomImageOverlay( // +
								new PComponents.Button(
									() => OnAdd?.Invoke()
								),
								new PComponents.Image(
									Config.Locations.IconsFolder + "add"
								),
								WindowItem.LayoutConfig.LayoutElementDynamic(
									new(Config.UI.Menu.ItemPadding)
								)
							).AddComponents(
								new PComponents.LayoutElement(1)
							)
						}
					)
			}
		};
	}

	public static void UpdateMenu(List<string> outputs) {
		LayoutContainer.SetSubItems(
			outputs.Select(OutputItem).ToArray()
		);

		WindowRealiser.Instance.UpdateWindow(Window);
	}

	public static void ShowMenu(RectTransform sourceButton) {
		Window.RealisedWindow.PlaceAt(sourceButton, 1, false);
		Window.RealisedWindow.Show();
	}

	static WindowItem OutputItem(string name, int i) =>
		WindowItem.NewButtonCustomText(
			new PComponents.Button(
				() => Select(i)
			),
			new PComponents.Text(
				name,
				TMPro.TextAlignmentOptions.Left,
				fontSize: Config.UI.Menu.FontSize
			),
			WindowItem.LayoutConfig.LayoutElement(
				new(0, Config.UI.Menu.ItemHeight),
				Config.UI.Menu.ItemPadding * FourSides.One)
		);

	public static CWindow[] Windows => new[] {
		Window.SetGroup("tools")
	};
}