using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Context
{
	[Serializable]
	public enum ContextType
	{
		EditingNormal,
		EditingPart
	}

	public static ContextType Current { get; private set; }

	public static void SetCurrent(ContextType type)
	{
		Current = type;
	}
}