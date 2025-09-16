using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// static for now, can figure out howt o make it into object form later
public static class SEProcedural {

	static void Setup(TimedEventInvoker iv) {
		GameObject g = iv.gameObject;

		var se = g.AddComponent<ScriptEditor>();
		var sh = g.AddComponent<SyntaxHighlighter>();
		var history = g.AddComponent<LazyHistory>(); // interchangable with history if fix it
		history.SE = se;
			
	}

	static CWindow SEWindow;
	static void SetSEWindow() {
		SEWindow = new() {
			Name = "ScriptEditor",
			Config = new() {
				Resizable = true,
				Movable = true,
				HideOnStart = false
			},
			Items = new WindowItem[] {

			},
			CustomEvents = new() {
				new(TimedEventInvoker.Timing.Awake, Setup)
			}
		};
	}
}