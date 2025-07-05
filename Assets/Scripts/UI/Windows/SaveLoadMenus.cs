using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = PMenu.Window;
 
public class SaveLoadMenus {

	public static event Action OnSave;
	public static void Save() { OnSave?.Invoke(); }
	public static void ClearSave() { OnSave = null; }

	public static event Action OnSaveAs;
	public static void SaveAs() { OnSaveAs?.Invoke(); }
	public static void ClearSaveAs() { OnSaveAs = null; }


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
				WindowItem.NewButton(
					new PComponents.Button(() => OnNameEnterPressed?.Invoke()),
					PMenu.WindowItemLayout(220)
					).SetSubItems(
					WindowItem.NewText(
						new PComponents.Text(
							"Save!",
							fontSize: Config.UI.Menu.FontSize,
							alignment: TextAlignmentOptions.Center
							),
						WindowItem.LayoutConfig.FillLayout)
					)
				)
		},
		showTitle: true,
		isFlyout: false,
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
			.OnRealized((rt) => {
				
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
			.OnRealized((rt) => {
				SaveStatusText = rt.GetComponent<TextMeshProUGUI>();
			})
		}
	};

	static readonly W LoadOptionsMenu = new(
		"Load Options Menu",
		500,
		new(){

		}
		);

	public static CWindow[] Windows => new[] {
		NamePrompt.CWindow,
		SaveStatus,
		LoadOptionsMenu.CWindow
	};
}