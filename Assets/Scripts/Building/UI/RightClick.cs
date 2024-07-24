using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RightClick : MonoBehaviour
{
	public ProceduralUI testpanel;
	
	void Update()
	{
		if (Mouse.current.rightButton.wasPressedThisFrame)
		{
			Vector2 mousePos = Controls.inputMaster.UI.Point.ReadValue<Vector2>();
			testpanel.Display(mousePos);
		}
	}
}
