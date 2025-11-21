using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// probably gonna get changed
public static class CPU_UI {
	public static Action<ScriptEditorRewritten> OnEdit;

	public static Func<(string contents, string name)> GetCurrentScript;

	public static void EditScript() {
		// make new ser
		ScriptEditorRewritten.CreateWindow(EditorCreated);
	}

	static void EditorCreated(ScriptEditorRewritten editor) {
		var (contents, name) = GetCurrentScript?.Invoke() ?? throw new("GetCurrentScript not subscribed to!");

		editor.SetFileName(name);
		editor.Load(contents);

		OnEdit?.Invoke(editor);
	}
}