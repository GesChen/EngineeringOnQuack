using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ContextWindows {
	// i highk cant think of a better name for this 

	internal static WindowCollection GetCollection(string name) =>
		name switch {
			"playing" => PlayingWindows,
			"editing" => EditingWindows,
			"operating" => OperatingWindows,
			"paused" => PausedWC,
			"mainmenu" => MainMenuWC,
			_ => throw new($"invalid collection name {name}")
		};

	static WindowCollection PlayingWindows =>
		MakeCollection(
		new Action[] {
			PlayingMainUI.Set,
			PauseUI.Set
		},
		() => (
		Conglomerate(
			PlayingMainUI.Windows
		),
		new PMenu.Window[0]));

	static WindowCollection EditingWindows => 
		MakeCollection(
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

	static WindowCollection OperatingWindows => 
		MakeCollection(
		new Action[] {
			OperatingMainUI.Set,
			PlayingMainUI.SetSC
		},
		() => (
		Conglomerate(
			OperatingMainUI.Windows,
			new[] { PlayingMainUI.SitControl }
		),
		Conglomerate(
			OperatingMainUI.Menus
		))
	);

	static WindowCollection PausedWC =>
		MakeCollection(
		new Action[] { PauseUI.Set },
		() => (PauseUI.Windows, new PMenu.Window[0])
		);

	static WindowCollection MainMenuWC =>
		MakeCollection(
		new Action[] { MainMenuUI.Set },
		() => (MainMenuUI.Windows, new PMenu.Window[0])
		);

	public struct WindowCollection {
		public CWindow[] Windows;
		public PMenu.Window[] Menus;
		public Action[] Sets;

		public WindowCollection(
			CWindow[] windows,
			PMenu.Window[] menus,
			Action[] sets) {

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
		Action[] sets,
		Func<(CWindow[] windows, PMenu.Window[] menus)> getter
		) {

		foreach (var setter in sets) {
			setter();
		}

		var (windows, menus) = getter();

		return new(windows, menus, sets);
	}

	static T[] Conglomerate<T>(params T[][] lists) =>
		lists
		.SelectMany(l => l)
		.ToArray();
}