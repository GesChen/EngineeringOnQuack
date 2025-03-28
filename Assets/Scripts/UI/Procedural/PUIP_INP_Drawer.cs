using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PUIComponent.IconNamePair))]
public class MyStructDrawer : PropertyDrawer {
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		return EditorGUI.GetPropertyHeight(property, label, false); // Default height
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		// Split the rectangle into two halves
		Rect leftRect = new (position.x, position.y, position.width / 2 - 5, position.height);
		Rect rightRect = new (position.x + position.width / 2 + 5, position.y, position.width / 2 - 5, position.height);

		// Get the individual fields from the struct
		SerializedProperty value1 = property.FindPropertyRelative("Name");
		SerializedProperty value2 = property.FindPropertyRelative("Icon");

		// Draw the fields side by side
		EditorGUI.PropertyField(leftRect, value1, GUIContent.none);
		EditorGUI.PropertyField(rightRect, value2, GUIContent.none);
	}
}