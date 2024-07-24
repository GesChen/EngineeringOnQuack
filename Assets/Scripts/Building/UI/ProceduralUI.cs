using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProceduralUI : MonoBehaviour
{
	public string menuTitle;

	public Canvas canvas;
	public List<ProceduralUIComponent> components = new();

	[Space]
	[Header("Debug")]
	public bool mouseOver;
	public bool mouseInRange;
	public bool visible;
	public float width;
	public float height;
	public ProceduralUI dropdownParent;
	//public ProceduralUIComponent dropdownComponent;

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
		if (Controls.mousePos != Controls.lastMousePos)
		{
			MouseUpdate();

			foreach (ProceduralUIComponent component in components)
			{
				component.Update(this);
			}
		}
	}

	public void MouseUpdate()
	{
		Debug.Log("update");

		// hide this menu if the mouse is outside the range and the mouse is not in range of any active dropdowns
		// + is not dropdown and button which shows it is hovered
		mouseInRange = CheckMouseValidity(Config.UIConfig.MouseValidityMargin);
		if (!mouseInRange) 
		{ 
			// any of its own children dropdown aren't 
			bool anyDropDownsInRange = false;
			foreach (ProceduralUIComponent component in components)
			{
				if (component.IsDropDown && component.DropdownMenu.mouseInRange)
				{
					anyDropDownsInRange = true;
					break;
				}
			}

			if (!anyDropDownsInRange)
			{
				Hide();
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
		GameObject panel = Instantiate(Config.UIConfig.PanelPrefab, canvas.transform);
		panel.name = $"Panel ({menuTitle})";
		panelTransform = panel.GetComponent<RectTransform>();
		panel.GetComponent<Image>().color = Config.UIConfig.BackgroundColor;

		// add title
		ProceduralUIComponent titleComponent = new()
		{
			Text = menuTitle,
			Type = UIComponentType.text
		};
		components.Insert(0, titleComponent);

		// create all the items
		for (int i = 0; i < components.Count; i++)
		{
			ProceduralUIComponent component = components[i];
			GenerateUIComponent(ref component);
			/*if (component.IsDropDown)
			{
				if (component.DropdownMenu == null)
					Debug.LogError($"Dropdown {component.Text} has no required menu component");
				else
					component.DropdownMenu.dropdownComponent = component;
			}*/
			component.transform.SetParent(panelTransform);

			components[i] = component;
		}

		// setup the panel
		// width is based off the longest text in the menu
		float longestTextWidth = Mathf.NegativeInfinity;
		foreach (ProceduralUIComponent component in components)
		{
			if (component.type == UIComponentType.text || component.type == UIComponentType.button)
			{
				float textwidth = TextWidthApproximation(component.mainComponent.Text, Config.UIConfig.FontAsset, Config.UIConfig.FontSize) + 2 * Config.UIConfig.TextPadding;
				longestTextWidth = Mathf.Max(longestTextWidth, textwidth);
			}
		}

		width = longestTextWidth + Config.UIConfig.SidePadding * 2;
		height = Config.UIConfig.ItemHeight * components.Count + Config.UIConfig.VerticalSpacing * (components.Count - 1) + Config.UIConfig.SidePadding * 2;

		panelTransform.sizeDelta = new(width, height);

		grid = panel.GetComponent<GridLayoutGroup>();
		grid.cellSize = new(longestTextWidth, Config.UIConfig.ItemHeight);
		grid.spacing = new(0, Config.UIConfig.VerticalSpacing);
	}

	public void Display(Vector2 position, bool doOffset = true)
	{ // automatically tries to position the top left corner at position, adjust with offset
		visible = true;
		panelTransform.gameObject.SetActive(true);

		position += new Vector2(width, -height) / 2f; // place at top left corner
		if (doOffset) position += new Vector2(-Config.UIConfig.DisplayTopLeftCornerOffset.x, Config.UIConfig.DisplayTopLeftCornerOffset.y); // add offset

		// make sure it wont go out of the screen
		Vector2 screenSize = canvas.renderingDisplaySize;
		position.x = Mathf.Clamp(position.x, width / 2f + Config.UIConfig.MinDistFromSides, screenSize.x - width / 2f - Config.UIConfig.MinDistFromSides);
		position.y = Mathf.Clamp(position.y, height / 2f + Config.UIConfig.MinDistFromSides, screenSize.y - height / 2f - Config.UIConfig.MinDistFromSides);

		panelTransform.position = position;
	}

	public void Hide()
	{
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
		GameObject newObj = Instantiate(Config.UIConfig.ComponentPrefab);
		newObj.name = $"Item ({component.Text})";
		Image image = newObj.GetComponent<Image>();
		TextMeshProUGUI text = null;
		Button button = null;

		switch (component.Type)
		{
			case UIComponentType.button:
				image.color = Color.white;

				text = Instantiate(Config.UIConfig.TextPrefab, newObj.transform).GetComponent<TextMeshProUGUI>();
				text.text = component.Text;

				button = newObj.AddComponent<Button>();
				button.colors = new()
				{
					normalColor = Config.UIConfig.BackgroundColor,
					highlightedColor = Config.UIConfig.ButtonHoverColor,
					pressedColor = Config.UIConfig.ButtonPressedColor,
					colorMultiplier = 1f
				};
				button.onClick = component.OnClick;
				button.navigation = new() { mode = Navigation.Mode.None };

				//component.Init(this);

				break;
			case UIComponentType.text:
				image.color = Config.UIConfig.BackgroundColor;
				text = Instantiate(Config.UIConfig.TextPrefab, newObj.transform).GetComponent<TextMeshProUGUI>();
				text.text = component.Text;
				break;
			case UIComponentType.divider:
				image.color = Config.UIConfig.BackgroundColor;
				Instantiate(Config.UIConfig.DividerPrefab, newObj.transform);
				break;
		}
		if (component.Type == UIComponentType.text || component.Type == UIComponentType.button)
		{ // add text padding
			text.rectTransform.offsetMin = new(Config.UIConfig.TextPadding, Config.UIConfig.TextPadding);
			text.rectTransform.offsetMax = new(-Config.UIConfig.TextPadding, -Config.UIConfig.TextPadding);
		}

		RectTransform rectTransform = newObj.GetComponent<RectTransform>();
		component.mainComponent = component;
		component.rectTransform = rectTransform;
		component.transform = newObj.transform;
		component.imageComponent = image;
		component.type = component.Type;
		component.textmeshproText = text;
		component.button = button;

		//return component.reference;
	}

	public void HideAllDropdowns()
	{
		foreach(ProceduralUIComponent component in components)
		{
			if (component.IsDropDown)
			{
				component.HideDropdown();
			}
		}
	}
	
}