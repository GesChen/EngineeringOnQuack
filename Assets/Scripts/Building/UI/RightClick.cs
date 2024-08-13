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
		public ProceduralUI panel;
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

		lastContext = Context.Current;
		lastPos = mousePos;
	}

	public void UpdatePanel()
	{
		if (currentPanel != null)
			currentPanel.Hide();

		ProceduralUI panel = null;
		foreach (Head head in heads)
		{
			if (head.context == Context.Current)
			{
				panel = head.panel;
				break;
			}
		}

		if (panel == null)
			Debug.LogError($"no panel defined for context {Context.Current}");

		currentPanel = panel;
	}
}
