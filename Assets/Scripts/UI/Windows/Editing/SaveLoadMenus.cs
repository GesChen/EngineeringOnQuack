using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = PMenu.Window;

public class SaveLoadMenus {

	public static Action OnSave;
	public static void Save() { OnSave?.Invoke(); }

	public static Action OnSaveAs;
	public static void SaveAs() { OnSaveAs?.Invoke(); }

	public static void ShowNamePrompt(Action<string> nameCallback) {
		NamePrompt.CWindow.RealisedWindow.PlaceAtCenter();
		NamePrompt.CWindow.RealisedWindow.Show();

		SaveStatusText.text = "";

		OnNameEnterPressed = null;
		OnNameEnterPressed += () => nameCallback?.Invoke(PromptedName);
	}
	public static void HideNamePrompt() {
		NamePrompt.CWindow.RealisedWindow.Hide();
	}

	public static void ShowLoadMenu() {
		FileExplorer.CreateNewFE(
			Config.Building.Saving.AssembliesLocation
			, new(
				FileExplorer.Type.OpenFile,
				new[] { Config.Building.Saving.SaveExtension },
				FileExplorer.MetadataGetters.GetBytes,
				"Load",
				Load,
				5)
		);
	}

	public static void ShowSaveIcon() {
		SaveStatus.RealisedWindow.Show();
	}
	public static void HideSaveIcon() {
		// may have been destroyed from context change so hidden already
		if (!SaveStatus.RealisedExists) return;

		SaveStatus.RealisedWindow.Hide();
	}
	public static void SetSaveText(string text) {
		SaveStatusText.text = text;
	}

	static string PromptedName;
	//static Image SaveStatusImage;
	static TextMeshProUGUI SaveStatusText;
	static event Action OnNameEnterPressed;

	static W NamePrompt;
	static void SetNamePrompt() {
		NamePrompt = new(
			"Name Your Creation!", 220, false, new(){
				new W.InputField(
					(value) => PromptedName = value,
					"Enter name here..."),
				new W.CustomItem(
					WindowItem.NewButtonCustomText(
						new PComponents.Button(() => OnNameEnterPressed?.Invoke()),
						new PComponents.Text(
							"Save!",
							TextAlignmentOptions.Center,
							fontSize: Config.UI.Menu.FontSize
						),
						PMenu.WindowItemLayout(220)
					)
				)
			},
			showTitle: true,
			closable: true,
			extraSpacing: 5
			);
	}

	static readonly float imageSize = 50;
	static readonly float textHeight = 20;
	static readonly float margin = 5;

	static CWindow SaveStatus;
	public static void SetSaveStatus() {
		SaveStatus = new() {
			Name = "Save Status",
			Config = new() {
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
							TextAlignmentOptions.Center,
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
	}

	public static Action<string> OnLoad;
	static void Load(string path) {
		try {
			OnLoad?.Invoke(path);
		} catch (Exception e) {
			PDialog.GenerateDialog(
				new PDialog(
					$"An error occurred while loading:\n{e.Message}",
					new PDialog.Option[] {
						new("Ok", null)
					},
					new(300, 200)
			));
		}
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
	/*static (float left, float right)[] LeftAndRights(float[] spacings) {
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
	}*/

	public static void Set() {
		SetNamePrompt();
		SetSaveStatus();
	}
	public static CWindow[] Windows => new[] {
		NamePrompt.CWindow.SetGroup("saveload"),
		SaveStatus.SetGroup("saveload"),
	};
	public static W[] Menus => new[] {
		NamePrompt
	};
}