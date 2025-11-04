using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// probably gonna get changed
public static class CPU_UI {
	public static void ClearEvents() {

	}

	public static void Setup() {

	}

	public static Func<(string[] contents, string name)> GetCurrentScript;

	public static void OnEditScript() {
		var (contents, name) = GetCurrentScript?.Invoke() ?? throw new("GetCurrentScript not subscribed to!");

		SEProcedural.Show();

		SEProcedural.SetFileName(name);
		SEProcedural.ScriptEditor.Load(contents);
	}
}