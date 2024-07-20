using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightClick : MonoBehaviour
{
	public ProceduralUI testpanel;
	
	void Update()
	{
		if (Controls.inputMaster.UI.RightClick.WasPressedThisFrame())
		{
			Vector2 mousePos = Controls.inputMaster.UI.Point.ReadValue<Vector2>();
			testpanel.Display(mousePos);
		}
	}
}
