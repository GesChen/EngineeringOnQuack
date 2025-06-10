using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Config.UI.ColorBlock))]
public class ColorBlockDrawer : PropertyDrawer {
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty(position, label, property);

		// Draw label
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		// Don't indent child fields
		var indent = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		// Calculate rects
		float lineHeight = EditorGUIUtility.singleLineHeight + 2;
		Rect rect = new(position.x, position.y, position.width, lineHeight);

		// Draw fields
		EditorGUI.PropertyField(rect, property.FindPropertyRelative("DefaultColor"), new GUIContent("Default"));
		rect.y += lineHeight;
		EditorGUI.PropertyField(rect, property.FindPropertyRelative("HoverColor"), new GUIContent("Hover"));
		rect.y += lineHeight;
		EditorGUI.PropertyField(rect, property.FindPropertyRelative("PressedColor"), new GUIContent("Pressed"));
		rect.y += lineHeight;
		EditorGUI.PropertyField(rect, property.FindPropertyRelative("DisabledColor"), new GUIContent("Disabled"));
		rect.y += lineHeight;
		EditorGUI.PropertyField(rect, property.FindPropertyRelative("ToggledColor"), new GUIContent("Toggled"));
		rect.y += lineHeight;
		EditorGUI.PropertyField(rect, property.FindPropertyRelative("FadeDuration"), new GUIContent("Fade Duration"));

		// Restore indent
		EditorGUI.indentLevel = indent;

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		// 6 fields (5 colors + 1 float), each with a line height
		return (EditorGUIUtility.singleLineHeight + 2) * 6;
	}
}
