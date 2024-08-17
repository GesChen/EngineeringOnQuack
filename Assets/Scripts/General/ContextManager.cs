using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextManager : MonoBehaviour
{
	#region singleton
	private static ContextManager _instance;
	public static ContextManager Instance { get { return _instance; } }
	public Context.ContextType contextPreview;
	void Awake() { UpdateSingleton(); }
	private void OnEnable() { UpdateSingleton(); }
	void UpdateSingleton()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(this);
		}
		else
		{
			_instance = this;
		}
	}
	#endregion

	public enum WindowMouseOver
	{
		workplace
	}
	public WindowMouseOver mouseOverWindow;

	public enum SelectionStatus
	{
		NoSelection,
		SingleSelection,
		MultipleSelections
	}
	public SelectionStatus selectionStatus;

	void LateUpdate()
	{
		DetermineMouseOverWindow();

		switch (mouseOverWindow)
		{
			case WindowMouseOver.workplace:
				WorkplaceContexts();
				break;
		}

		contextPreview = Context.Current;
	}

	void DetermineMouseOverWindow()
	{
		mouseOverWindow = WindowMouseOver.workplace; // todo after have multiple windows
	}

	void WorkplaceContexts()
	{
		switch (selectionStatus)
		{
			case SelectionStatus.NoSelection:
				Context.SetCurrent(Context.ContextType.EditingNormal);
				break;
			case SelectionStatus.SingleSelection:
				Context.SetCurrent(Context.ContextType.EditingPart);
				break;
			case SelectionStatus.MultipleSelections:
				Context.SetCurrent(Context.ContextType.EditingMultiple);
				break;
		}
	}
}
