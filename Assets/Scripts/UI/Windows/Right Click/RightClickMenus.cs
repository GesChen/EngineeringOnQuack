using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using W = MenuUtil.Window;

public class RightClickMenus : MonoBehaviour {

	public delegate void NewPartEvent(string name);
	public static event NewPartEvent OnNewPartMade;
	public static event Action OnUndo;
	public static event Action OnRedo;
	public static event Action OnCopy;
	public static event Action OnPaste;
	public static event Action OnDelete;

	public static void ClearEvents() {
		OnNewPartMade	= null;
		OnUndo			= null;
		OnRedo			= null;
		OnCopy			= null;
		OnPaste			= null;
		OnDelete		= null;
	}

	static void MakeNewPart(string name) {
		RightClick.Instance.Hide(); // hide the menu

		OnNewPartMade?.Invoke(name);
	}

	// i wanna dry but kiss is more important
	static void Undo() {	RightClick.Instance.Hide();	OnUndo?.Invoke(); }
	static void Redo(){		RightClick.Instance.Hide();	OnRedo?.Invoke(); }
	static void Copy(){		RightClick.Instance.Hide();	OnCopy?.Invoke(); }
	static void Paste(){	RightClick.Instance.Hide();	OnPaste?.Invoke(); }
	static void Delete(){	RightClick.Instance.Hide();	OnDelete?.Invoke(); }

	// will have to add more later for other contexts but for now this is enough

	// todo: some grouping system to put them all under one parent
	public static readonly W digital = new(
		200,
		new(){
				new W.Button(() => MakeNewPart("cpu"),		"cpu",		"",		"cpu"),
				new W.Button(() => MakeNewPart("ram"),		"ram",		"",		"ram"),
				new W.Button(() => MakeNewPart("display"),	"display",	"",		"display"),
				new W.Button(() => MakeNewPart("script"),	"script",	"",		"script")
			});

	public static readonly W mechanical = new(
		200,
		new(){
				new W.Button(() => MakeNewPart("motor 1"),	"motor 1",		"", "motor 1"),
				new W.Button(() => MakeNewPart("motor 2"),	"motor 2",		"", "motor 2"),
				new W.Button(() => MakeNewPart("piston 1"),	"piston 1",		"", "piston 1"),
				new W.Button(() => MakeNewPart("piston 1"),	"piston 1",		"", "piston 2"),
				new W.Button(() => MakeNewPart("servo 1"),	"servo 1",		"", "servo 1"),
				new W.Button(() => MakeNewPart("servo 1"),	"servo 1",		"", "servo 2")
		});

	public static readonly W structural = new(
		200,
		new(){
				new W.Button(() => MakeNewPart("cube"),		"cube",			"",	"cube"),
				new W.Button(() => MakeNewPart("sphere"),	"sphere",		"",	"sphere"),
				new W.Button(() => MakeNewPart("cylinder"),	"cylinder",		"",	"cylinder"),
				new W.Button(() => MakeNewPart("wedge"),	"wedge",		"",	"wedge")
		});

	public static readonly W newPart = new(
		200,
		new() {
			new W.Flyout(structural,	"structural",	"", "structural"),
			new W.Flyout(mechanical,	"mechanical",	"", "mechanical"),
			new W.Flyout(digital,		"digital",		"", "digital"),
			new W.Button(() => MakeNewPart("cable"),			"cable",		"", "cable")
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
			new W.Flyout(newPart,	"new part", "", "plus"),
			new W.Button(() => Undo(),		"undo",		"",	"undo"),
			new W.Button(() => Redo(),		"redo",		"",	"redo"),
			new W.Button(() => Copy(),		"copy",		"",	"copy"),
			new W.Button(() => Paste(),		"paste",	""),
			new W.Button(() => Delete(),	"delete",	"",	"delete"),
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