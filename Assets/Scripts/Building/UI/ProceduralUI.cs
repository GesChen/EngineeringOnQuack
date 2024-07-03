using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ProceduralUI : MonoBehaviour
{
	[Header("Settings")]
	public string menuTitle;
	[Space]
	public Color backgroundColor;
	public Color buttonHoverColor;
	public Color buttonPressedColor;
	public Color textColor;
	[Space]
	public int itemHeight;
	public int verticalSpacing;
	public int sidePadding;
	public int textPadding;
	[Space]
	public int fontSize;
	public TMP_FontAsset fontAsset;
	public Vector2 displayTopLeftCornerOffset;
	public float minDistFromSides;
	[Space]
	public float dropDownArrowRightOffset;
	public float dropDownArrowSize;
	public float dropDownDisplayOffset;

	[Header("Technical")]
	public GameObject panelPrefab;
	public GameObject componentPrefab;
	public GameObject textPrefab;
	public GameObject dividerPrefab;
	public GameObject dropDownArrow;

	public Canvas canvas;
	public List<ProceduralUIComponent> components = new();
	private List<UIComponentReference> references = new();

	private RectTransform panelTransform;
	private GridLayoutGroup grid;
	float width;
	float height;

	void Start()
	{
		// generate everything and then hide it
		Generate();
		Hide();
	}

	void Generate()
	{
		// create the panel
		GameObject panel = Instantiate(panelPrefab, canvas.transform);
		panel.name = $"Panel ({menuTitle})";
		panelTransform = panel.GetComponent<RectTransform>();
		panel.GetComponent<Image>().color = backgroundColor;

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
			UIComponentReference reference = GenerateUIComponent(component);
			component.reference = reference;

			components[i] = component;
			references.Add(reference);
			reference.transform.SetParent(panelTransform);
		}

		// setup the panel
		// width is based off the longest text in the menu
		float longestTextWidth = Mathf.NegativeInfinity;
		foreach (UIComponentReference reference in references)
		{
			if (reference.type == UIComponentType.text || reference.type == UIComponentType.button)
			{
				float textwidth = TextWidthApproximation(reference.mainComponent.Text, fontAsset, fontSize) + 2 * textPadding;
				longestTextWidth = Mathf.Max(longestTextWidth, textwidth);
			}
		}

		width = longestTextWidth + sidePadding * 2;
		height = itemHeight * components.Count + verticalSpacing * (components.Count - 1) + sidePadding * 2;

		panelTransform.sizeDelta = new(width, height);

		grid = panel.GetComponent<GridLayoutGroup>();
		grid.cellSize = new(longestTextWidth, itemHeight);
		grid.spacing = new(0, verticalSpacing);
	}

	public void Display(Vector2 position, bool doOffset = true)
	{ // automatically tries to position the top left corner at position, adjust with offset
		panelTransform.gameObject.SetActive(true);

		position += new Vector2(width, -height) / 2f; // place at top left corner
		if(doOffset) position += new Vector2(-displayTopLeftCornerOffset.x, displayTopLeftCornerOffset.y); // add offset

		// make sure it wont go out of the screen
		Vector2 screenSize = canvas.renderingDisplaySize;
		position.x = Mathf.Clamp(position.x, width / 2f + minDistFromSides, screenSize.x - width / 2f - minDistFromSides);
		position.y = Mathf.Clamp(position.y, height / 2f + minDistFromSides, screenSize.y - height / 2f - minDistFromSides);

		panelTransform.position = position;
	}

	public void Hide()
	{
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

	public UIComponentReference GenerateUIComponent(ProceduralUIComponent component)
	{
		GameObject newObj = Instantiate(componentPrefab);
		Image image = newObj.GetComponent<Image>();	
		TextMeshProUGUI text = null;
		Button button = null;
		switch (component.Type)
		{
			case UIComponentType.button:
				image.color = Color.white;
				text = Instantiate(textPrefab, newObj.transform).GetComponent<TextMeshProUGUI>();
				text.text = component.Text;
				button = newObj.AddComponent<Button>();
				button.colors = new()
				{
					normalColor = backgroundColor,
					highlightedColor = buttonHoverColor,
					pressedColor = buttonPressedColor,
					colorMultiplier = 1f
				};
				if (component.IsDropDown)
				{
					EventTrigger.Entry pointerEnter = new() { eventID = EventTriggerType.PointerEnter };
					pointerEnter.callback.AddListener((_) => { DisplayDropdown(component); });

					EventTrigger.Entry pointerExit = new() { eventID = EventTriggerType.PointerExit };
					pointerExit.callback.AddListener((_) => { HideDropdown(component); });

					EventTrigger eventTrigger = button.gameObject.AddComponent<EventTrigger>();
					eventTrigger.triggers.Add(pointerEnter);
					eventTrigger.triggers.Add(pointerExit);
				}

				button.onClick = component.OnClick;
				button.navigation = new() { mode = Navigation.Mode.None };
				
				break;
			case UIComponentType.text:
				image.color = backgroundColor;
				text = Instantiate(textPrefab, newObj.transform).GetComponent<TextMeshProUGUI>();
				text.text = component.Text;
				break;
			case UIComponentType.divider:
				image.color = backgroundColor;
				Instantiate(dividerPrefab, newObj.transform);
				break;
		}
		if (component.Type == UIComponentType.text || component.Type == UIComponentType.button)
		{ // add text padding
			text.rectTransform.offsetMin = new(textPadding, textPadding);
			text.rectTransform.offsetMax = new(-textPadding, -textPadding);
		}

		component.reference = new()
		{
			mainComponent = component,
			rectTransform = newObj.GetComponent<RectTransform>(),
			transform = newObj.transform,
			imageComponent = image,
			type = component.Type,
			text = text,
			button = button
		};

		return component.reference;
	}

	public void DisplayDropdown(ProceduralUIComponent component)
	{
		ProceduralUI dropdown = component.DropdownMenu;
		if (component.Type == UIComponentType.button && dropdown != null)
		{
			Vector3[] corners = new Vector3[4];
			component.reference.rectTransform.GetWorldCorners(corners);

			// multiple conditions for placing this panel
			// normal condition: place top left corner of dropdown at top right of button
			// if placing at top left won't fit, place top right corner of dropdown at top left of button
			// let the display script handle vertical issues

			// check if would fit at right
			bool wouldFit = corners[2].x + dropdown.width + dropDownDisplayOffset + sidePadding < canvas.renderingDisplaySize.x;
			if (wouldFit)
			{ // place top left at top right (3)
				Vector2 position = (Vector2)corners[2] + dropDownDisplayOffset * Vector2.right + sidePadding * Vector2.one;
				dropdown.Display(position, false);
			}
			else
			{
				// place top right corner of dropdown at top left (2)
				Vector2 position = (Vector2)corners[1] + (dropdown.width + dropDownDisplayOffset) * Vector2.left + new Vector2(-sidePadding, sidePadding);
				dropdown.Display(position, false);
			}
		}
	}

	public void HideDropdown(ProceduralUIComponent component)
	{
		if (component.Type == UIComponentType.button && component.DropdownMenu != null)
		{
			component.DropdownMenu.Hide();
		}
	}
}
