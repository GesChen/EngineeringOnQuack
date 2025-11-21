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
		public Action[] Sets;

		public WindowCollection(
			Type context,
			CWindow[] windows,
			PMenu.Window[] menus,
			Action[] sets) {

			Context = context;
			Windows = windows;
			Menus = menus;
			Sets = sets;
		}
	}

	// possibly the most cursed thing ever written. however? it works? 
	// because the issue is that the custom setters need to be called
	// before the things can be accessed and like at all accessed
	// kinda complicated yeah but it works so
	public static WindowCollection MakeCollection(
		Type context,
		Action[] sets,
		Func<(CWindow[] windows, PMenu.Window[] menus)> getter
		) {

		foreach (var setter in sets) {
			setter();
		}

		var (windows, menus) = getter();

		return new(context, windows, menus, sets);
	}

	static T[] Conglomerate<T>(params T[][] lists) =>
		lists
		.SelectMany(l => l)
		.ToArray();

	static WindowCollection EditingWindows => 
		MakeCollection(
		typeof(Contexts.Editing),
		new Action[] {
			RightClickMenus.Set,
			TransformToolsMenu.Set,
			MaterialEditingMenu.Set,
			SaveLoadMenus.Set,
			OutputsMenu.Set,
			BottomBar.Set,
			UnsavedWorkMenu.Set,
			Transceiver_UI.Set
		},
		() => (
		Conglomerate(
			RightClickMenus.Windows,
			TransformToolsMenu.Windows,
			MaterialEditingMenu.Windows,
			SaveLoadMenus.Windows,
			OutputsMenu.Windows,
			BottomBar.Windows,
			UnsavedWorkMenu.Windows,
			Transceiver_UI.Windows
		),
		Conglomerate(
			RightClickMenus.Menus,
			MaterialEditingMenu.Menus,
			SaveLoadMenus.Menus,
			BottomBar.Menus,
			UnsavedWorkMenu.Menus
		))
	);

	static WindowCollection SimulatingWindows => 
		MakeCollection(
		typeof(Contexts.Simulating),
		new Action[] {
			SimulatingMainUI.Set
		},
		() => (
		Conglomerate(
			SimulatingMainUI.Windows
		),
		Conglomerate(
			SimulatingMainUI.Menus
		))
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