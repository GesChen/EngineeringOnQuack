using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;

public class PrefabLabels : MonoBehaviour
{
	[Serializable]
	public struct PrefabLabel {
		public Component Component;
		public string Name;
	}
	public List<PrefabLabel> chunks;

	public T Get<T>(string name) where T : Component {
		PrefabLabel item = chunks.Find(c => c.Name == name);
		
		if (item.Component is T)
			return item.Component as T;
		
		return null;
	}
}

[CustomPropertyDrawer(typeof(PrefabLabels.PrefabLabel))]
public class MyStructDrawer : PropertyDrawer {
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		return EditorGUI.GetPropertyHeight(property, label, false); // Default height
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		// Split the rectangle into two halves
		Rect leftRect = new(position.x, position.y, position.width / 2 - 5, position.height);
		Rect rightRect = new(position.x + position.width / 2 + 5, position.y, position.width / 2 - 5, position.height);

		// Get the individual fields from the struct
		SerializedProperty value1 = property.FindPropertyRelative("Component");
		SerializedProperty value2 = property.FindPropertyRelative("Name");

		// Draw the fields side by side
		EditorGUI.PropertyField(leftRect, value1, GUIContent.none);
		EditorGUI.PropertyField(rightRect, value2, GUIContent.none);
	}
}
