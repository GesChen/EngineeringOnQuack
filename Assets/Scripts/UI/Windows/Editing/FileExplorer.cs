using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FileExplorer {
	static string IconPath = "Icons/add to group";
	static string UseButtonLabel = "Load"; // to be changed per instance

	// properties will be instance members too
	static float FooterItemsHeights = 30;
	static float ItemHeight = 30;
	static float IconNameSpacing = 10;
	static WindowItem ItemsLayout;

	public static void ClearEvents() {
		OnUsePressed = null;
	}

	public void Show() {
		ExplorerWindow.RealisedWindow.PlaceAtCenter();
		ExplorerWindow.RealisedWindow.Show();
	}

	static void Cancel() {
		// do nothing back and just close
		ExplorerWindow.RealisedWindow.Hide();
	}

	// temp, signatures to change
	static event Action OnRenamePressed;
	static event Action OnDeletePressed;
	static event Action OnUsePressed;

	static void Rename() {

	}
	static void Delete() {

	}
	static void Use() {

	}

	static void Select(int i) {
		OptionSelectionUIHelper.SetColors(ItemsLayout.SubItems.ToArray(), i);

		CurrentlySelected = i;
	}
	static int CurrentlySelected = -1;
	static string CurrentFieldContents = "";
	static TMP_InputField InputField;
	static void ClearField() { InputField.text = ""; }

	// temporarily static stuff for now for testing 
	public static CWindow ExplorerWindow;
	static void SetEW() {
		ExplorerWindow = new() {
			Name = "Explorer",
			Config = new() {
				Resizable = true,
				Movable = true,
				Size = CWindow.Configuration.FreeSizeMinimum(
					new(500, 500),
					new(0, FooterItemsHeights)),
				HideOnStart = false
			},
			Items = new[] {
			WindowItem.NewScrollView(
				new PComponents.ScrollView(horizontalScrolling: false),
				WindowItem.LayoutConfig.DynamicLayout(
					FooterItemsHeights * FourSides.DownConst),
				new() {
					WindowItem.NewLayout(
						PComponents.Layout.Vertical.Fixed(false, true),
						WindowItem.LayoutConfig.FillLayout,
						new()
					).OnRealized((_, wi) => ItemsLayout = wi)
				}
			),
			WindowItem.NewInputField(
				new PComponents.InputField(
					v => CurrentFieldContents = v,
					"Enter name..."
				),
				WindowItem.LayoutConfig.Custom(
					position: new(0, 0, 1, 0),
					sizeDelta: new(0, FooterItemsHeights),
					fixedPosition: UIPosition.AnchoredOffset(
						UIPosition.BottomCenter,
						new(0,FooterItemsHeights + Config.UI.Visual.DefaultLayoutSpacing)
					)
				)
			).OnRealized((_, wi) => 
				InputField = (TMP_InputField)(wi.GetComponent<PComponents.InputField>()
				.RealComponent)),
			WindowItem.NewLayout(
				"Buttons",
				PComponents.Layout.Horizontal.Fixed(true, true),
				WindowItem.LayoutConfig.Custom(
					position: new(0, 0, 1, 0),
					sizeDelta: new(0, FooterItemsHeights),
					fixedPosition: new() {
						Pivot = UIPosition.BottomCenter
						}
				),
				new() {
					WindowItem.NewButtonCustomText(
						"Cancel",
						new PComponents.Button(() => Cancel()),
						new("Cancel", alignment: TMPro.TextAlignmentOptions.Center),
						WindowItem.LayoutConfig.LayoutElementDynamic()
					),
					WindowItem.NewButtonCustomText(
						"Rename",
						new PComponents.Button(() => Rename()),
						new("Rename", alignment: TMPro.TextAlignmentOptions.Center),
						WindowItem.LayoutConfig.LayoutElementDynamic()
					),
					WindowItem.NewButtonCustomText(
						"Delete",
						new PComponents.Button(() => Delete()),
						new("Delete", alignment: TMPro.TextAlignmentOptions.Center),
						WindowItem.LayoutConfig.LayoutElementDynamic()
					),
					WindowItem.NewButtonCustomText(
						"Use", // naming will be changed later to more specific
						new PComponents.Button(() => Use()),
						new(UseButtonLabel, alignment: TMPro.TextAlignmentOptions.Center), 
						WindowItem.LayoutConfig.LayoutElementDynamic()
					)
				}
			)
		}
		};
	}

	public struct EntryData {
		public string Name;
		public float NameWidth;
		public (string label, float width)[] Metadata;

		public EntryData(string name, float namewidth, params (string label, float width)[] metadata) {
			Name = name;
			NameWidth = namewidth;
			Metadata = metadata;
		}
	}
	static WindowItem FileEntry(int id, EntryData entry) =>
		WindowItem.NewButton(
			"File Entry",
			new PComponents.Button(() => Select(id)),
			WindowItem.LayoutConfig.LayoutElement(new(0, ItemHeight))
		).SetSubItems(
			WindowItem.NewLayout(
				PComponents.Layout.Horizontal.Fixed(true, true),
				WindowItem.LayoutConfig.FillLayout,
				new WindowItem[] {
					WindowItem.NewEmpty(
						WindowItem.LayoutConfig.LayoutElementDynamic(),
						new() {
							WindowItem.NewText(
							new PComponents.Text(entry.Name),
							WindowItem.LayoutConfig.DynamicLayout(
								FourSides.LeftConst * (ItemHeight + IconNameSpacing) // room for icon
							)
						),
						WindowItem.NewImage(
							"Icon",
							new PComponents.Image(IconPath),
							WindowItem.LayoutConfig.FixedLayout(
								UIPosition.AnchoredOffset(
									UIPosition.MiddleLeft,
									new(Config.UI.Menu.ItemPadding, 0)
								),
								Vector2.one * (ItemHeight - 2 * Config.UI.Menu.ItemPadding)
							)
						)
						}
					).AddComponents(new PComponents.LayoutElement(entry.NameWidth))
					.Wrap()
				}.Concat(entry.Metadata.Select(md =>
					WindowItem.NewText(
						new PComponents.Text(md.label),
						WindowItem.LayoutConfig.LayoutElementDynamic()
					).AddComponents(new PComponents.LayoutElement(md.width))
					.Wrap()
				)).ToList()
			)
		);

	public static void SetEntries(params EntryData[] entries) {
		ItemsLayout.SetSubItems(entries.Select((e, i) => FileEntry(i, e)).ToArray());
		 
		WindowRealiser.Instance.UpdateWindow(ExplorerWindow);
	}

	public static void Set() {
		SetEW();

		
	}
	public static CWindow[] Windows => new[] {
		ExplorerWindow
	};
	
}