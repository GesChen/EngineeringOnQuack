using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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


	[Header("Technical")]
	public GameObject panelPrefab;
	public GameObject componentPrefab;
	public GameObject textPrefab;
	public GameObject dividerPrefab;

	public Canvas canvas;
	public List<ProceduralUIComponent> components = new();
	private List<UIComponentReference> references = new();

	private RectTransform panelTransform;
	private GridLayoutGroup grid;

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

		// create all the items
		for (int i = 0; i < components.Count; i++)
		{
			ProceduralUIComponent component = components[i];
			UIComponentReference reference = GenerateUIComponent(ref component);
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

		float width = longestTextWidth + sidePadding * 2;
		float height = itemHeight * components.Count + verticalSpacing * (components.Count - 1) + sidePadding * 2;

		panelTransform.sizeDelta = new(width, height);

		grid = panel.GetComponent<GridLayoutGroup>();
		grid.cellSize = new(longestTextWidth, itemHeight);
		grid.spacing = new(0, verticalSpacing);
	}

	void Display(Vector2 position)
	{

	}

	void Hide()
	{

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

	public UIComponentReference GenerateUIComponent(ref ProceduralUIComponent component)
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
				button.onClick = component.OnClick;
				
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
			transform = newObj.transform,
			imageComponent = image,
			type = component.Type,
			text = text,
			button = button
		};

		return component.reference;
	}
}