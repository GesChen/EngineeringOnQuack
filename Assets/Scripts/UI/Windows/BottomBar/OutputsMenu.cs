using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = PMenu.Window;

public static class OutputsMenu {
	static readonly float ListBoxHeight = 100;
	public static WindowItem LayoutContainer;

	static readonly float width = 200;

	public static void ClearNameChanged() { OnNameChanged = null; }
	public static event Action<string> OnNameChanged;

	public static void ClearSubtract() { OnSubtract = null; }
	public static event Action OnSubtract;

	public static void ClearRename() { OnRename = null; }
	public static event Action OnRename;

	public static void ClearAdd() { OnAdd = null; }
	public static event Action OnAdd;

	public static void ClearItemSelected() { OnItemSelected = null; }
	public static event Action<int> OnItemSelected;
	public static void Select(int i) {
		OptionSelectionUIHelper.SetColors(LayoutContainer.SubItems.ToArray(), i);

		OnItemSelected?.Invoke(i);
	}

	public static W Menu;
	static void SetMenu() {
		Menu = new(
			"Manage Outputs",
			width,
			new() {
					new W.CustomItem( // list
						WindowItem.NewScrollView(
							new PComponents.ScrollView(
								horizontalScrolling: false
							),
							WindowItem.LayoutConfig.FixedLayout(
								UIPosition.AnchoredAt(UIPosition.TopLeft),
								new(width, ListBoxHeight),
								new FourSides(10)
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
						)
					),
					new W.InputField( // the actual naming part
						(name) => OnNameChanged?.Invoke(name), // you could put it directly as onnamechanged and it would work but 
						// it wouldn't use the up to date version with all of the subscriptions
						"Name for Output..."
					),
					new W.CustomItem( // controls
						WindowItem.NewLayout(
							PComponents.Layout.Horizontal.Fixed(
								true,
								true),
							WindowItem.LayoutConfig.FixedLayout(
								UIPosition.LayoutItem,
								new(width, Config.UI.Menu.ItemHeight)
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
										fontSize: Config.UI.Menu.FontSize,
										alignment: TMPro.TextAlignmentOptions.Center
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
					)
			},
			isFlyout: false,
			movable: true,
			closable: true,
			extraSpacing: Config.UI.Visual.DefaultLayoutSpacing
		);
	}

	public static void UpdateMenu(List<string> outputs) {
		LayoutContainer.SetSubItems(
			outputs.Select((o, i) => OutputItem(o, i)).ToArray());

		Menu.RequestRegeneration();

		WindowRealiser.Instance.UpdateWindow(Menu.CWindow);
	}

	public static void ShowMenu(RectTransform sourceButton) {
		Menu.CWindow.RealisedWindow.PlaceAt(sourceButton, true, true, true);
		Menu.CWindow.RealisedWindow.Show();
	}

	static WindowItem OutputItem(string name, int i) =>
		WindowItem.NewButtonCustomText(
			new PComponents.Button(
				() => Select(i)
			),
			new PComponents.Text(
				name,
				fontSize: Config.UI.Menu.FontSize,
				alignment: TMPro.TextAlignmentOptions.Left
			),
			WindowItem.LayoutConfig.FixedLayout(
				UIPosition.LayoutItem,
				new(width, Config.UI.Menu.ItemHeight),
				new(5)
			)
		);

	public static void Set() {
		SetMenu();
	}
	public static CWindow[] Windows => new[] {
		Menu.CWindow.SetGroup("tools")
	};
	public static W[] Menus => new[] {
		Menu
	};
}