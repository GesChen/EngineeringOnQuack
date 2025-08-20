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

		public WindowCollection(Type context, CWindow[] windows) {
			Context = context;
			Windows = windows;
		}
	}

	static readonly WindowCollection EditingWindows = new(
		typeof(Contexts.Editing),
		new[] {
			RightClickMenus.Windows,
			TransformToolsMenu.Windows,
			MaterialEditingMenu.Windows,
			SaveLoadMenus.Windows,
			OutputsMenu.Windows,
			BottomBar.Windows,
		}
		.SelectMany(l => l)
		.ToArray()
	);

	public static WindowCollection[] WindowCollections = new[] {
		EditingWindows
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