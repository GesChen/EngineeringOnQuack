using System.Collections;
using System.Collections.Generic;
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

		public Window(string title, float width, List<Item> items) {
			Title = title;
			ShowTitle = true;
			Width = width;
			Items = items;
		}
		public Window(float width, List<Item> items) {
			ShowTitle = false;
			Width = width;
			Items = items;
		}

		public class Item {
			public string IconName;
			public bool HasIcon = false;
			public string Label;

			public Item(string iconName, string label) {
				IconName = iconName;
				HasIcon = true;
				Label = label;
			}

			public Item(string label) {
				HasIcon = false;
				Label = label;
			}
		}

		public class Button : Item {
			public delegate void ButtonClickEvent();
			public ButtonClickEvent OnButtonClick;

			public Button(string iconName, string label, ButtonClickEvent onButtonClick)
				: base(iconName, label) {
				OnButtonClick = onButtonClick;
			}

			public Button(string label, ButtonClickEvent onButtonClick)
				: base(label) {
				OnButtonClick = onButtonClick;
			}
		}

		public class Flyout : Item {
			public Window SubWindow;

			public Flyout(string iconName, string label, Window subWindow)
				: base(iconName, label) {
				SubWindow = subWindow;
			}

			public Flyout(string label, Window subWindow)
				: base(label) {
				SubWindow = subWindow;
			}
		}
	}

	public static CWindow ConvertWindow(Window rcw) {
		CWindow cw = new() {
			Name = rcw.ShowTitle ? rcw.Title : "Right Click Window",
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
				WindowItem.Components.Layout.VerticalDynamic(
					ItemSpacing,
					TextAnchor.UpperLeft
				),
				WindowItem.LayoutConfig.FillLayout,
				items
			);

		cw.Items = new[] { finalLayoutItem };

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

				CWindow subWindow = ConvertWindow(flyout.SubWindow);

				newItem = WindowItem.NewFlyoutTrigger(
					item.Label,
					new(subWindow, indicator),
					layout
				).WithSubItems(subs);
				break;

			case Window.Button button:
				newItem = WindowItem.NewButton(
					new(new() { new(button.OnButtonClick) }),
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

		return newItem;
	}
}