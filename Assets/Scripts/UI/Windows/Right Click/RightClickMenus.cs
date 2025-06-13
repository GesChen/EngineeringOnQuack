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
	public static event Action OnUndo;
	public static event Action OnRedo;
	public static event Action OnCopy;
	public static event Action OnPaste;
	public static event Action OnDuplicate;
	public static event Action OnDelete;
	public static event Action OnGroup;
	public static event Action OnUnGroup;
	public delegate void TabOpenEvent(WindowItem source);
	public static event TabOpenEvent OnMaterial;

	public static void ClearEvents() {
		OnNewPartMade = null;
		OnUndo = null;
		OnRedo = null;
		OnCopy = null;
		OnPaste = null;
		OnDuplicate = null;
		OnDelete = null;
		OnMaterial = null;
		OnGroup = null;
		OnUnGroup = null;
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
			new W.Button(() => MakeNewPart("cpu"),      "cpu",      iconName: "cpu"),
			new W.Button(() => MakeNewPart("ram"),      "ram",      iconName: "ram"),
			new W.Button(() => MakeNewPart("display"),  "display",  iconName: "display"),
			new W.Button(() => MakeNewPart("script"),   "script",   iconName: "script")
		});

	public static readonly W mechanical = new(
		200,
		new(){
			new W.Button(() => MakeNewPart("motor 1"),  "motor 1",  iconName: "motor 1"),
			new W.Button(() => MakeNewPart("motor 2"),  "motor 2",  iconName: "motor 2"),
			new W.Button(() => MakeNewPart("piston 1"), "piston 1", iconName: "piston 1"),
			new W.Button(() => MakeNewPart("piston 1"), "piston 1", iconName: "piston 2"),
			new W.Button(() => MakeNewPart("servo 1"),  "servo 1",  iconName: "servo 1"),
			new W.Button(() => MakeNewPart("servo 1"),  "servo 1",  iconName: "servo 2")
		});

	public static readonly W structural = new(
		200,
		new(){
			new W.Button(() => MakeNewPart("cube"),     "cube",     iconName: "cube"),
			new W.Button(() => MakeNewPart("sphere"),   "sphere",   iconName: "sphere"),
			new W.Button(() => MakeNewPart("cylinder"), "cylinder", iconName: "cylinder"),
			new W.Button(() => MakeNewPart("wedge"),    "wedge",    iconName: "wedge")
		});

	public static readonly W newPart = new(
		200,
		new() {
			new W.Flyout(structural,    "structural",   iconName: "structural"),
			new W.Flyout(mechanical,    "mechanical",   iconName: "mechanical"),
			new W.Flyout(digital,       "digital",      iconName: "digital"),
			new W.Button(() => MakeNewPart("cable"),    "cable",    iconName: "cable")
		});

	// yagni for now we're just doing multiple variations of the same main panel
	// ¯\_("/)_/¯ if this starts getting excessive, then make a variating panel class
	// or something to dynamically update the contents based on which variation is needed
	// but look i dont have that time right now once it gets too laggy we can do that

	static readonly W.Item newpart = 
		new W.Flyout(newPart, "new part", iconName: "plus");

	static readonly W.Item duplicate = 
		new W.Button(() => Call(OnDuplicate), "duplicate", iconName: "duplicate");

	static W.Item materialItem(W source, int index) =>
		new W.Button(
			() => Material(source, index), 
			"material",
			iconSprite: Config.Building.MaterialIcon);
			
	public static readonly W inWorldDefaultPanel = new(
		"Editing", 200, new() {
			newpart,
			new W.Button(() => Call(OnUndo),	"undo",		iconName: "undo"),
			new W.Button(() => Call(OnRedo),	"redo",		iconName: "redo"),
			new W.Button(() => Call(OnPaste),	"paste",	iconName: "paste"),
		});

	public static readonly WindowItem modifierList =
		WindowItem.NewLayout(
			PComponents.Layout.Horizontal.Fixed(
				true,
				true,
				0,
				TextAnchor.UpperLeft),
			WindowItem.LayoutConfig.FixedLayout(
				UIPosition.AnchoredAt(UIPosition.TopLeft),
				new (200, 45),
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


	// complicatd readonly property setup to allow for self referencing in the materialitem
	private static W m_inWorldSinglePanel;

	public static W inWorldSinglePanel {
		get {
			if (m_inWorldSinglePanel == null) {
				m_inWorldSinglePanel = new(
					"Editing", 200, new() {
						newpart,
						new W.CustomItem(modifierList),
						duplicate,
						null // gets replaced with materialitem 
					});

				m_inWorldSinglePanel.Items[3] = materialItem(m_inWorldSinglePanel, 3); // manual reflection lol
			}
			
			return m_inWorldSinglePanel;
		}
	}

	private static W m_inWorldMultiPanel;
	
	public static W inWorldMultiPanel {
		get {
			if (m_inWorldMultiPanel == null) {
				m_inWorldMultiPanel = new(
					"Editing", 200, new() {
						newpart,
						new W.CustomItem(modifierList),
						duplicate,
						null, // gets replaced with materialitem 
						new W.Button(() => Call(OnGroup), "group", iconName: "group"),
					});

				m_inWorldMultiPanel.Items[3] = materialItem(m_inWorldMultiPanel, 3); // manual reflection lol
			}
			return m_inWorldMultiPanel;
		}
	}

	private static readonly W[] windows = new[]{
		digital,
		mechanical,
		structural,
		newPart,
		inWorldDefaultPanel,
		inWorldSinglePanel,
		inWorldMultiPanel
	};

	public static CWindow[] Windows => windows.Select(w => w.CWindow).ToArray();
}