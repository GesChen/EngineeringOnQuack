using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PControl{
	static readonly float Size = 30;

	public string Label;
	public char Key; // change later as needed with modifiers n whatnot

	public PControl(string label, char key) {
		Label = label;
		Key = key;
	}

	public CWindow ToWindow() {
		float labelWidth = HF.TextWidthExact(Label, Config.UI.Visual.DefaultFont, Config.UI.Visual.FontSize).x;

		return new() {
			Name = $"{Label} control",
			Config = new() {
				Resizable = false,
				Movable = false,
				Size = CWindow.Configuration.FixedSize(new(labelWidth + Config.UI.Menu.IconLabelSpacing * 2 + Size, Size)),
				Closable = false
			},
			Items = new WindowItem[] {
				WindowItem.NewText(
					new PComponents.Text(
						Label,
						TMPro.TextAlignmentOptions.Left
					),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredOffset(
							UIPosition.TopLeft,
							new(Config.UI.Menu.IconLabelSpacing, 0)
						),
						new(labelWidth, Size)
					)
				),
				WindowItem.NewImage(
					new PComponents.Image("Icons/Keys/squareframe"),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.TopRight),
						Size * Vector2.one
					)
				).SetSubItems(
					WindowItem.NewImage(
						new PComponents.Image("Icons/Keys/" + Key),
						WindowItem.LayoutConfig.FillLayout
					)
				)
			}
		};
	}
}