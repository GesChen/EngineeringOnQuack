using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using M = Config.UI.Menu;

// defo will change away from rcw name once i can think of another use and generalization for this
// yeah its changed to menu now
public class MenuUtil : MonoBehaviour {
	// this layout should be used for custom items too !!!!!!!!!!!!!!!!
	public static WindowItem.LayoutConfig WindowItemLayout(float windowWidth) =>
		WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredAt(UIPosition.TopLeft),
			new (windowWidth, M.ItemHeight),
			new (M.ItemPadding)
		);

	public class Window {
		public string Title;
		public bool ShowTitle;
		public float Width;
		public List<Item> Items;

		public CWindow CWindow;

		public Window(string title, float width, List<Item> items, bool showTitle = true) {
			Title = title;
			ShowTitle = showTitle;
			Width = width;
			Items = items;
		}
		public Window(float width, List<Item> items) {
			ShowTitle = false;
			Width = width;
			Items = items;
		}

		public Window AddUpdateToCW(Action action) {
			CWindow.AddUpdate(action);
			return this;
		}


		public class Item {
			public string Label;
			public string IconName;
			public bool HasIcon = false;
			public string Description;
			public bool HasDescription = false;

			public WindowItem RealItem;

			protected Item(string label, string description = null, string iconName = null) {
				Label = label;

				IconName = iconName;
				HasIcon = iconName != null;

				Description = description;
				HasDescription = description != null && description != "";
			}
		}

		public class Text : Item {
			public Text(string label, string description = null, string iconName = null)
				: base(label, description, iconName) { }
		}

		public class Button : Item {
			public PComponents.Button.ClickEvent OnButtonClick;

			public Button(PComponents.Button.ClickEvent onButtonClick, string label, string description = null, string iconName = null)
				: base(label, description, iconName) {
				OnButtonClick = onButtonClick;
			}
		}

		public class Flyout : Item {
			public CWindow SubWindow;

			public Flyout(Window subWindow, string label, string description = null, string iconName = null)
				: base(label, description, iconName) {
				SubWindow = subWindow.CWindow;
			}

			public Flyout(CWindow subWindow, string label, string description = null, string iconName = null)
				: base(label, description, iconName) {
				SubWindow = subWindow;
			}
		}

		public class CustomItem : Item {
			// doesn't acutally use item's stuff but inherits so it can be stored together
			public WindowItem item;

			public CustomItem(WindowItem item)
				: base("", null, null) {
				this.item = item;
			}
		}
	}

	public static CWindow[] ConvertWindows(params Window[] rcws) {
		CWindow[] converted = new CWindow[rcws.Length];
		for (int i = 0; i < rcws.Length; i++)
			converted[i] = ConvertWindow(rcws[i]);

		return converted;
	}

	public static CWindow ConvertWindow(Window rcw) {
		if (rcw == null) {
			Debug.LogError("Window is null!");
			return null;
		}

		CWindow cw = new() {
			Name = rcw.ShowTitle ? rcw.Title : "Menu",
			Config = new() {
				Resizable = false,
				Movable = false,
				ContentDynamic = true,
				DynamicPadding = FourSides.Even(Config.UI.RightClick.WindowPadding)
			}
		};

		List<WindowItem> items = new();

		// generate title
		if (rcw.ShowTitle) {
			var title = WindowItem.NewText(
				new(
					rcw.Title,
					fontSize: M.FontSize,
					alignment: TextAlignmentOptions.Center
					),
				WindowItem.LayoutConfig.FixedLayout(
					UIPosition.AnchoredAt(UIPosition.TopLeft),
					new(rcw.Width, M.TitleHeight)
					)
				);

			items.Add(title);
		}

		// generate items
		foreach (var item in rcw.Items)
			items.Add(GenerateItem(item, rcw));

		var finalLayoutItem =
			WindowItem.NewLayout(
				PComponents.Layout.VerticalDynamic(
					M.ItemSpacing,
					TextAnchor.UpperLeft
				),
				WindowItem.LayoutConfig.FillLayout,
				items
			);

		cw.Items = new[] { finalLayoutItem };
		rcw.CWindow = cw;

		cw.Config.IsFlyout = true; // all menus should be flyouts? might change this later idk

		return cw;
	}

	static WindowItem GenerateItem(Window.Item item, Window rcw) {
		WindowItem[] subs = new WindowItem[item.HasIcon ? 2 : 1];

		if (item.HasIcon) {
			var icon = WindowItem.NewImage(
				"Icon",
				new(Config.UI.Locations.IconsFolder + item.IconName),
				WindowItem.LayoutConfig.FixedLayout(
					UIPosition.AnchoredAt(UIPosition.MiddleLeft),
					new(M.IconSize, M.IconSize)
				)
			);

			subs[1] = icon;
		}

		var label = WindowItem.NewText(
			"Label",
			new(
				item.Label,
				fontSize: M.FontSize,
				alignment : TextAlignmentOptions.Left
			),
			WindowItem.LayoutConfig.DynamicLayout(
				new FourSides(0, 0, 0, M.IconSize + M.IconLabelSpacing),
				FourSides.Zero,
				FourSides.Zero
			)
		);
		subs[0] = label;

		WindowItem newItem = null;
		switch (item) {
			case Window.CustomItem ci:
				newItem = ci.item;
				break;

			case Window.Flyout flyout:
				var indicator = WindowItem.NewImage(new(),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.MiddleRight),
						new(M.FlyoutIndicatorSize, M.FlyoutIndicatorSize)
					)
				);

				CWindow subWindow = flyout.SubWindow;
				if (subWindow == null) 
					Debug.LogError($"Forgot to generate the subwindow of flyout {flyout.Label}");

				newItem = WindowItem.NewFlyoutTrigger(
					item.Label,
					new(subWindow, indicator),
					new(normalColor: Config.UI.Visual.BackgroundColor),
					WindowItemLayout(rcw.Width)
					).WithSubItems(subs)
					.AddComponents(new PComponents.FlyoutHider());
				break;

			case Window.Button button:

				newItem = WindowItem.NewButton(
					item.Label,
					new(
						button.OnButtonClick,
						normalColor: Config.UI.Visual.BackgroundColor
						),
					WindowItemLayout(rcw.Width)
					).WithSubItems(subs)
					.AddComponents(new PComponents.FlyoutHider());
				break;

			case Window.Text:
				newItem = WindowItem.NewText(
					item.Label,
					new(
						item.Label,
						fontSize: M.FontSize
						),
					WindowItemLayout(rcw.Width)
				).WithSubItems(subs);
				break;
		}
		if (item.HasDescription)
			newItem.AddDescription(item.Description);

		item.RealItem = newItem;

		return newItem;
	}
}