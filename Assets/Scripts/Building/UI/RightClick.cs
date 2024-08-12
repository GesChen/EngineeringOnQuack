using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RightClick : MonoBehaviour
{
	public ProceduralUI testpanel;

	float timeAtDown;
	Vector2 posAtDown;
	void Update()
	{
		Vector2 mousePos = Mouse.current.position.value;
		if (Mouse.current.rightButton.wasPressedThisFrame)
		{
			posAtDown = mousePos;
			testpanel.Display(mousePos);
		}
		if (((posAtDown - mousePos).magnitude > Config.UI.MaxMouseMovementToCount && Mouse.current.rightButton.isPressed) ||
			Mouse.current.leftButton.isPressed || Mouse.current.middleButton.isPressed)
		{
			testpanel.Hide();
		}
	}
}
