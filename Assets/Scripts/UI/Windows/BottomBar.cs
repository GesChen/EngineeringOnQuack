using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using W = PMenu.Window;

public static class BottomBar {
	static readonly float size = 30;

	public static event Action OnTransformOpened;
	public static event Action OnMaterialOpened;

	public static void ClearTransform() { OnTransformOpened = null; }
	public static void ClearMaterial() { OnMaterialOpened = null; }

	static readonly W FileMenu = new(
		"File", 200, new(){
			new W.Button(null, "Save"), // todo: descriptions? and icons
			new W.Button(null, "Save As"),
			new W.Button(null, "Load"),
			new W.Button(null, "Load Recent"),
			new W.Button(null, "Insert Save"), // ? might keep 
		});

	static readonly W ToolsMenu = new(
		"File", 200, new(){
			new W.Button(() => OnTransformOpened?.Invoke(), "Transform"), // todo: descriptions? and icons
			new W.Button(() => OnMaterialOpened?.Invoke(), "Material"), // todo: descriptions? and icons
		});

	static WindowItem BarItem(string label, float width, CWindow target) =>
		WindowItem.NewFlyoutTrigger(
			label,
			new PComponents.FlyoutTrigger(
				target,
				openHorizontally: false,
				openPrioritizingRight: true,
				openPrioritizingUp: true
				),
			WindowItem.LayoutConfig.LayoutElement(new(width, size))
			).SetSubItems(
			WindowItem.NewText(
				new PComponents.Text(
					label,
					alignment: TMPro.TextAlignmentOptions.Center),
				WindowItem.LayoutConfig.FillLayout
				)
			);
	/*.AddEvent(TimedEventInvoker.Timing.Start, // bold on hover
		(source) => { // holy nesting
			var hover = source.gameObject.GetComponent<HoverTarget>();
			var text = source.gameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
			hover.OnHoverStateChange += (state) => {
				text.fontStyle = 
					state 
					? TMPro.FontStyles.Bold 
					: TMPro.FontStyles.Normal;
			};
		});*/

	public static CWindow Bar = new(){
		Name = "Bottom Bar",
		Config = new(){
			Resizable = false,
			Movable = false,
			Size = CWindow.Configuration.FixedSize(new(0, size)),
			Position = new(
				new(0, 0),
				new(1, 0),
				new(.5f, 0),
				new(0, 0)
				),
			Closable = false,
			HideOnStart = false
		},
		Items = new WindowItem[] {
			WindowItem.NewLayout(
				PComponents.Layout.Horizontal.Fixed(
					false,
					true,
					10 // todo
					),
				WindowItem.LayoutConfig.FillLayout,
				new(){
					BarItem("File", 100, FileMenu.CWindow),
					BarItem("Tools", 100, ToolsMenu.CWindow),
				})
		}
	};
	
	public static CWindow[] Windows => new CWindow[] {
		Bar,
		FileMenu.CWindow,
		ToolsMenu.CWindow
	};
}