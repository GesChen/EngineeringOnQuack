using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = MenuUtil.Window;

public class RightClickMenus : MonoBehaviour {
	// will have to add more later for other contexts but for now this is enough

	// todo: some grouping system to put them all under one parent
	public static readonly W digital = new(
		200,
		new(){
				new W.Button(null,	"cpu",		"",		"cpu"),
				new W.Button(null,	"ram 8kb",	"",		"ram"),
				new W.Button(null,	"display",	"",		"display"),
				new W.Button(null,	"script",	"",		"script")
			});

	public static readonly W mechanical = new(
		200,
		new(){
				new W.Button(null,	"motor 1",		"", "motor 1"),
				new W.Button(null,	"motor 2",		"", "motor 2"),
				new W.Button(null,	"piston 1",		"", "piston 1"),
				new W.Button(null,	"piston 1",		"", "piston 2"),
				new W.Button(null,	"servo 1",		"", "servo 1"),
				new W.Button(null,	"servo 1",		"", "servo 2")
		});

	public static readonly W structural = new(
		200,
		new(){
				new W.Button(null,	"cube",			"",	"cube"),
				new W.Button(null,	"sphere",		"",	"sphere"),
				new W.Button(null,	"cylinder",		"",	"cylinder"),
				new W.Button(null,	"wedge",		"",	"wedge")
		});

	public static readonly W newPart = new(
		200,
		new() {
			new W.Flyout(structural,	"structural",	"", "structural"),
			new W.Flyout(mechanical,	"mechanical",	"", "mechanical"),
			new W.Flyout(digital,		"digital",		"", "digital"),
			new W.Button(null,			"cable",		"", "cable")
		});

	// yagni for now we're just doing multiple variations of the same main panel
	// ¯\_("/)_/¯ if this starts getting excessive, then make a variating panel class
	// or something to dynamically update the contents based on which variation is needed
	// but look i dont have that time right now once it gets too laggy we can do that

	public static readonly W inworldDefaultPanel = new(
		"Editing", 200, new() {
			new W.Flyout(newPart,	"new part",	"",	"plus"),
			new W.Button(null,		"undo",		"",	"undo"),
			new W.Button(null,		"redo",		"",	"redo"),
		});

	public static readonly W inworldSinglePanel = new(
		"Editing", 200, new() {
			new W.Flyout(newPart,	"new part",	"",	"plus"),
			new W.Button(null,		"undo",		"",	"undo"),
			new W.Button(null,		"redo",		"",	"redo"),
			new W.Button(null,		"copy",		"",	"copy"),
			new W.Button(null,		"paste",	""),
			new W.Button(null,		"delete",	"",	"delete"),
		});

	public static CWindow[] GetWindows()
		=> MenuUtil.ConvertWindows(
			digital, 
			mechanical, 
			structural, 
			newPart, 
			inworldDefaultPanel,
			inworldSinglePanel);
}