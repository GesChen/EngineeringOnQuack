using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public struct UIComponentReference
{
	public ProceduralUIComponent mainComponent;
	public Transform transform;
	public RectTransform rectTransform;
	public Image imageComponent;
	public UIComponentType type;
	public TextMeshProUGUI text;
	public Button button;
}

public enum UIComponentType
{
	button,
	text,
	divider
}

[Serializable]
public class ProceduralUIComponent // general container for components, doesnt actually do anything itself
{
	public UIComponentType Type;
	public string Text;
	public bool IsDropDown;
	public ProceduralUI DropdownMenu;
	public Button.ButtonClickedEvent OnClick = new();
	public UIComponentReference reference;

	public void Click()
	{
		if (Type == UIComponentType.button)
		{
			OnClick.Invoke();
		}
	}
}