using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
	public string Description;
	public Sprite icon;
	public ProceduralUI DropdownMenu;
	public Button.ButtonClickedEvent OnClick = new();

	[HideInNormalInspector] public ProceduralUI main;
	[HideInNormalInspector] public Transform transform;
	[HideInNormalInspector] public RectTransform rectTransform;
	[HideInNormalInspector] public Image imageComponent;
	[HideInNormalInspector] public TextMeshProUGUI textmeshproText;
	[HideInNormalInspector] public Button button;
	[HideInNormalInspector] public Image dropdownArrowImage;
	[HideInNormalInspector] public Transform descriptionTransform;

	[HideInNormalInspector] public bool mouseOver;

	bool lastMouseOver;
	float startOverTime;
	public void Update()
	{
		try
		{
			mouseOver = HF.Vector2InRectTransform(Controls.mousePos, rectTransform) && main.visible && !main.anyDropDownsInRange;
		}
		catch
		{
			Debug.Log(rectTransform);
			Debug.Log(Text);
		}

		if (mouseOver && lastMouseOver != mouseOver)
			startOverTime = Time.time;

		if (mouseOver)
		{
			// all other dropdowns in the parent should hide
			main.HideAllDropdowns();

			if (Type == UIComponentType.dropdown)
				DisplayDropdown(main);
		}
		if (!string.IsNullOrWhiteSpace(Description))
		{
			if (mouseOver && Time.time - startOverTime > Config.UI.HoverTimeUntilDescription)
				ShowDescription();
			else
				HideDescription();
		}

		if (Type == UIComponentType.dropdown)
		{
			DropdownMenu.dropdownOverride = mouseOver;

			if (mouseOver || ((DropdownMenu.mouseInRange || DropdownMenu.anyDropDownsInRange) && DropdownMenu.visible))
				dropdownArrowImage.sprite = Config.UI.DropDownOpenedSprite;
			else
				dropdownArrowImage.sprite = Config.UI.DropDownClosedSprite;
		}

		lastMouseOver = mouseOver;
	}

	public void DisplayDropdown(ProceduralUI main)
	{
		if (Type == UIComponentType.dropdown && DropdownMenu != null)
		{
			Vector3[] corners = new Vector3[4];
			rectTransform.GetWorldCorners(corners);

			// check if would fit at right
			bool wouldFit = corners[2].x + DropdownMenu.width + Config.UI.DropDownDisplayOffset + Config.UI.SidePadding < main.canvas.renderingDisplaySize.x;
			if (wouldFit)
			{ // place top left at top right (3)
				Vector2 position = (Vector2)corners[2] + Config.UI.DropDownDisplayOffset * Vector2.right + Config.UI.SidePadding * Vector2.one;
				DropdownMenu.Display(position, false);
			}
			else
			{
				// place top right corner of dropdown at top left (2)
				Vector2 position =
					(Vector2)corners[1] +
					(DropdownMenu.width + Config.UI.DropDownDisplayOffset) * Vector2.left
					+ new Vector2(-Config.UI.SidePadding, Config.UI.SidePadding);
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

	public void ShowDescription()
	{
		descriptionTransform.gameObject.SetActive(true);

		Vector2 newLocal = descriptionTransform.localPosition;
		newLocal.y = -Config.UI.ItemHeight / 2 - Config.UI.DescriptionHeight / 2;
		descriptionTransform.localPosition = newLocal;

		Vector3 newGlobal = descriptionTransform.position;
		newGlobal.x = (transform.position.x + Mouse.current.position.value.x) / 2;
		newGlobal.z = 10;
		descriptionTransform.position = newGlobal;
	}

	public void HideDescription()
	{
		descriptionTransform.gameObject.SetActive(false);
	}
}