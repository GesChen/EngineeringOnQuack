using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Config.UI.ColorBlock))]
public class ColorBlockDrawer : PropertyDrawer {
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		// Begin property
		EditorGUI.BeginProperty(position, label, property);

		// Draw foldout
		property.isExpanded = EditorGUI.Foldout(
			new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
			property.isExpanded,
			label,
			true // toggleOnLabelClick
		);

		if (property != null && property.isExpanded) {
			EditorGUI.indentLevel++;

			float lineHeight = EditorGUIUtility.singleLineHeight + 2;
			Rect fieldRect = new(position.x, position.y + lineHeight, position.width, EditorGUIUtility.singleLineHeight);

			DrawField(ref fieldRect, property, "NormalColor", "Normal");
			DrawField(ref fieldRect, property, "HoverColor", "Hover");
			DrawField(ref fieldRect, property, "PressedColor", "Pressed");
			DrawField(ref fieldRect, property, "DisabledColor", "Disabled");
			DrawField(ref fieldRect, property, "ToggledColor", "Toggled");
			DrawField(ref fieldRect, property, "FadeDuration", "Fade Duration");

			EditorGUI.indentLevel--;
		}

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		if (!property.isExpanded) {
			return EditorGUIUtility.singleLineHeight;
		}

		// 1 line for foldout + 6 fields (5 colors + 1 float)
		return EditorGUIUtility.singleLineHeight * 7 + 2 * 6;
	}

	private void DrawField(ref Rect position, SerializedProperty property, string name, string displayName) {
		var prop = property.FindPropertyRelative(name);
		if (prop != null) {
			EditorGUI.PropertyField(position, prop, new GUIContent(displayName));
			position.y += EditorGUIUtility.singleLineHeight + 2;
		} else {
			EditorGUI.LabelField(position, $"Missing: {name}");
			position.y += EditorGUIUtility.singleLineHeight + 2;
		}
	}
}
