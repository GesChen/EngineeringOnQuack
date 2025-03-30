using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using TMPro;

public class Config : MonoBehaviour
{
	[System.Serializable]
	public struct UI
	{
		public Canvas _mainCanvas;
		[Header("Controls")]
		[Description("How many pixels the mouse can move away form the rect to still be considered as over")]
		public int _mouseValidityMargin;
		public float _maxRightClickTime;        // right click
		public float _maxMovementAfterClick;
		public float _maxMouseMovementToCount;  // right click
		public float _hoverTimeUntilDescription;
		[Space]
		[Header("Visual")]
		public Color _backgroundColor;
		public Color _descriptionBackgroundColor;
		public Color _buttonHoverColor;
		public Color _buttonPressedColor;
		public Color _textColor;
		public int _outlineThickness;
		public Color _outlineColor;
		[Space]
		public int _itemHeight;
		public int _verticalSpacing;
		public int _sidePadding;
		public int _insidePadding;
		public int _iconSize;
		public float _dropDownArrowSize;
		[Space]
		public int _fontSize;
		public TMP_FontAsset _fontAsset;
		public Vector2 _displayTopLeftCornerOffset;
		public float _minDistFromSides;
		public float _dropDownDisplayOffset;
		[Space]
		public int _descriptionFontSize;
		public int _descriptionHeight;

		[Header("Technical")]
		public GameObject _panelPrefab;
		public GameObject _componentPrefab;
		public GameObject _textPrefab;
		public GameObject _descriptionObject;
		public GameObject _dividerPrefab;
		public GameObject _dropDownArrow;
		public GameObject _iconPrefab;
		public Sprite _dropDownClosedSprite;
		public Sprite _dropDownOpenedSprite;

		#region statics
		public static Canvas MainCanvas;
		public static int MouseValidityMargin;
		public static float MaxRightClickTime;
		public static float MaxMovementAfterClick;
		public static float MaxMouseMovementToCount;
		public static float HoverTimeUntilDescription;
		public static Color BackgroundColor;
		public static Color DescriptionBackgroundColor;
		public static Color ButtonHoverColor;
		public static Color ButtonPressedColor;
		public static Color TextColor;
		public static int OutlineThickness;
		public static Color OutlineColor;
		public static int ItemHeight;
		public static int VerticalSpacing;
		public static int SidePadding;
		public static int InsidePadding;
		public static int IconSize;
		public static float DropDownArrowSize;
		public static int FontSize;
		public static TMP_FontAsset FontAsset;
		public static Vector2 DisplayTopLeftCornerOffset;
		public static float MinDistFromSides;
		public static float DropDownDisplayOffset;
		public static int DescriptionFontSize;
		public static int DescriptionHeight;
		public static GameObject PanelPrefab;
		public static GameObject ComponentPrefab;
		public static GameObject TextPrefab;
		public static GameObject DescriptionObject;
		public static GameObject DividerPrefab;
		public static GameObject DropDownArrow;
		public static GameObject IconPrefab;
		public static Sprite DropDownClosedSprite;
		public static Sprite DropDownOpenedSprite;
		#endregion
	}
	public UI UIConfig;

	[Header("Technical")]
	public int _fpsLimit;
	public static int FpsLimit;
	public int _maxRecursionDepth = 128;
	public static int MAX_RECURSION_DEPTH;
	public string _fileSaveLocation;
	public static string FileSaveLocation;

	void Awake() { UpdateStatics(); }
	void OnEnable() { UpdateStatics(); }

	void UpdateStatics()
	{
		UI.MainCanvas					= UIConfig._mainCanvas;
		UI.MouseValidityMargin			= UIConfig._mouseValidityMargin;
		UI.MaxRightClickTime			= UIConfig._maxRightClickTime;
		UI.MaxMovementAfterClick		= UIConfig._maxMovementAfterClick;
		UI.MaxMouseMovementToCount		= UIConfig._maxMouseMovementToCount;
		UI.HoverTimeUntilDescription	= UIConfig._hoverTimeUntilDescription;
		UI.BackgroundColor				= UIConfig._backgroundColor;
		UI.DescriptionBackgroundColor	= UIConfig._descriptionBackgroundColor;
		UI.ButtonHoverColor				= UIConfig._buttonHoverColor;
		UI.ButtonPressedColor			= UIConfig._buttonPressedColor;
		UI.TextColor					= UIConfig._textColor;
		UI.OutlineThickness				= UIConfig._outlineThickness;
		UI.OutlineColor					= UIConfig._outlineColor;
		UI.ItemHeight					= UIConfig._itemHeight;
		UI.VerticalSpacing				= UIConfig._verticalSpacing;
		UI.SidePadding					= UIConfig._sidePadding;
		UI.InsidePadding				= UIConfig._insidePadding;
		UI.IconSize						= UIConfig._iconSize;
		UI.DropDownArrowSize			= UIConfig._dropDownArrowSize;
		UI.FontSize						= UIConfig._fontSize;
		UI.FontAsset					= UIConfig._fontAsset;
		UI.DisplayTopLeftCornerOffset	= UIConfig._displayTopLeftCornerOffset;
		UI.MinDistFromSides				= UIConfig._minDistFromSides;
		UI.DropDownDisplayOffset		= UIConfig._dropDownDisplayOffset;
		UI.DescriptionFontSize			= UIConfig._descriptionFontSize;
		UI.DescriptionHeight			= UIConfig._descriptionHeight;
		UI.PanelPrefab					= UIConfig._panelPrefab;
		UI.ComponentPrefab				= UIConfig._componentPrefab;
		UI.TextPrefab					= UIConfig._textPrefab;
		UI.DescriptionObject			= UIConfig._descriptionObject;
		UI.DividerPrefab				= UIConfig._dividerPrefab;
		UI.DropDownArrow				= UIConfig._dropDownArrow;
		UI.IconPrefab					= UIConfig._iconPrefab;
		UI.DropDownClosedSprite			= UIConfig._dropDownClosedSprite;
		UI.DropDownOpenedSprite			= UIConfig._dropDownOpenedSprite;

		FpsLimit = _fpsLimit;
		MAX_RECURSION_DEPTH = _maxRecursionDepth;
		FileSaveLocation = _fileSaveLocation;
	}
}
