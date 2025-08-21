using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = PMenu.Window;

public static class SaveLoadMenus {

	static SaveLoadMenus() {
		OnSave = null;
		OnSaveAs = null;
		OnLoad = null;
		OnLoadRequested = null;
		OnLoadEntryChosen = null;
	}

	public static event Action OnSave;
	public static void Save() { OnSave?.Invoke(); }

	public static event Action OnSaveAs;
	public static void SaveAs() { OnSaveAs?.Invoke(); }

	public static void ShowNamePrompt(Action<string> nameCallback) {
		NamePrompt.CWindow.RealisedWindow.Show();

		var canvas = NamePrompt.CWindow.RealisedWindow.canvas;
		Vector2 center = canvas.renderingDisplaySize / 2;
		NamePrompt.CWindow.RealisedWindow.SetWorldCorner(center, 4);

		SaveStatusText.text = "";

		OnNameEnterPressed = null;
		OnNameEnterPressed += () => nameCallback?.Invoke(PromptedName);
	}
	public static void HideNamePrompt() {
		NamePrompt.CWindow.RealisedWindow.Hide();
	}

	public static event Action OnLoadRequested;
	public static void ShowLoadMenu() {
		OnLoadRequested?.Invoke();

		Vector2 center = LoadOptionsMenu.RealisedWindow.canvas
			.renderingDisplaySize / 2f;
		LoadOptionsMenu.RealisedWindow.SetWorldCorner(center, 4);
		LoadOptionsMenu.RealisedWindow.Show();
	}
	public static void HideLoadMenu() {

	}

	public static void ShowSaveIcon() {
		SaveStatus.RealisedWindow.Show();
	}
	public static void HideSaveIcon() {
		SaveStatus.RealisedWindow.Hide();
	}
	public static void SetSaveText(string text) {
		SaveStatusText.text = text;
	}

	static string PromptedName;
	//static Image SaveStatusImage;
	static TextMeshProUGUI SaveStatusText;
	static event Action OnNameEnterPressed;

	static readonly W NamePrompt = new(
		"Name Your Creation!", 220, new(){
			new W.InputField(
				(value) => PromptedName = value,
				"Enter name here..."),
			new W.CustomItem(
				WindowItem.NewButtonCustomText(
					new PComponents.Button(() => OnNameEnterPressed?.Invoke()),
					new PComponents.Text(
						"Save!",
						fontSize: Config.UI.Menu.FontSize,
						alignment: TextAlignmentOptions.Center
					),
					PMenu.WindowItemLayout(220)
				)
			)
		},
		showTitle: true,
		isFlyout: false,
		closable: true,
		extraSpacing: 5
		);

	static readonly float imageSize = 50;
	static readonly float textHeight = 20;
	static readonly float margin = 5;

	static readonly CWindow SaveStatus = new(){
		Name = "Save Status",
		Config = new(){
			Resizable = false,
			Movable = false,
			Size = CWindow.Configuration.FixedSize(new(
				imageSize + margin * 2,
				imageSize + textHeight + margin * 3)),
			Position = UIPosition.AnchoredOffset(
				UIPosition.TopRight,
				new(-100, -100)),
			Closable = false,
		},
		Items = new WindowItem[]{
			WindowItem.NewImage(
				"Status Image",
				new PComponents.Image(
					Config.Locations.IconsFolder + "save"
				),
				WindowItem.LayoutConfig.FixedLayout(
					UIPosition.AnchoredOffset(
						UIPosition.TopLeft,
						new(margin, -margin)),
					new(imageSize, imageSize)
					)
			)
			.OnRealized((rt, _) => {

			}),
			WindowItem.NewText(
					"Status text",
					new PComponents.Text(
						"",
						alignment: TextAlignmentOptions.Center,
						fontSize: 16),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredOffset(
							UIPosition.BottomLeft,
							new(margin, margin)),
						new(imageSize, textHeight)
						)
				)
			.OnRealized((rt, _) => {
				SaveStatusText = rt.GetComponent<TextMeshProUGUI>();
			})
		}
	};

	static void Cancel() {
		LoadOptionsMenu.RealisedWindow.Hide();
	}

	public static event Action OnLoad;
	static void Load() {
		OnLoad?.Invoke();

		LoadOptionsMenu.RealisedWindow.Hide();
	}

	// really should be using a horizontal layout with layoutelements to set the sizes of each bro
	// i dont know what to call this its for the left and right positions
	// idfk bruh
	/* | -- | -- | -- |
	 * 0   .3   .6    1
	 * l0r6 l3r3 l6r0
	 * | -- | ---- | -- |
	 * 0   .2     .5    1
	 * l0r8    l2r5  l5r0
	 */
	static (float left, float right)[] LeftAndRights(float[] spacings) {
		int count = spacings.Length + 1;

		(float, float)[] leftrights = new (float, float)[count];
		for (int i = 0; i < count ; i++) {
			float left =
				i == 0
				? 0
				: spacings[i - 1];

			float right =
				i == count - 1
				? 0
				: 1 - spacings[i];

			leftrights[i] = (left, right);
		}
		return leftrights;
	}

	static readonly FourSides EntryTextMargin = new(20, 5);
	static readonly float[] Spacings = { .6f } ;
	static readonly (float left, float right)[] LeftRights = // look idk. 
		LeftAndRights(Spacings);
	static readonly float FileEntryHeight = 40;

	public static event Action<int> OnLoadEntryChosen;

	// add other details later like part count or whatever
	// like idk if i want filesize but i gotta add more than just name for now
	public static WindowItem FileEntry(int id, string name, int parts) =>
		WindowItem.NewButton(
			$"File Entry \"{name}\"",
			new PComponents.Button(() => OnLoadEntryChosen?.Invoke(id)),
			WindowItem.LayoutConfig.LayoutElement(
				FileEntryHeight * Vector2.one,
				new(Config.UI.Menu.ItemPadding)
				)
			).SetSubItems(
				WindowItem.NewText( // name text
					new PComponents.Text(
						name,
						alignment: TextAlignmentOptions.Left),
					WindowItem.LayoutConfig.DynamicLayout(
						margin: EntryTextMargin,
						position: new(0, LeftRights[0].right, 0, LeftRights[0].left))),
				WindowItem.NewText( // more stuff idk 
					new PComponents.Text(
						$"{parts} Parts",
						alignment: TextAlignmentOptions.Left),
					WindowItem.LayoutConfig.DynamicLayout(
						margin: EntryTextMargin,
						position: new(0, LeftRights[1].right, 0, LeftRights[1].left)))
				);


	public static WindowItem LoadOptionsLayout;
	static readonly float BottomOptionsHeight = 50;
	public static readonly CWindow LoadOptionsMenu = new(){
		Name = "Load Options Menu",
		Config = new(){
			Size = CWindow.Configuration.FreeSize(new(500, 500))
		},
		Items = new WindowItem[]{
			WindowItem.NewScrollView(
				"Files Scroll View",
				new PComponents.ScrollView(
					horizontalScrolling: false
				),
				WindowItem.LayoutConfig.DynamicLayout(
					margin: BottomOptionsHeight * FourSides.DownConst),
				new() { // file entries, probably make this procedural and update
					WindowItem.NewLayout(
						PComponents.Layout.Vertical.Fixed(
							false,
							true),
						WindowItem.LayoutConfig.FillLayout,
						new(){
						}).OnRealized((_, item) => {
							LoadOptionsLayout = item;
						})
				}
			),
			WindowItem.NewLayout(
				"Button Container",
				PComponents.Layout.Horizontal.Fixed(
					true,
					true,
					5
					),
				WindowItem.LayoutConfig.Custom(
					position: new(0, 0, 1, 0),
					sizeDelta: new(0, BottomOptionsHeight),
					fixedPosition: new() {
						Pivot = UIPosition.BottomCenter
						}
				),
				new() {
					WindowItem.NewButton(
						"Cancel",
						new PComponents.Button(Cancel),
						WindowItem.LayoutConfig.DynamicLayout(
							margin: new(10),
							position: new(0, .5f, 0, 0))
						).SetSubItems(
							WindowItem.NewText(
								new PComponents.Text(
									"Cancel",
									alignment: TextAlignmentOptions.Center
									),
								WindowItem.LayoutConfig.FillLayout)
						),
					WindowItem.NewButton(
						"Load",
						new PComponents.Button(Load),
						WindowItem.LayoutConfig.DynamicLayout(
							margin: new(10),
							position: new(0, 0, 0, .5f))
						).SetSubItems(
							WindowItem.NewText(
								new PComponents.Text(
									"Load",
									alignment: TextAlignmentOptions.Center
									),
								WindowItem.LayoutConfig.FillLayout)
						)
					}
				)
		}
	};

	public static CWindow[] Windows => new[] {
		NamePrompt.CWindow.SetGroup("saveload"),
		SaveStatus.SetGroup("saveload"),
		LoadOptionsMenu.SetGroup("saveload")
	};
	public static W[] Menus => new[] {
		NamePrompt
	};
}