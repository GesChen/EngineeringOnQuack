using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ContextWindows {
	// i highk cant think of a better name for this 
	public struct WindowCollection {
		public Type Context;
		public CWindow[] Windows;
		public PMenu.Window[] Menus;

		public WindowCollection(
			Type context,
			CWindow[] windows,
			PMenu.Window[] menus) {

			Context = context;
			Windows = windows;
			Menus = menus;
		}
	}

	static T[] Conglomerate<T>(params T[][] lists) =>
		lists
		.SelectMany(l => l)
		.ToArray();

	static WindowCollection EditingWindows => new(
		typeof(Contexts.Editing),
		Conglomerate(
			RightClickMenus.Windows,
			TransformToolsMenu.Windows,
			MaterialEditingMenu.Windows,
			SaveLoadMenus.Windows,
			OutputsMenu.Windows,
			BottomBar.Windows
		),
		Conglomerate(
			RightClickMenus.Menus,
			MaterialEditingMenu.Menus,
			SaveLoadMenus.Menus,
			OutputsMenu.Menus,
			BottomBar.Menus
		)
	);

	static WindowCollection SimulatingWindows => new(
		typeof(Contexts.Simulating),
		Conglomerate(
			SimulatingMainUI.Windows
		),
		Conglomerate(
			SimulatingMainUI.Menus
		)
	);

	public static WindowCollection[] WindowCollections => new[] {
		EditingWindows,
		SimulatingWindows
	};

	public static WindowCollection? FindCollectionByContext(IContext context) {
		// search for closest window candidate
		// for now we just search downwards cuz idk how we'd search up

		while (context != null) {
			var tryget = GetCollectionStrict(context);
			if (tryget.HasValue) {
				return tryget.Value;
			}

			context = context.Parent;
		}
		return null;
	}

	static WindowCollection? GetCollectionStrict(IContext context) {
		Type type = context.GetType();
		
		return 
			WindowCollections
			.Select<WindowCollection, WindowCollection?>(wc => wc)
			.FirstOrDefault(wc => wc.Value.Context == type);
	}
}