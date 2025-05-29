using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextManager : Singleton<ContextManager> {
	public Context.ContextType contextPreview;

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
