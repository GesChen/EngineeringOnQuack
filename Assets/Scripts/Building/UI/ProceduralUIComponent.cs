using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public enum UIComponentType
{
	text,
	button,
	dropdown,
	divider
}

[Serializable]
public class ProceduralUIComponent // general container for components, doesnt actually do anything itself
{
	public UIComponentType Type;
	public string Text;
	public ProceduralUI DropdownMenu;
	public Button.ButtonClickedEvent OnClick = new();
	public Sprite icon;

	[HideInNormalInspector] public ProceduralUI main;
	[HideInNormalInspector] public Transform transform;
	[HideInNormalInspector] public RectTransform rectTransform;
	[HideInNormalInspector] public Image imageComponent;
	[HideInNormalInspector] public TextMeshProUGUI textmeshproText;
	[HideInNormalInspector] public Button button;

	[HideInNormalInspector] public bool mouseOver;

	public void Update()
	{
		try
		{
			mouseOver = HF.Vector2InRectTransform(Controls.mousePos, rectTransform) && main.visible;
		}
		catch
		{
			Debug.Log(rectTransform);
			Debug.Log(Text);
		}

		if (mouseOver)
		{
			// all other dropdowns in the parent should hide
			main.HideAllDropdowns();

			if (Type == UIComponentType.dropdown)
				DisplayDropdown(main);
		}

		if (Type == UIComponentType.dropdown)
			DropdownMenu.dropdownOverride = mouseOver;
	}

	public void DisplayDropdown(ProceduralUI main)
	{
		if (Type == UIComponentType.dropdown && DropdownMenu != null)
		{
			Vector3[] corners = new Vector3[4];
			rectTransform.GetWorldCorners(corners);

			// check if would fit at right
			bool wouldFit = corners[2].x + DropdownMenu.width + Config.UIConfig.DropDownDisplayOffset + Config.UIConfig.SidePadding < main.canvas.renderingDisplaySize.x;
			if (wouldFit)
			{ // place top left at top right (3)
				Vector2 position = (Vector2)corners[2] + Config.UIConfig.DropDownDisplayOffset * Vector2.right + Config.UIConfig.SidePadding * Vector2.one;
				DropdownMenu.Display(position, false);
			}
			else
			{
				// place top right corner of dropdown at top left (2)
				Vector2 position = 
					(Vector2)corners[1] + 
					(DropdownMenu.width + Config.UIConfig.DropDownDisplayOffset) * Vector2.left 
					+ new Vector2(-Config.UIConfig.SidePadding, Config.UIConfig.SidePadding);
				DropdownMenu.Display(position, false);
			}
		}
	}

	public void HideDropdown()
	{
		if (Type == UIComponentType.dropdown &&
			DropdownMenu != null)
		{
			DropdownMenu.Hide();
		}
	}
}