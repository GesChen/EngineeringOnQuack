using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;

public class PrefabChunker : MonoBehaviour
{
	public struct PrefabChunk {
		public GameObject Object;
		public string Name;
	}
	public List<PrefabChunk> chunks;

	public GameObject Get(string name) {
		return chunks.Find(c => c.Name == name).Object;
	}
}

[CustomPropertyDrawer(typeof(PrefabChunker.PrefabChunk))]
public class MyStructDrawer : PropertyDrawer {
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		return EditorGUI.GetPropertyHeight(property, label, false); // Default height
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		// Split the rectangle into two halves
		Rect leftRect = new(position.x, position.y, position.width / 2 - 5, position.height);
		Rect rightRect = new(position.x + position.width / 2 + 5, position.y, position.width / 2 - 5, position.height);

		// Get the individual fields from the struct
		SerializedProperty value1 = property.FindPropertyRelative("Object");
		SerializedProperty value2 = property.FindPropertyRelative("Name");

		// Draw the fields side by side
		EditorGUI.PropertyField(leftRect, value1, GUIContent.none);
		EditorGUI.PropertyField(rightRect, value2, GUIContent.none);
	}
}