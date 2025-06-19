using System;
using System.Collections.Generic;
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

		public bool Movable = false;
		public bool IsFlyout = true;
		public bool Closable = false;
		public bool HideOnStart = true;
		public bool Switchable = false;

		private CWindow m_cwindow;
		public CWindow CWindow {
			get {
				m_cwindow ??= ConvertWindow(this);
				return m_cwindow;
			}
			// no setter because it is done by the getter
		}

		private SwitchableMenu m_dynamicComponent;
		public SwitchableMenu SwitchingComponent {
			get {
				if (!Switchable) throw new("Tried getting Dynamic Component of a non dynamic window");
				if (m_dynamicComponent == null) throw new("Dynamic Component not created!");
				return m_dynamicComponent;
			}
			internal set { m_dynamicComponent = value; }
		}

		public Window(
			string title, 
			float width, 
			List<Item> items, 
			bool showTitle = true, 
			bool movable = false,
			bool isFlyout = true,
			bool closable = false,
			bool hideOnStart = true,
			bool switchable = false) {

			Title = title;
			ShowTitle = showTitle;
			Width = width;
			Items = items;
			
			Movable = movable;
			IsFlyout = isFlyout;
			Closable = closable;
			HideOnStart = hideOnStart;
			Switchable = switchable;
		}
		public Window(float width, List<Item> items) {
			ShowTitle = false;
			Width = width;
			Items = items;
		}

		public Window AddEventToCW(
			TimedEventInvoker.Timing timing, 
			TimedEventInvoker.TimedEventCall action) {

			CWindow.AddEvent(timing, action);
			return this;
		}

		public class Item {
			public string Label;
			
			// repr for icons cuz there are multiple ways to represent an icon
			public struct IconType {
				public bool Exists;
				public string Name;
				public string Path;
				public Sprite Sprite;
			}
			public IconType Icon;
			public bool HasIcon = false;
			public string Description;
			public bool HasDescription = false;
			public List<WindowItem> ExtraSubItems;

			public WindowItem RealItem;

			protected Item(
				string label,
				string description = null,
				string iconName = null,
				string iconPath = null,
				Sprite iconSprite = null) {
				Label = label;

				Icon = new() {
					Exists = iconName != null || iconPath != null || iconSprite != null,
					Name = iconName,
					Path = iconPath,
					Sprite = iconSprite
				};

				HasIcon = Icon.Exists;

				Description = description;
				HasDescription = description != null && description != "";
			}

			public Item AddSubItems(params WindowItem[] subs) {
				ExtraSubItems ??= new();

				ExtraSubItems.AddRange(subs);
				return this;
			}
		}

		public class Text : Item {
			public Text(
				string label,
				string description = null,
				string iconName = null,
				string iconPath = null,
				Sprite iconSprite = null)
				: base(label, description, iconName, iconPath, iconSprite) { }
		}

		public class Button : Item {
			public PComponents.Button.ClickEvent OnButtonClick;

			public Button(
				PComponents.Button.ClickEvent onButtonClick,
				string label,
				string description = null,
				string iconName = null,
				string iconPath = null,
				Sprite iconSprite = null)
				: base(label, description, iconName, iconPath, iconSprite) {

				OnButtonClick = onButtonClick;
			}
		}

		public class Flyout : Item {
			public CWindow SubWindow;
			public bool AddIndicator;

			public Flyout(
				Window subWindow,
				string label,
				bool addIndicator = true,
				string description = null,
				string iconName = null,
				string iconPath = null,
				Sprite iconSprite = null)
				: base(label, description, iconName, iconPath, iconSprite) {

				SubWindow = subWindow.CWindow;
				AddIndicator = addIndicator;
			}

			public Flyout(CWindow subWindow,
				string label,
				bool addIndicator = true,
				string description = null,
				string iconName = null,
				string iconPath = null,
				Sprite iconSprite = null)
				: base(label, description, iconName, iconPath, iconSprite) {

				SubWindow = subWindow;
				AddIndicator = addIndicator;
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

	/*
	public static CWindow[] ConvertWindows(params Window[] rcws) {
		CWindow[] converted = new CWindow[rcws.Length];
		for (int i = 0; i < rcws.Length; i++)
			converted[i] = ConvertWindow(ref rcws[i]);

		return converted;
	}*/

	public static CWindow ConvertWindow(Window rcw) {
		if (rcw == null) {
			Debug.LogError("Window is null!");
			return null;
		}

		CWindow cw = new() {
			Name = 
				rcw.Title != null
				? $"[M] {rcw.Title}"
				: "Menu",
			Config = new() {
				Resizable = false,
				Movable = rcw.Movable,
				ContentDynamic = true,
				DynamicPadding = FourSides.Even(Config.UI.RightClick.WindowPadding),
				IsFlyout = rcw.IsFlyout,
				Closable = rcw.Closable,
				HideOnStart = rcw.HideOnStart,
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
				PComponents.Layout.Vertical.Dynamic(
					M.ItemSpacing,
					TextAnchor.UpperLeft
				),
				WindowItem.LayoutConfig.FillLayout,
				items
			);

		cw.Items = new[] { finalLayoutItem };

		//cw.Config.IsFlyout = true; // all menus should be flyouts? might change this later idk
		// yeah i changed it

		// add dynamic menu and items
		if (rcw.Switchable) {
			cw.AddEvent(TimedEventInvoker.Timing.Start, (_) => {
				rcw.SwitchingComponent =
					cw.RealisedWindow.gameObject.AddComponent<SwitchableMenu>();

				rcw.SwitchingComponent.items = new(items);

				if (rcw.ShowTitle) // don't want to include the title
					rcw.SwitchingComponent.items.RemoveAt(0);
			});
		}
		return cw;
	}

	static WindowItem GenerateItem(Window.Item item, Window rcw) {
		List<WindowItem> subList = new();

		// add label if its not empty or null
		if (item.Label != null && item.Label != "") {
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
			subList.Add(label);
		}

		// add icon if it exists
		WindowItem icon = null;
		if (item.HasIcon) {

			// get the pimage 
			PComponents.Image image;
			if (item.Icon.Sprite != null)
				image = new(item.Icon.Sprite);
			else if (item.Icon.Path != null)
				image = new(item.Icon.Path);
			else
				image = new(Config.Locations.IconsFolder + item.Icon.Name);

			icon = WindowItem.NewImage(
				"Icon",
				image,
				WindowItem.LayoutConfig.FixedLayout(
					UIPosition.AnchoredAt(UIPosition.MiddleLeft),
					new(M.IconSize, M.IconSize)
				)
			);

			subList.Add(icon);
		}

		// any extras
		if (item.ExtraSubItems != null && item.ExtraSubItems.Count != 0)
			subList.AddRange(item.ExtraSubItems);

		WindowItem[] subs = subList.ToArray();

		// actually generate the WI
		WindowItem newItem = null;
		switch (item) {
			case Window.CustomItem ci:
				newItem = ci.item;
				break;

			case Window.Flyout flyout:
				WindowItem indicator = null;
				
				if (flyout.AddIndicator)
					indicator = WindowItem.NewImage(new(),
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
					).SetSubItems(subs)
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
					).SetSubItems(subs)
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
				);

				if (icon != null)
					newItem.AddSubItems(icon);

				break;
		}
		if (item.HasDescription)
			newItem.AddDescription(item.Description);

		item.RealItem = newItem;

		return newItem;
	}
}