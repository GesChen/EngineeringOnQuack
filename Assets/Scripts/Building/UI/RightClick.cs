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
		if (Mouse.current.rightButton.wasPressedThisFrame) 
		{
			timeAtDown = Time.time;
			posAtDown = Mouse.current.position.value;
		}
		if (Mouse.current.rightButton.wasReleasedThisFrame && 
			Time.time - timeAtDown < Config.UIConfig.MaxRightClickTime &&
			(posAtDown - Mouse.current.position.value).magnitude < Config.UIConfig.MaxMouseMovementToCount)
		{
			Vector2 mousePos = Controls.inputMaster.UI.Point.ReadValue<Vector2>();
			testpanel.Display(mousePos);
		}
	}
}