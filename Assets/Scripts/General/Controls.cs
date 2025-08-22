using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Controls : MonoBehaviour
{
    public static float clickMaxDist = 5;
    public static float clickMaxTime = .1f;

	public static InputMaster inputMaster;

	void Awake()
	{
		inputMaster = new InputMaster();
	}
	void OnEnable()
	{
		inputMaster ??= new InputMaster();
		inputMaster.Enable();
	}
	void OnDisable()
	{
		inputMaster.Disable();
	}
	public static Controls GetControls()
	{
		return FindObjectOfType<Controls>();
	}

	public delegate void MouseMove(Vector2 position);
	public static event MouseMove OnMouseMove;

	public static Vector2 mousePos;
	public static Vector2 lastMousePos;
	void Update()
	{
		mousePos = Mouse.current.position.value;
		if (lastMousePos != mousePos)
		{
			//Debug.Log($"moved to {mousePos}");
			OnMouseMove?.Invoke(mousePos);
		}
	}
	private void LateUpdate()
	{
		lastMousePos = mousePos;
	}
}