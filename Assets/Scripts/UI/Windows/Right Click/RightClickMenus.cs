using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using W = MenuUtil.Window;

public class RightClickMenus : MonoBehaviour {

	public delegate void NewPartEvent(string name);
	public static event NewPartEvent OnNewPartMade;

	// at some point if this gets too extreme we can do an enum based event approach
	// but would require more processing from the subscriber
	public static event Action OnUndo;
	public static event Action OnRedo;
	public static event Action OnCopy;
	public static event Action OnPaste;
	public static event Action OnDuplicate;
	public static event Action OnDelete;
	public static event Action OnGroup;
	public static event Action OnUnGroup;
	public static event Action OnCombineGroups;
	public static event Action OnAddToGroup;
	public static event Action OnRemoveFromGroup;
	public delegate void TabOpenEvent(WindowItem source);
	public static event TabOpenEvent OnMaterial;

	public static void ClearEvents() {
		// theres no reason to align them i just think it looks funny
		OnNewPartMade			= null;
		OnUndo					= null;
		OnRedo					= null;
		OnCopy					= null;
		OnPaste					= null;
		OnDuplicate				= null;
		OnDelete				= null;
		OnMaterial				= null;
		OnGroup					= null;
		OnUnGroup				= null;
		OnUnGroup				= null;
		OnCombineGroups			= null;
		OnAddToGroup			= null;
		OnRemoveFromGroup		= null;
	}

	static void MakeNewPart(string name) {
		RightClick.Instance.Hide(); // hide the menu

		OnNewPartMade?.Invoke(name);
	}

	// i wanna dry but kiss is more important
	static void Call(Action action) { RightClick.Instance.Hide(); action?.Invoke(); }
	static void Material(W source, int index) {
		WindowItem item = source.Items[index].RealItem;

		OnMaterial?.Invoke(item);
	}

	// will have to add more later for other contexts but for now this is enough

	// TODO: some grouping system to put them all under one parent
	public static readonly W digital = new(
		200,
		new(){
			new W.Button(() => MakeNewPart("cpu"),      "cpu",      iconName: "Parts/cpu"),
			new W.Button(() => MakeNewPart("ram"),      "ram",      iconName: "Parts/ram"),
			new W.Button(() => MakeNewPart("display"),  "display",  iconName: "Parts/display"),
			new W.Button(() => MakeNewPart("script"),   "script",   iconName: "Parts/script")
		});

	public static readonly W mechanical = new(
		200,
		new(){
			new W.Button(() => MakeNewPart("motor 1"),  "motor 1",  iconName: "Parts/motor 1"),
			new W.Button(() => MakeNewPart("motor 2"),  "motor 2",  iconName: "Parts/motor 2"),
			new W.Button(() => MakeNewPart("piston 1"), "piston 1", iconName: "Parts/piston 1"),
			new W.Button(() => MakeNewPart("piston 1"), "piston 1", iconName: "Parts/piston 2"),
			new W.Button(() => MakeNewPart("servo 1"),  "servo 1",  iconName: "Parts/servo 1"),
			new W.Button(() => MakeNewPart("servo 1"),  "servo 1",  iconName: "Parts/servo 2")
		});

	public static readonly W structural = new(
		200,
		new(){
			new W.Button(() => MakeNewPart("cube"),     "cube",     iconName: "Parts/cube"),
			new W.Button(() => MakeNewPart("sphere"),   "sphere",   iconName: "Parts/sphere"),
			new W.Button(() => MakeNewPart("cylinder"), "cylinder", iconName: "Parts/cylinder"),
			new W.Button(() => MakeNewPart("wedge"),    "wedge",    iconName: "Parts/wedge")
		});

	public static readonly W newPart = new(
		200,
		new() {
			new W.Flyout(structural,	"structural",		iconName: "Parts/structural"),
			new W.Flyout(mechanical,	"mechanical",		iconName: "Parts/mechanical"),
			new W.Flyout(digital,		"digital",			iconName: "Parts/digital"),
			new W.Button(() => MakeNewPart("cable"),"cable",iconName: "Parts/cable")
		});

	// yagni for now we're just doing multiple variations of the same main panel
	// ¯\_("/)_/¯ if this starts getting excessive, then make a variating panel class
	// or something to dynamically update the contents based on which variation is needed
	// but look i dont have that time right now once it gets too laggy we can do that


	static W.Item materialItem(W source, int index) =>
		new W.Button(
			() => Material(source, index), 
			"material",
			iconSprite: Config.Building.MaterialIcon);

	static readonly float mainwidth = 220;

	// horizontal layout with a bunch of buttons for modifying stuff
	// undo redo copy paste delete
	public static readonly WindowItem modifierList =
		WindowItem.NewLayout(
			PComponents.Layout.Horizontal.Fixed(
				true,
				true,
				0,
				TextAnchor.UpperLeft),
			WindowItem.LayoutConfig.FixedLayout(
				UIPosition.AnchoredAt(UIPosition.TopLeft),
				new (mainwidth, 45),
				new (5)
			),
			new (){
				WindowItem.NewButtonCustomImageOverlay(
					"Undo",
					new(() => Call(OnUndo)),
					new("Icons/undo"),
					WindowItem.LayoutConfig.FillLayout)
				.AddDescription("Undo"),

				WindowItem.NewButtonCustomImageOverlay(
					"Redo",
					new(() => Call(OnRedo)),
					new("Icons/redo"),
					WindowItem.LayoutConfig.FillLayout)
				.AddDescription("Redo"),

				WindowItem.NewButtonCustomImageOverlay(
					"Copy",
					new(() => Call(OnCopy)),
					new("Icons/copy"),
					WindowItem.LayoutConfig.FillLayout)
				.AddDescription("Copy"),

				WindowItem.NewButtonCustomImageOverlay(
					"Paste",
					new(() => Call(OnPaste)),
					new("Icons/paste"),
					WindowItem.LayoutConfig.FillLayout)
				.AddDescription("Paste"),

				WindowItem.NewButtonCustomImageOverlay(
					"Delete",
					new(() => Call(OnDelete)),
					new("Icons/delete"),
					WindowItem.LayoutConfig.FillLayout)
				.AddDescription("Delete")
			});

	private static W m_inWorldUniversalMenu;
	
	public static W inWorldUniversalMenu {
		get {
			if (m_inWorldUniversalMenu == null) {
				m_inWorldUniversalMenu = new(
					"Editing", mainwidth, new() { // deindented for readability
// ---------------------------- Universal Menu Items ------------------------------------
/* 0*/	new W.Flyout(newPart,					"new part",		iconName: "plus"),
/* 1*/	new W.Button(() => Call(OnUndo),		"undo",			iconName: "undo"),
/* 2*/	new W.Button(() => Call(OnRedo),		"redo",			iconName: "redo"),
/* 3*/	new W.Button(() => Call(OnPaste),		"paste",		iconName: "paste"),
/* 4*/	new W.CustomItem(modifierList),
/* 5*/	new W.Button(() => Call(OnDuplicate),	"duplicate",	iconName: "duplicate"),
/* 6*/	null, // gets replaced with materialitem 
/* 7*/	new W.Button(() => Call(OnGroup),			"group",				iconName: "group"),
/* 8*/	new W.Button(() => Call(OnUnGroup),			"ungroup",				iconName: "ungroup"),
/* 9*/	new W.Button(() => Call(OnCombineGroups),	"combine groups",		iconName: "combine groups"),
/*10*/	new W.Button(() => Call(OnAddToGroup),		"add to group",			iconName: "add to group"),
/*11*/	new W.Button(() => Call(OnRemoveFromGroup),	"remove from group",	iconName: "remove from group"),
// -----------------------------------------------------------------------------------
					},
					switchable: true);

				// fill in the material item
				m_inWorldUniversalMenu.Items[6] = materialItem(m_inWorldUniversalMenu, 6); // manual reflection lol
			}
			return m_inWorldUniversalMenu;
		}
	}

/*	public static class UniversalMasks {
		// masks to use the universal menu properly		= 0b_012345678901234
		public static readonly int Default				= 0b_111100000000;
		public static readonly int SingleSelection		= 0b_100011100000;
		public static readonly int MultiSelection		= 0b_100011110000;

		// different multi select groups
		public static readonly int AGPF_APOOGF			= 0b_100011101100;
		public static readonly int AGPT_APOOGF			= 0b_100011101100;
		public static readonly int AGPF_APOOGT			= 0b_100011101010;
		public static readonly int AGPT_APOOGT_AGPST	= 0b_100011101000;
		public static readonly int AGPT_APOOGT_AGPSF	= 0b_100011101001;
	}*/

	public static class UniversalIndices {
		public static readonly int[] Default			= new[]{0, 1, 2, 3};
		public static readonly int[] SingleSelection	= new[]{0, 4, 5, 6};
		public static readonly int[] MultiSelection		= new[]{0, 4, 5, 6, 7};

		// different multi select groups
		public static readonly int[] AGPF_APOOGF		= new[]{0, 4, 5, 6, 8, 9};
		public static readonly int[] AGPT_APOOGF		= new[]{0, 4, 5, 6, 8, 9};
		public static readonly int[] AGPF_APOOGT		= new[]{0, 4, 5, 6, 8, 10};
		public static readonly int[] AGPT_APOOGT_AGPST	= new[]{0, 4, 5, 6, 8};
		public static readonly int[] AGPT_APOOGT_AGPSF	= new[]{0, 4, 5, 6, 8, 11};
	}

	private static readonly W[] windows = {
		digital,
		mechanical,
		structural,
		newPart,
		inWorldUniversalMenu
	};

	public static CWindow[] Windows => windows.Select(w => w.CWindow).ToArray();
}