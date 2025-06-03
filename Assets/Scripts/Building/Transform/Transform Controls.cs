using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransformControlsTemporary : MonoBehaviour
{
	public TransformTools Tools;
	public Button translate;
	public Button rotate;
	public Button scale;

	void Start()
	{
		GetComponent<Image>().color = Config.UI.Visual.BackgroundColor;
		Outline outline = gameObject.AddComponent<Outline>();
		outline.effectColor = Config.UI.Visual.OutlineColor;
		outline.effectDistance = Config.UI.Visual.OutlineThickness * Vector2.one;

		UpdateColors();
	}
	
	void UpdateColors()
	{
		ColorBlock normalColors = new()
		{
			colorMultiplier = 1f,
			normalColor = Config.UI.Button.DefaultColor,
			highlightedColor = Config.UI.Button.HoverColor,
			pressedColor = Config.UI.Button.PressedColor
		};
		ColorBlock enabledColors = new()
		{
			colorMultiplier = 1f,
			normalColor = Config.UI.Button.ToggledColor,
			highlightedColor = Config.UI.Button.HoverColor,
			pressedColor = Config.UI.Button.PressedColor
		};

		translate.colors = Tools.translating ? enabledColors : normalColors;
		rotate.colors = Tools.rotating ? enabledColors : normalColors;
		scale.colors = Tools.scaling ? enabledColors : normalColors;
	}

	public void TranslateClicked()
	{
		Tools.translating = !Tools.translating;
		UpdateColors();
	}

	public void RotateClicked()
	{
		Tools.rotating = !Tools.rotating;
		UpdateColors();
	}

	public void ScaleClicked()
	{
		Tools.scaling= !Tools.scaling;
		UpdateColors();
	}
}
