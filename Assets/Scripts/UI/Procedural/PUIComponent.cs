using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;


[Serializable]
public class PUIComponent // general container for components, doesnt actually do anything itself
{
	[Serializable]
	public struct IconNamePair {
		public string Name;
		public Sprite Icon;
	}

	public enum ComponentType {
		text,
		button,
		dropdown,
		divider
	}

	public ComponentType Type;
	public string Text;
	public string Description;
	public Sprite icon; // rename this once ported over
	public ProceduralUI DropdownMenu;
	public PUIPanel DropdownMenuPanel; // must be realised into dropdownmenu 
	public Button.ButtonClickedEvent OnClick = new();

	[HideInNormalInspector] public bool mouseOver;
	[HideInNormalInspector] public ProceduralUI main;
	[HideInNormalInspector] public Transform transform;
	[HideInNormalInspector] public RectTransform rectTransform;
	[HideInNormalInspector] public Image imageComponent;
	[HideInNormalInspector] public TextMeshProUGUI textmeshproText;
	[HideInNormalInspector] public Button button;
	[HideInNormalInspector] public Image dropdownArrowImage;

	[HideInNormalInspector] public ComponentDescription description;
	[HideInNormalInspector] public int descriptionBoollistIndex;

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

			if (Type == ComponentType.dropdown)
				DisplayDropdown(main);
		}
		if (!string.IsNullOrWhiteSpace(Description))
		{
			if (mouseOver)
			{
				if (Time.time - startOverTime > Config.UI.HoverTimeUntilDescription)
					ShowDescription();
			}
			else
				HideDescription();
		}

		if (Type == ComponentType.dropdown)
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
		if (Type == ComponentType.dropdown && DropdownMenu != null)
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
		if (Type == ComponentType.dropdown &&
			DropdownMenu != null)
		{
			DropdownMenu.Hide();
		}
	}

	public void ShowDescription()
	{
		main.PrimeDescription(this);
		description.any[descriptionBoollistIndex] = true;

		float yOffset = Config.UI.ItemHeight / 2 + Config.UI.DescriptionHeight / 2;
		Vector2 newGlobal = new (
			(transform.position.x + Mouse.current.position.value.x) / 2,
			transform.position.y - yOffset);

		description.mainObject.transform.position = newGlobal;
	}

	public void HideDescription()
	{
		description.any[descriptionBoollistIndex] = false;
	}

	#region constructors
	public PUIComponent(
		ComponentType type, 
		string text, 
		string desc, 
		Sprite icon, 
		PUIPanel dropdown, 
		Button.ButtonClickedEvent onClick) {

		Type = type;
		Text = text;
		Description = desc;
		this.icon = icon;
		DropdownMenuPanel = dropdown;
		OnClick = onClick;
	}
	public static PUIComponent NewText(string text, string desc, Sprite icon = null) {
		return new(ComponentType.text, text, desc, icon, null, null);
	}
	public static PUIComponent NewButton(string text, string desc, Button.ButtonClickedEvent onClick, Sprite icon = null) {
		return new(ComponentType.button, text, desc, icon, null, onClick);
	}
	public static PUIComponent NewDropdown(string text, string desc, PUIPanel dropdown, Sprite icon = null) {
		return new(ComponentType.dropdown, text, desc, icon, dropdown, null);
	}
	public static PUIComponent NewDivider() {
		return new(ComponentType.divider, "", "", null, null, null);
	}
	
		
	#endregion
}