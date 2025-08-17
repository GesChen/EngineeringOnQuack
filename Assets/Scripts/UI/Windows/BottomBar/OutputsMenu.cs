using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = PMenu.Window;

public static class OutputsMenu {
	static readonly float ListBoxHeight = 100;
	static WindowItem LayoutContainer;

	static float width = 200;

	static string CurrentName = "";

	static void Subtract() {

	}

	static void Rename() {

	}

	static void Add() {

	}

	public static W Menu = new(
		"Manage Outputs",
		width,
		new() {
			new W.CustomItem( // list
				WindowItem.NewScrollView(
					new PComponents.ScrollView(),
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
							WindowItem.LayoutConfig.FillLayout,
							new(){}
						).OnRealized((_, wi) =>
							LayoutContainer = wi
						)
					}
				)
			),
			new W.InputField( // the actual naming part
				(newname) => CurrentName = newname,
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
							new PComponents.Button(Subtract),
							new PComponents.Image(
								Config.Locations.IconsFolder + "subtract"
							),
							WindowItem.LayoutConfig.LayoutElementDynamic(
								new(Config.UI.Menu.ItemPadding)
							)
						).AddComponents(
							new PComponents.LayoutElement(1)
						),
						WindowItem.NewButton( // rename
							new PComponents.Button(Subtract),
							WindowItem.LayoutConfig.LayoutElementDynamic(
								new(Config.UI.Menu.ItemPadding)
							)
						).SetSubItems(
							WindowItem.NewText(
								new PComponents.Text(
									"Rename",
									fontSize: Config.UI.Menu.FontSize,
									alignment: TMPro.TextAlignmentOptions.Center
								),
								WindowItem.LayoutConfig.FillLayout
							)
						).AddComponents(
							new PComponents.LayoutElement(4)
						),
						WindowItem.NewButtonCustomImageOverlay( // +
							new PComponents.Button(Subtract),
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

	public static void UpdateMenu(List<string> outputs) {
		LayoutContainer.SetSubItems(
			outputs.Select(o => OutputItem(o)).ToArray());

		Menu.RequestRegeneration();

		WindowRealiser.Instance.UpdateWindow(Menu.CWindow);
	}

	public static void ShowMenu() {
		Vector2 center = Menu.CWindow.RealisedWindow.canvas
			.renderingDisplaySize / 2f;
		
		Menu.CWindow.RealisedWindow.SetWorldCorner(center, 4);
		Menu.CWindow.RealisedWindow.Show();
	}

	static WindowItem OutputItem(string name) =>
		WindowItem.NewImage(
			new PComponents.Image(Config.UI.Visual.BackgroundColor),
			WindowItem.LayoutConfig.FixedLayout(
				UIPosition.LayoutItem,
				new(width, Config.UI.Menu.ItemHeight)
			)
		).SetSubItems(
			WindowItem.NewText(
				new PComponents.Text(
					name,
					fontSize: Config.UI.Menu.FontSize
				),
				WindowItem.LayoutConfig.FillLayout
			)
		);

	public static CWindow Window => Menu.CWindow;
}