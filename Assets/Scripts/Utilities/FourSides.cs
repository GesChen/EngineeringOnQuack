using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct FourSides { // changed to struct but might break shit so might turn it back
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
	public FourSides(float horizontal, float vertical) : 
		this(vertical, horizontal, vertical, horizontal) { }

	public static explicit operator RectOffset(FourSides fs)=> // for padding and large number typed. 
		new((int)fs.Left, (int)fs.Right, (int)fs.Up, (int)fs.Down);

	public readonly Vector4 ToTMProType() => 
		new(Left, Up, Right, Down);
	public readonly Vector4 ToRectMask2DType() => 
		new(Left, Down, Right, Up);

	public static FourSides Zero		=> new(0, 0, 0, 0);
	public static FourSides UpConst		=> new(1, 0, 0, 0);
	public static FourSides RightConst	=> new(0, 1, 0, 0);
	public static FourSides DownConst	=> new(0, 0, 1, 0);
	public static FourSides LeftConst	=> new(0, 0, 0, 1);
	public static FourSides Even(float v) => new(v, v, v, v);

	public override readonly int GetHashCode() {
		return HashCode.Combine(Up, Right, Down, Left);
	}

	public override readonly bool Equals(object obj) {
		if (obj is not FourSides fs)
			throw new InvalidOperationException("Cannot compare FourSides with a different type.");

		return 
			fs.Up == Up
			&& fs.Right == Right
			&& fs.Down == Down
			&& fs.Left == Left;
	}

	public readonly void SetTransformOffsets(RectTransform rectTransform) {
		rectTransform.offsetMin = new(Left, Down);
		rectTransform.offsetMax = new(-Right, -Up);
	}

	public static bool operator ==(FourSides a, FourSides b) => 
		a.Equals(b);
	public static bool operator !=(FourSides a, FourSides b) => 
		!a.Equals(b);
	public static FourSides operator +(FourSides a, FourSides b) =>
		new(a.Up + b.Up,
			a.Right + b.Right,
			a.Down + b.Down,
			a.Left + b.Left);
	public static FourSides operator -(FourSides a, FourSides b) =>
		new(a.Up - b.Up,
			a.Right - b.Right,
			a.Down - b.Down,
			a.Left - b.Left);

	public static FourSides operator *(FourSides a, float b) =>
		new(a.Up * b,
			a.Right * b,
			a.Down * b,
			a.Left * b);
	public static FourSides operator *(float a, FourSides b) =>
		new(a * b.Up,
			a * b.Right,
			a * b.Down,
			a * b.Left);

	public override readonly string ToString() => 
		$"FourSides(Up: {Up}, Right: {Right}, Down: {Down}, Left: {Left})";
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
