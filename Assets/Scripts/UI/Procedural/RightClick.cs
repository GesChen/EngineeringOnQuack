using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RightClick : MonoBehaviour
{
	[Serializable]
	public struct Head
	{
		public PUIPanelHead panel;
		public Context.ContextType context;
	}

	public List<Head> heads;

	private ProceduralUI currentPanel;

	Vector2 posAtDown;
	Vector2 lastPos;
	Context.ContextType lastContext;

	private void Start()
	{
		UpdatePanel();
	}

	void Update()
	{
		if (Context.Current != lastContext)
			UpdatePanel();

		if (currentPanel != null)
			HandleInput();

		lastContext = Context.Current;
	}

	public void UpdatePanel()
	{
		if (currentPanel != null)
			currentPanel.Hide();

		ProceduralUI panel = heads.Find(head => head.context == Context.Current).panel.main;

		if (panel == null)
			Debug.LogWarning($"no panel defined for context {Context.Current}");
		
		// contexts without panels will not show anything
		currentPanel = panel;
	}
	
	void HandleInput()
	{

		Vector2 mousePos = Mouse.current.position.value;

		if (Mouse.current.rightButton.wasPressedThisFrame)
		{
			posAtDown = mousePos;

			bool lastMovementTooFar = (lastPos - mousePos).magnitude > Config.UI.MaxMouseMovementToCount;
			if (!lastMovementTooFar)
				currentPanel.Display(mousePos);
		}

		bool mouseMovedTooFar = (posAtDown - mousePos).magnitude > Config.UI.MaxMovementAfterClick;
		bool otherButtonPressed = Mouse.current.leftButton.isPressed || Mouse.current.middleButton.isPressed;
		if ((mouseMovedTooFar && Mouse.current.rightButton.isPressed) ||
			(otherButtonPressed && !(currentPanel.anyDropDownsInRange || currentPanel.mouseInRange)))
		{
			currentPanel.Hide();
		}

		lastPos = mousePos;
	}
}
