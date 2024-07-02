using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RightClick : MonoBehaviour
{
	public ProceduralUI testpanel;

	#region inputmaster
	InputMaster controls;

	void Awake()
	{
		controls = new InputMaster();
	}
	void OnEnable()
	{
		controls ??= new InputMaster();
		controls.Enable();
	}
	void OnDisable()
	{
		controls.Disable();
	}
	#endregion

	
	void Update()
	{
		if (controls.UI.RightClick.WasPressedThisFrame())
		{
			Vector2 mousePos = controls.UI.Point.ReadValue<Vector2>();
			testpanel.Display(mousePos);
		}
	}
}
