//#define DEBUGMODE

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ProceduralUI : MonoBehaviour
{
	public string menuTitle;
	public bool addTitle = true;

	public Canvas canvas;
	public List<ProceduralUIComponent> components = new();

	[Space]
	[HideInNormalInspector] public bool mouseOver;
	[HideInNormalInspector] public bool mouseInRange;
	[HideInNormalInspector] public bool visible;
	[HideInNormalInspector] public float width;
	[HideInNormalInspector] public float height;
	[HideInNormalInspector] public bool dropdownOverride;
	[HideInNormalInspector] public bool anyDropDownsInRange;

	private RectTransform panelTransform;
	private GridLayoutGroup grid;
	
	void Start()
	{
		// generate everything and then hide it
		Generate();
		Hide();
	}

	void Update()
	{
		if (Controls.mousePos != Controls.lastMousePos || Mouse.current.rightButton.isPressed)
		{
			MouseUpdate();

			foreach (ProceduralUIComponent component in components)
			{
				component.Update();
			}
		}
	}

	public void MouseUpdate()
	{
		// hide this menu if the mouse is outside the range and the mouse is not in range of any active dropdowns
		// + is not dropdown and button which shows it is hovered
		mouseInRange = CheckMouseValidity(Config.UI.MouseValidityMargin);

		anyDropDownsInRange = false;
		CheckComponentsForInRange(this, ref anyDropDownsInRange);

#if DEBUGMODE
		HF.LogColor($"{menuTitle}: inrange {mouseInRange}", Color.cyan); 
		Debug.Log($"{menuTitle}: any {anyDropDownsInRange}");
#endif
		if (!mouseInRange && !anyDropDownsInRange && !dropdownOverride)
		{
			Hide();
		}
	}

	// recursively checks components for dropdowns with mouse in range of the menu
	void CheckComponentsForInRange(ProceduralUI procedural, ref bool inRange)
	{
		if (inRange) return;

		foreach (ProceduralUIComponent component in procedural.components)
		{
			if (component.Type == UIComponentType.dropdown && component.DropdownMenu.visible)
			{
				if (component.DropdownMenu.mouseInRange)
				{
					inRange = true;
					return;
				}
				CheckComponentsForInRange(component.DropdownMenu, ref inRange);
			}
		}
	}

	// used to see if should hide component when mouse leaves, 
	// hover over buttons should use normal
	public bool CheckMouseValidity(int margin)
	{
		Vector2 mousePos = Controls.mousePos;

		Vector3[] corners = new Vector3[4];
		panelTransform.GetWorldCorners(corners);
		Vector2 min = corners[0];
		Vector2 max = corners[2];

		// expanding by margin achieves same effect
		min -= margin * Vector2.one;
		max += margin * Vector2.one;

		return mousePos.x < max.x && mousePos.y < max.y && mousePos.x > min.x && mousePos.y > min.y;
	}

	void Generate()
	{
		// create the panel
		GameObject panel = Instantiate(Config.UI.PanelPrefab, canvas.transform);
		panel.name = $"Panel ({menuTitle})";
		panelTransform = panel.GetComponent<RectTransform>();
		panel.GetComponent<Image>().color = Config.UI.BackgroundColor;

		// outline
		Outline outline = panel.GetComponent<Outline>();
		outline.effectColor = Config.UI.OutlineColor;
		outline.effectDistance = Config.UI.OutlineThickness * Vector2.one;

		// add title
		if (addTitle)
		{
			ProceduralUIComponent titleComponent = new()
			{
				Text = menuTitle,
				Type = UIComponentType.text
			};
			components.Insert(0, titleComponent);
		}

		// create all the items
		for (int i = 0; i < components.Count; i++)
		{
			ProceduralUIComponent component = components[i];
			GenerateUIComponent(ref component);
			if (component.Type == UIComponentType.dropdown && component.DropdownMenu == null)
			{
				Debug.LogError($"Dropdown {component.Text} has no required menu component");
			}
			component.transform.SetParent(panelTransform);

			components[i] = component;
		}

		// setup the panel
		// width is based off the longest text in the menu
		float longestTextWidth = Mathf.NegativeInfinity;
		foreach (ProceduralUIComponent component in components)
		{
			if (component.Type == UIComponentType.text || 
				component.Type == UIComponentType.button || 
				component.Type == UIComponentType.dropdown)
			{
				float textwidth = TextWidthApproximation(component.Text, Config.UI.FontAsset, Config.UI.FontSize) + 20;
				longestTextWidth = Mathf.Max(longestTextWidth, textwidth);
			}
		}

		width = longestTextWidth + Config.UI.SidePadding * 6 + Config.UI.IconSize + Config.UI.DropDownArrowSize;
		height = Config.UI.ItemHeight * components.Count + Config.UI.VerticalSpacing * (components.Count - 1) + Config.UI.SidePadding * 2;

		panelTransform.sizeDelta = new(width, height);

		grid = panel.GetComponent<GridLayoutGroup>();
		float cellWidth = longestTextWidth + Config.UI.InsidePadding * 2 + Config.UI.IconSize + Config.UI.DropDownArrowSize;
		grid.cellSize = new(cellWidth, Config.UI.ItemHeight);
		grid.spacing = new(0, Config.UI.VerticalSpacing);
	}

	public void Display(Vector2 position, bool doOffset = true)
	{ // automatically tries to position the top left corner at position, adjust with offset
#if DEBUGMODE
		HF.LogColor($"{menuTitle} displaying", Color.green);
#endif

		visible = true;
		panelTransform.gameObject.SetActive(true);

		position += new Vector2(width, -height) / 2f; // place at top left corner
		if (doOffset) position += new Vector2(-Config.UI.DisplayTopLeftCornerOffset.x, Config.UI.DisplayTopLeftCornerOffset.y); // add offset

		// make sure it wont go out of the screen
		Vector2 screenSize = canvas.renderingDisplaySize;
		position.x = Mathf.Clamp(position.x, width / 2f + Config.UI.MinDistFromSides, screenSize.x - width / 2f - Config.UI.MinDistFromSides);
		position.y = Mathf.Clamp(position.y, height / 2f + Config.UI.MinDistFromSides, screenSize.y - height / 2f - Config.UI.MinDistFromSides);

		panelTransform.position = position;
	}

	public void Hide()
	{
#if DEBUGMODE
		HF.LogColor($"{menuTitle} hiding", Color.red);
#endif
		visible = false;
		panelTransform.gameObject.SetActive(false);
	}

	public float TextWidthApproximation(string text, TMP_FontAsset fontAsset, int fontSize)
	{
		// Compute scale of the target point size relative to the sampling point size of the font asset.
		float pointSizeScale = fontSize / (fontAsset.faceInfo.pointSize * fontAsset.faceInfo.scale);
		float emScale = fontSize * 0.01f;

		float styleSpacingAdjustment = 0; // (style & FontStyles.Bold) == FontStyles.Bold ? fontAsset.boldSpacing : 0;
		float normalSpacingAdjustment = fontAsset.normalSpacingOffset;

		float width = 0;

		for (int i = 0; i < text.Length; i++)
		{
			char unicode = text[i];
			// Make sure the given unicode exists in the font asset.
			if (fontAsset.characterLookupTable.TryGetValue(unicode, out TMP_Character character))
				width += character.glyph.metrics.horizontalAdvance * pointSizeScale + (styleSpacingAdjustment + normalSpacingAdjustment) * emScale;
		}

		return width;
	}

	public void GenerateUIComponent(ref ProceduralUIComponent component)
	{
		GameObject newObj = Instantiate(Config.UI.ComponentPrefab);
		newObj.name = $"Item ({component.Text})";
		Image image = newObj.GetComponent<Image>();
		TextMeshProUGUI text = null;
		Button button = null;

		switch (component.Type)
		{
			case UIComponentType.dropdown:
			case UIComponentType.button:
				image.color = Color.white;

				text = Instantiate(Config.UI.TextPrefab, newObj.transform).GetComponent<TextMeshProUGUI>();
				text.text = component.Text;

				button = newObj.AddComponent<Button>();
				button.colors = new()
				{
					normalColor = Config.UI.BackgroundColor,
					highlightedColor = Config.UI.ButtonHoverColor,
					pressedColor = Config.UI.ButtonPressedColor,
					colorMultiplier = 1f
				};
				button.onClick = component.OnClick;
				button.navigation = new() { mode = Navigation.Mode.None };

				break;
			case UIComponentType.text:
				image.color = Config.UI.BackgroundColor;
				text = Instantiate(Config.UI.TextPrefab, newObj.transform).GetComponent<TextMeshProUGUI>();
				text.text = component.Text;
				break;
			case UIComponentType.divider:
				image.color = Config.UI.BackgroundColor;
				Instantiate(Config.UI.DividerPrefab, newObj.transform);
				break;
		}
		if (component.Type == UIComponentType.text || component.Type == UIComponentType.button || component.Type == UIComponentType.dropdown)
		{ // add text padding
			float leftOffset = Config.UI.InsidePadding * 3 + Config.UI.IconSize;
			float rightOffset = Config.UI.InsidePadding * 3 + Config.UI.DropDownArrowSize;
			text.rectTransform.offsetMin = new(leftOffset, Config.UI.InsidePadding);
			text.rectTransform.offsetMax = new(-rightOffset, -Config.UI.InsidePadding);
		}

		if (component.icon != null)
		{
			GameObject iconObj = Instantiate(Config.UI.IconPrefab, newObj.transform);
			Image iconComponent = iconObj.GetComponent<Image>();
			iconComponent.sprite = component.icon;

			RectTransform iconRect = iconObj.GetComponent<RectTransform>();
			iconRect.localPosition = new(Config.UI.InsidePadding + Config.UI.IconSize / 2, Config.UI.ItemHeight / 2);
			iconRect.sizeDelta = Config.UI.IconSize * Vector2.one;
		}

		if (component.Type == UIComponentType.dropdown)
		{
			GameObject dropdownIconObj = Instantiate(Config.UI.DropDownArrow, newObj.transform);
			Image iconComponent = dropdownIconObj.GetComponent<Image>();
			iconComponent.sprite = Config.UI.DropDownClosedSprite;

			RectTransform iconRect = dropdownIconObj.GetComponent<RectTransform>();
			iconRect.localPosition = new(-(Config.UI.InsidePadding + Config.UI.DropDownArrowSize / 2), Config.UI.ItemHeight / 2);
			iconRect.sizeDelta = Config.UI.DropDownArrowSize * Vector2.one;

			component.dropdownArrowImage = iconComponent;
		}

		RectTransform rectTransform = newObj.GetComponent<RectTransform>();
		component.rectTransform = rectTransform;
		component.transform = newObj.transform;
		component.imageComponent = image;
		component.textmeshproText = text;
		component.button = button;

		component.main = this;

		//return component.reference;
	}

	public void HideAllDropdowns()
	{
		foreach(ProceduralUIComponent component in components)
		{
			if (component.Type == UIComponentType.dropdown)
			{
				component.HideDropdown();
			}
		}
	}
	
}