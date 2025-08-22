using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class UIBarUtils {
	public static WindowItem DynamicBarFlyout(float width, string label, CWindow target, (bool right, bool up) openDirection) =>
		WindowItem.NewFlyoutTrigger(
			label,
			new PComponents.FlyoutTrigger(
				target,
				openHorizontally: false,
				openPrioritizingRight: openDirection.right,
				openPrioritizingUp: openDirection.up
				),
			WindowItem.LayoutConfig.LayoutElementDynamic()
		).SetSubItems(
		WindowItem.NewText(
			new PComponents.Text(
				label,
				alignment: TextAlignmentOptions.Center),
			WindowItem.LayoutConfig.FillLayout
			)
		).AddComponents(
			new PComponents.LayoutElement(width)
		);

	public static WindowItem DynamicBarSpace(float width) =>
		WindowItem.NewEmpty(
			WindowItem.LayoutConfig.LayoutElementDynamic()
		).AddComponents(
			new PComponents.LayoutElement(width)
		);

	public static WindowItem DynamicBarText(float width, string text, float bgopacity) =>
		WindowItem.NewImage(
				new PComponents.Image(
					Config.UI.Visual.BackgroundColor *
						new Color(1, 1, 1, bgopacity)
				),
				WindowItem.LayoutConfig.LayoutElementDynamic()
		).SetSubItems(
			WindowItem.NewText(
				new PComponents.Text(
					text,
					alignment: TextAlignmentOptions.Center),
				WindowItem.LayoutConfig.FillLayout
			)
		).AddComponents(
			new PComponents.LayoutElement(width)
		);

	public static WindowItem DynamicBarButton(float width, string label, Action target) =>
		WindowItem.NewButtonCustomText(
			new PComponents.Button(target),
			new PComponents.Text(
				label,
				alignment: TextAlignmentOptions.Center
			),
			WindowItem.LayoutConfig.LayoutElementDynamic()
		).AddComponents(
			new PComponents.LayoutElement(width)
		);

	public static WindowItem DynamicBarInputField(float width, string placeholder, float bgopacity, Action<string> onValueChanged) =>
		WindowItem.NewImage(
				new PComponents.Image(
					Config.UI.Visual.BackgroundColor *
						new Color(1, 1, 1, bgopacity)
				),
				WindowItem.LayoutConfig.LayoutElementDynamic()
		).SetSubItems(
			WindowItem.NewInputField(
				new PComponents.InputField(
					onValueChanged,
					placeholder,
					alignment: TextAlignmentOptions.Center
				),
				WindowItem.LayoutConfig.FillLayout
			)
		).AddComponents(
			new PComponents.LayoutElement(width)
		);


}