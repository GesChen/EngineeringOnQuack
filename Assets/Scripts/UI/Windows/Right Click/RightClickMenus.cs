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
	public static event Action OnDuplicate;
	public static event Action OnDelete;

	public static void ClearEvents() {
		OnNewPartMade	= null;
		OnUndo			= null;
		OnRedo			= null;
		OnCopy			= null;
		OnPaste			= null;
		OnDuplicate		= null;
		OnDelete		= null;
	}

	static void MakeNewPart(string name) {
		RightClick.Instance.Hide(); // hide the menu

		OnNewPartMade?.Invoke(name);
	}

	// i wanna dry but kiss is more important
	static void Undo()		{	RightClick.Instance.Hide();	OnUndo?.Invoke(); }
	static void Redo()		{	RightClick.Instance.Hide();	OnRedo?.Invoke(); }
	static void Copy()		{	RightClick.Instance.Hide();	OnCopy?.Invoke(); }
	static void Paste()		{	RightClick.Instance.Hide();	OnPaste?.Invoke(); }
	static void Duplicate()	{	RightClick.Instance.Hide(); OnDuplicate?.Invoke(); }
	static void Delete()	{	RightClick.Instance.Hide();	OnDelete?.Invoke(); }

	// will have to add more later for other contexts but for now this is enough

	// TODO: some grouping system to put them all under one parent
	public static readonly W digital = new(
		200,
		new(){
			new W.Button(() => MakeNewPart("cpu"),		"cpu",		iconName: "cpu"),
			new W.Button(() => MakeNewPart("ram"),		"ram",		iconName: "ram"),
			new W.Button(() => MakeNewPart("display"),	"display",	iconName: "display"),
			new W.Button(() => MakeNewPart("script"),	"script",	iconName: "script")
		});

	public static readonly W mechanical = new(
		200,
		new(){
			new W.Button(() => MakeNewPart("motor 1"),	"motor 1",	iconName: "motor 1"),
			new W.Button(() => MakeNewPart("motor 2"),	"motor 2",	iconName: "motor 2"),
			new W.Button(() => MakeNewPart("piston 1"),	"piston 1",	iconName: "piston 1"),
			new W.Button(() => MakeNewPart("piston 1"),	"piston 1",	iconName: "piston 2"),
			new W.Button(() => MakeNewPart("servo 1"),	"servo 1",	iconName: "servo 1"),
			new W.Button(() => MakeNewPart("servo 1"),	"servo 1",	iconName: "servo 2")
		});

	public static readonly W structural = new(
		200,
		new(){
			new W.Button(() => MakeNewPart("cube"),		"cube",		iconName: "cube"),
			new W.Button(() => MakeNewPart("sphere"),	"sphere",	iconName: "sphere"),
			new W.Button(() => MakeNewPart("cylinder"),	"cylinder",	iconName: "cylinder"),
			new W.Button(() => MakeNewPart("wedge"),	"wedge",	iconName: "wedge")
		});

	public static readonly W newPart = new(
		200,
		new() {
			new W.Flyout(structural,	"structural",	iconName: "structural"),
			new W.Flyout(mechanical,	"mechanical",	iconName: "mechanical"),
			new W.Flyout(digital,		"digital",		iconName: "digital"),
			new W.Button(() => MakeNewPart("cable"),	"cable",	iconName: "cable")
		});

	// yagni for now we're just doing multiple variations of the same main panel
	// ¯\_("/)_/¯ if this starts getting excessive, then make a variating panel class
	// or something to dynamically update the contents based on which variation is needed
	// but look i dont have that time right now once it gets too laggy we can do that

	public static readonly W inworldDefaultPanel = new(
		"Editing", 200, new() {
			new W.Flyout(newPart,	"new part",	"",	"plus"),
			new W.Button(() => Undo(),		"undo",	iconName: "undo"),
			new W.Button(() => Redo(),		"redo",	iconName: "redo"),
			new W.Button(() => Paste(),		"paste",iconName: "paste"), // TODO
		});

	public static readonly WindowItem modifierList =
		WindowItem.NewLayout(
			PComponents.Layout.HorizontalFixed(
				0,
				TextAnchor.UpperLeft,
				true,
				true),
			WindowItem.LayoutConfig.FixedLayout(
				UIPosition.AnchoredAt(UIPosition.TopLeft),
				new (200, 45),
				new (5)
			),
			new (){
				WindowItem.NewButtonCustomImage(
					"Undo",
					new(() => Undo()),
					new("Icons/undo"),
					WindowItem.LayoutConfig.FillLayout)
				.WithDescription("Undo"),

				WindowItem.NewButtonCustomImage(
					"Redo",
					new(() => Redo()),
					new("Icons/redo"),
					WindowItem.LayoutConfig.FillLayout)
				.WithDescription("Redo"),

				WindowItem.NewButtonCustomImage(
					"Copy",
					new(() => Copy()),
					new("Icons/copy"),
					WindowItem.LayoutConfig.FillLayout)
				.WithDescription("Copy"),

				WindowItem.NewButtonCustomImage(
					"Paste",
					new(() => Paste()),
					new("Icons/paste"),
					WindowItem.LayoutConfig.FillLayout)
				.WithDescription("Paste"),

				WindowItem.NewButtonCustomImage(
					"Delete",
					new(() => Delete()),
					new("Icons/delete"),
					WindowItem.LayoutConfig.FillLayout)
				.WithDescription("Delete")
			});

	public static readonly W inworldSinglePanel = new(
		"Editing", 200, new() {
			new W.Flyout(newPart,			"new part",		iconName: "plus"),
			new W.CustomItem(modifierList),
			new W.Button(() => Duplicate(),	"duplicate",	iconName: "duplicate"),
		});
	
	public static readonly W inworldMultiPanel = new(
		"Editing", 200, new() {
			new W.Flyout(newPart,			"new part",		iconName: "plus"),
			new W.CustomItem(modifierList),
			new W.Button(() => Duplicate(),	"duplicate",	iconName: "duplicate"),
			new W.Button(null,	"group", iconName: "group"),
		});

	public static CWindow[] GetWindows()
		=> MenuUtil.ConvertWindows(
			digital,
			mechanical,
			structural,
			newPart,
			inworldDefaultPanel,
			inworldSinglePanel,
			inworldMultiPanel);
}