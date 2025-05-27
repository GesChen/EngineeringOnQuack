using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

// defo will change away from rcw name once i can think of another use and generalization for this
// yeah its changed to menu now
public class MenuUtil : MonoBehaviour {
	static readonly float TitleHeight		= 30;
	static readonly float ItemSpacing		= 0;
	static readonly float ItemHeight		= 40;
	static readonly float ItemPadding		= 5;
	static readonly float IconSize			= 30;
	static readonly float IconLabelSpacing	= 10;
	static readonly float FlyoutIndicatorSize = 20;

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

		public class Item {
			public string Label;
			public string IconName;
			public bool HasIcon = false;
			public string Description;
			public bool HasDescription = false;

			public Item(string label, string description = null, string iconName = null) {
				Label = label;

				IconName = iconName;
				HasIcon = iconName != null;

				Description = description;
				HasDescription = description != null && description != "";
			}
		}

		public class Button : Item {
			public delegate void ButtonClickEvent();
			public ButtonClickEvent OnButtonClick;

			public Button(ButtonClickEvent onButtonClick, string label, string description = null, string iconName = null)
				: base(label, description, iconName) {
				OnButtonClick = onButtonClick;
			}
		}

		public class Flyout : Item {
			public Window SubWindow;

			public Flyout(Window subWindow, string label, string description = null, string iconName = null)
				: base(label, description, iconName) {
				SubWindow = subWindow;
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
					TextAlignmentOptions.Center
					),
				WindowItem.LayoutConfig.FixedLayout(
					UIPosition.AnchoredAt(UIPosition.TopLeft),
					new(rcw.Width, TitleHeight)
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
					ItemSpacing,
					TextAnchor.UpperLeft
				),
				WindowItem.LayoutConfig.FillLayout,
				items
			);

		cw.Items = new[] { finalLayoutItem };
		rcw.CWindow = cw;

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
					new(IconSize, IconSize)
				)
			);

			subs[1] = icon;
		}

		var label = WindowItem.NewText(
			"Label",
			new(
				item.Label,
				TextAlignmentOptions.Left
			),
			WindowItem.LayoutConfig.DynamicLayout(
				new FourSides(0, 0, 0, IconSize + IconLabelSpacing),
				FourSides.Zero,
				FourSides.Zero
			)
		);
		subs[0] = label;

		var layout = WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredAt(UIPosition.TopLeft),
			new(rcw.Width, ItemHeight),
			new(ItemPadding)
		);

		WindowItem newItem = null;
		switch (item) {
			case Window.Flyout flyout:
				var indicator = WindowItem.NewImage(new(),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.MiddleRight),
						new(FlyoutIndicatorSize, FlyoutIndicatorSize)
					)
				);

				CWindow subWindow = flyout.SubWindow.CWindow;
				if (subWindow == null) 
					Debug.LogError($"Forgot to generate the subwindow of flyout {flyout.Label}");

				newItem = WindowItem.NewFlyoutTrigger(
						item.Label,
						new(subWindow, indicator),
						layout
					).WithSubItems(subs);
				break;

			case Window.Button button:
				List<UnityEngine.Events.UnityAction> action;
				if (button.OnButtonClick != null)
					action = new() { new(button.OnButtonClick) };
				else action = new() { };
				
				newItem = WindowItem.NewButton(
						new(action),
						layout
					).WithSubItems(subs);
				break;

			case Window.Item:
				newItem = WindowItem.NewImage(
					new(Config.UI.Visual.BackgroundColor),
					layout
				).WithSubItems(subs);
				break;
		}
		if (item.HasDescription)
			newItem.AddDescription(item.Description);

		return newItem;
	}
}