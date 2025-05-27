using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = MenuUtil.Window;

public class RightClickMenus : MonoBehaviour {
	// will have to add more later for other contexts but for now this is enough
	static W digital = new(
		200,
		new(){
				new W.Button(null,	"cpu",		"",		"cpu"),
				new W.Button(null,	"ram 8kb",	"",		"ram"),
				new W.Button(null,	"display",	"",		"display"),
				new W.Button(null,	"script",	"",		"script")
			});

	static W mechanical = new(
		200,
		new(){
				new W.Button(null,	"motor 1",		"", "motor 1"),
				new W.Button(null,	"motor 2",		"", "motor 2"),
				new W.Button(null,	"piston 1",		"", "piston 1"),
				new W.Button(null,	"piston 1",		"", "piston 2"),
				new W.Button(null,	"servo 1",		"", "servo 1"),
				new W.Button(null,	"servo 1",		"", "servo 2")
		});

	static W structural = new(
		200,
		new(){
				new W.Button(null,	"cube",			"",	"cube"),
				new W.Button(null,	"sphere",		"",	"sphere"),
				new W.Button(null,	"cylinder",		"",	"cylinder"),
				new W.Button(null,	"wedge",		"",	"wedge")
		});

	static W newPart = new(
		200,
		new() {
			new W.Flyout(structural,	"structural",	"", "structural"),
			new W.Flyout(mechanical,	"mechanical",	"", "mechanical"),
			new W.Flyout(digital,		"digital",		"", "digital"),
			new W.Button(null,			"cable",		"", "cable")
		});

	static W editingNormalPanel = new(
		"Editing",
		200,
		new() {
			new W.Flyout(newPart,	"new part",	"makes a new part",		"plus"),
			new W.Button(null,		"clean up",	"",						"clean up"),
			new W.Button(null,		"undo",		"",						"undo"),
			new W.Button(null,		"redo",		"",						"redo")
		});

	public static CWindow[] GetWindows()
		=> MenuUtil.ConvertWindows(digital, mechanical, structural, newPart, editingNormalPanel);
}