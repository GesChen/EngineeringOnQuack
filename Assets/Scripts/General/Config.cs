using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using TMPro;
using UnityEditor.Search;

public class Config : MonoBehaviour
{
	[System.Serializable]
	public struct UIConfig
	{
		[Header("Controls")]
		[Description("How many pixels the mouse can move away form the rect to still be considered as over")]
		public int _mouseValidityMargin;
		public float _maxRightClickTime;		// right click
		public float _maxMouseMovementToCount;  // right click
		[Space]
		[Header("Visual")]
		public Color _backgroundColor;
		public Color _buttonHoverColor;
		public Color _buttonPressedColor;
		public Color _textColor;
		[Space]
		public int _itemHeight;
		public int _verticalSpacing;
		public int _sidePadding;
		public int _textPadding;
		[Space]
		public int _fontSize;
		public TMP_FontAsset _fontAsset;
		public Vector2 _displayTopLeftCornerOffset;
		public float _minDistFromSides;
		[Space]
		public float _dropDownArrowRightOffset;
		public float _dropDownArrowSize;
		public float _dropDownDisplayOffset;

		[Header("Technical")]
		public GameObject _panelPrefab;
		public GameObject _componentPrefab;
		public GameObject _textPrefab;
		public GameObject _dividerPrefab;
		public GameObject _dropDownArrow;

		#region statics
		public static Color			BackgroundColor;
		public static Color			ButtonHoverColor;
		public static Color			ButtonPressedColor;
		public static Color			TextColor;
		public static int			ItemHeight;
		public static int			VerticalSpacing;
		public static int			SidePadding;
		public static int			TextPadding;
		public static int			FontSize;
		public static TMP_FontAsset FontAsset;
		public static Vector2		DisplayTopLeftCornerOffset;
		public static float			MinDistFromSides;
		public static float			DropDownArrowRightOffset;
		public static float			DropDownArrowSize;
		public static float			DropDownDisplayOffset;
		public static GameObject	PanelPrefab;
		public static GameObject	ComponentPrefab;
		public static GameObject	TextPrefab;
		public static GameObject	DividerPrefab;
		public static GameObject	DropDownArrow;
		public static int			MouseValidityMargin;
		public static float			MaxRightClickTime;
		public static float			MaxMouseMovementToCount;
		#endregion
	}
	public UIConfig UI;

	[Header("Technical")]
	public int _fpsLimit;
	public static int FpsLimit;

	void Awake()
	{
		UIConfig.BackgroundColor			= UI._backgroundColor;
		UIConfig.ButtonHoverColor			= UI._buttonHoverColor;
		UIConfig.ButtonPressedColor			= UI._buttonPressedColor;
		UIConfig.TextColor					= UI._textColor;
		UIConfig.ItemHeight					= UI._itemHeight;
		UIConfig.VerticalSpacing			= UI._verticalSpacing;
		UIConfig.SidePadding				= UI._sidePadding;
		UIConfig.TextPadding				= UI._textPadding;
		UIConfig.FontSize					= UI._fontSize;
		UIConfig.FontAsset					= UI._fontAsset;
		UIConfig.DisplayTopLeftCornerOffset	= UI._displayTopLeftCornerOffset;
		UIConfig.MinDistFromSides			= UI._minDistFromSides;
		UIConfig.DropDownArrowRightOffset	= UI._dropDownArrowRightOffset;
		UIConfig.DropDownArrowSize			= UI._dropDownArrowSize;
		UIConfig.DropDownDisplayOffset		= UI._dropDownDisplayOffset;
		UIConfig.PanelPrefab				= UI._panelPrefab;
		UIConfig.ComponentPrefab			= UI._componentPrefab;
		UIConfig.TextPrefab					= UI._textPrefab;
		UIConfig.DividerPrefab				= UI._dividerPrefab;
		UIConfig.DropDownArrow				= UI._dropDownArrow;
		UIConfig.MouseValidityMargin		= UI._mouseValidityMargin;
		UIConfig.MaxMouseMovementToCount	= UI._maxMouseMovementToCount;
		UIConfig.MaxRightClickTime			= UI._maxRightClickTime;

		FpsLimit = _fpsLimit;

	}
}
