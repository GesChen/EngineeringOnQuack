using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Transceiver_UI {
	public static Func<string[]> RequestOutputs;
	public static Func<int> InitialSelection;
	public static void OnSetOutput() {
		UpdateOutputs();

		OutputSelectionWindow.RealisedWindow.Show();
	}

	public static void UpdateOutputs() {
		if (RequestOutputs == null) throw new("RequestOuputs not subscribed to!");
		var outs = RequestOutputs();

		SelectionsLayout.SetSubItems(outs.Select(OutputItem).ToArray());

		var state = OutputSelectionWindow.RealisedWindow.Active;

		WindowRealiser.Instance.UpdateWindow(OutputSelectionWindow);

		OutputSelectionWindow.RealisedWindow.SetState(state);

		if (InitialSelection == null) throw new("InitialSelection not subscribed to!");
		var initial = InitialSelection();
		SelectItem(initial);
	}

	public static void SelectItem(int i) {
		OnItemSelected?.Invoke(i);

		foreach (var kvp in ToggleIcons) {
			kvp.Value.sprite = kvp.Key == i ? OnIcon : OffIcon;
		}
	}

	static readonly Dictionary<int, Image> ToggleIcons = new();
	public static Action<int> OnItemSelected;

	public static Sprite m_OnIcon;
	public static Sprite OnIcon = HF.LoadResource(ref m_OnIcon, Config.UI.Sprites.RadioButtonOn);

	public static Sprite m_OffIcon;
	public static Sprite OffIcon = HF.LoadResource(ref m_OffIcon, Config.UI.Sprites.RadioButtonOff);

	public static WindowItem OutputItem(string name, int i) =>
		WindowItem.NewButton(
			new PComponents.Button(() => SelectItem(i)),
			WindowItem.LayoutConfig.LayoutElement(
				Config.UI.Menu.ItemHeight * Vector2.one,
				new(Config.UI.Menu.ItemPadding)
			)
		).SetSubItems(
			WindowItem.NewImage( // indicator icon
				new PComponents.Image(
					OffIcon
				),
				WindowItem.LayoutConfig.FixedLayout(
					UIPosition.AnchoredAt(UIPosition.MiddleLeft),
					Config.UI.Menu.IconSize * Vector2.one
				)
			).OnRealized((rt, _) =>
				ToggleIcons[i] = rt.GetComponent<Image>()
			),
			WindowItem.NewText( // label
				new PComponents.Text(name),
				WindowItem.LayoutConfig.DynamicLayout(
					margin: new FourSides(0, 0, 0, Config.UI.Menu.IconSize + Config.UI.Menu.IconLabelSpacing)
				)
			)
		);

	public static WindowItem SelectionsLayout;
	public static CWindow OutputSelectionWindow;
	public static void SetOutputSelectionWindow() {
		OutputSelectionWindow = new() {
			Name = "Output Selection",
			Config = new() {
				Size = CWindow.Configuration.FreeSizeMinimum(
					new(250, 200),
					new(250, 100))
			},
			Items = new WindowItem[] {
				WindowItem.NewText(
					new PComponents.Text(
						"Set Transceiver Output",
						alignment:TMPro.TextAlignmentOptions.Center
					),
					WindowItem.LayoutConfig.Custom(
						position: new(1, 0, 0, 0),
						sizeDelta: new(0, 30),
						fixedPosition: new() {
							Pivot = UIPosition.TopCenter
						}
					)
				),
				WindowItem.NewScrollView(
					new PComponents.ScrollView(
						horizontalScrolling: false
					),
					WindowItem.LayoutConfig.Custom(
							margins: new(30, 0, 0, 0)
					),
					new() {
						WindowItem.NewLayout(
							PComponents.Layout.Vertical.Fixed(
								false,
								true
							),
							WindowItem.LayoutConfig.Custom(
								position: new(1, 0, 0, 0),
								sizeDelta: new(0, 0)
							),
							new()
						).OnRealized((_, wi) =>
							SelectionsLayout = wi
						)
					}
				)
			}
		};
	}

	public static void Set() {
		SetOutputSelectionWindow();
	}

	public static CWindow[] Windows => new CWindow[] {
		OutputSelectionWindow
	};
}