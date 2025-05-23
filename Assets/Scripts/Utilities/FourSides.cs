using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class FourSides {
	public float Up;
	public float Right;
	public float Down;
	public float Left;
	public FourSides(float up, float right, float down, float left) {
		Up = up;
		Right = right;
		Down = down;
		Left = left;
	}
	public FourSides(float x) : this(x, x, x, x) { }

	public RectOffset ToUnityType() // for padding and large number typed. 
		=> new((int)Left, (int)Right, (int)Up, (int)Down);

	public static FourSides Zero => new(0, 0, 0, 0);
	public static FourSides Even(float v) => new(v, v, v, v);
}

// thanks chatgpt
[CustomPropertyDrawer(typeof(FourSides))]
public class FourSidesDrawer : PropertyDrawer {
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		EditorGUI.BeginProperty(position, label, property);

		float rowHeight = EditorGUIUtility.singleLineHeight;
		float spacing = EditorGUIUtility.standardVerticalSpacing;

		SerializedProperty up = property.FindPropertyRelative("Up");
		SerializedProperty right = property.FindPropertyRelative("Right");
		SerializedProperty down = property.FindPropertyRelative("Down");
		SerializedProperty left = property.FindPropertyRelative("Left");

		if (EditorGUIUtility.wideMode) {
			float labelWidth = 40;
			float spacingBetween = 10f;
			float totalSpacing = spacingBetween * 3;
			float fieldWidth = (position.width - labelWidth * 4 - totalSpacing) / 4f;

			Rect upRect = new Rect(position.x, position.y, labelWidth, rowHeight);
			EditorGUI.LabelField(upRect, "Up");
			upRect.x += labelWidth;
			upRect.width = fieldWidth;
			EditorGUI.PropertyField(upRect, up, GUIContent.none);

			Rect rightRect = upRect;
			rightRect.x += fieldWidth + spacingBetween + labelWidth;
			EditorGUI.LabelField(new Rect(rightRect.x - labelWidth, rightRect.y, labelWidth, rowHeight), "Right");
			EditorGUI.PropertyField(rightRect, right, GUIContent.none);

			Rect downRect = rightRect;
			downRect.x += fieldWidth + spacingBetween + labelWidth;
			EditorGUI.LabelField(new Rect(downRect.x - labelWidth, downRect.y, labelWidth, rowHeight), "Down");
			EditorGUI.PropertyField(downRect, down, GUIContent.none);

			Rect leftRect = downRect;
			leftRect.x += fieldWidth + spacingBetween + labelWidth;
			EditorGUI.LabelField(new Rect(leftRect.x - labelWidth, leftRect.y, labelWidth, rowHeight), "Left");
			EditorGUI.PropertyField(leftRect, left, GUIContent.none);
		} else {
			Rect current = position;
			current.height = rowHeight;

			EditorGUI.LabelField(current, "Up");
			current.x += 40; current.width -= 40;
			EditorGUI.PropertyField(current, up, GUIContent.none);

			current.y += rowHeight + spacing; current.x = position.x; current.width = position.width;
			EditorGUI.LabelField(current, "Right");
			current.x += 40; current.width -= 40;
			EditorGUI.PropertyField(current, right, GUIContent.none);

			current.y += rowHeight + spacing; current.x = position.x; current.width = position.width;
			EditorGUI.LabelField(current, "Down");
			current.x += 40; current.width -= 40;
			EditorGUI.PropertyField(current, down, GUIContent.none);

			current.y += rowHeight + spacing; current.x = position.x; current.width = position.width;
			EditorGUI.LabelField(current, "Left");
			current.x += 40; current.width -= 40;
			EditorGUI.PropertyField(current, left, GUIContent.none);
		}

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		if (EditorGUIUtility.wideMode) return EditorGUIUtility.singleLineHeight;
		return EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing * 3;
	}
}
