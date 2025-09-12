using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralScriptEditor {
	

	static int LineHeight = 20;

	int LineNumbersWidth = -1;
	WindowItem Line(int num, string contents) =>
		WindowItem.NewEmpty(
			WindowItem.LayoutConfig.LayoutElement(LineHeight * Vector2.one),
			new() {
				WindowItem.NewText( // line number
					new PComponents.Text(
						num.ToString(),
						alignment: TMPro.TextAlignmentOptions.Right
					),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.MiddleLeft),
						new(LineNumbersWidth, LineHeight))
				),
				WindowItem.NewText( // line content
					new PComponents.Text(
						contents
					),
					WindowItem.LayoutConfig.DynamicLayout(
						LineNumbersWidth * FourSides.LeftConst // modify the left spacing
					)
				)
			}
		);

	
}