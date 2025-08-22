using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using M = Config.UI.Menu;

// defo will change away from rcw name once i can think of another use and generalization for this
// yeah its changed to menu now
public class PMenu {
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

		public void Reset() {
			m_CWindow = null;
			m_customizationComponent = null;
		}

		// extra spacing between items
		public float ExtraSpacing = 0;

		private bool RegenerateRequested = false;
		public void RequestRegeneration() => RegenerateRequested = true;

		private CWindow m_CWindow;
		public CWindow CWindow {
			get {
				if (m_CWindow == null)
					m_CWindow = GenerateCWindow(this);
				else if (RegenerateRequested)
					UpdateWindow(ref m_CWindow, this);
				
				RegenerateRequested = false;

				return m_CWindow;
			}
			// no setter because it is done by the getter
		}

		

		#region Customization
		// i am rewriting like half of this at 11:58 pm i woke at 3:45 
		// and maybeslept for like 1-2 hrs on the plane the code is going 
		// to be bad and i do very much realise it alreeady
		// the word "customization" has lost all meaning already
		public bool Customizable = false;

		public CustomizationData Customization; // null

		public void CustomizeIfAble(CustomizationData customization) {
			if (Customizable)
				Customize(customization);
		}

		/// <summary>
		/// Sets menus customization and updates the component
		/// </summary>
		public void Customize(CustomizationData customization) {
			Customization = customization;

			if (customization.Indices != null)
				CustomizationComponent.UpdateActiveState(Customization.Indices);

			if (customization.Width.HasValue)
				CustomizationComponent.UpdateWidth(Customization.Width.Value);
		}
		
		private CustomizableMenu m_customizationComponent;
		internal CustomizableMenu CustomizationComponent {
			get {
				if (!Customizable) throw new("Tried getting Customization Component of a non customizable window");
				if (m_customizationComponent == null) throw new("Customization Component not created!");
				return m_customizationComponent;
			}
			set { m_customizationComponent = value; }
		}

		public class CustomizationData {
			public int[] Indices;
			public float? Width;
		}
		#endregion
		public Window(
			string title, 
			float width, 
			List<Item> items, 
			bool showTitle = true, 
			bool movable = false,
			bool isFlyout = true,
			bool closable = false,
			bool hideOnStart = true,
			bool switchable = false,
			float extraSpacing = 0) {

			Title = title;
			ShowTitle = showTitle;
			Width = width;
			Items = items;
			
			Movable = movable;
			IsFlyout = isFlyout;
			Closable = closable;
			HideOnStart = hideOnStart;
			Customizable = switchable;

			ExtraSpacing = extraSpacing;
		}

		public Window AddEventToCW(
			TimedEventInvoker.Timing timing, 
			TimedEventInvoker.TimedEventCall action) {

			CWindow.AddEvent(timing, action);
			return this;
		}

		public abstract class Item {
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

			public event Action<WindowItem> RealItemMadeEvent;
			public Item OnRealItemMade(Action<WindowItem> action) {
				RealItemMadeEvent += action;
				return this;
			}
			public void RealItemMade(WindowItem item) {
				RealItem = item;
				RealItemMadeEvent?.Invoke(item);
			}

			public abstract WindowItem ConvertToItem(WindowItem[] subs, float width);
		}

		public class Text : Item {
			public Text(
				string label,
				string description = null,
				string iconName = null,
				string iconPath = null,
				Sprite iconSprite = null)
				: base(label, description, iconName, iconPath, iconSprite) { }

			public override WindowItem ConvertToItem(WindowItem[] subs, float width) {
				return WindowItem.NewText(
					Label,
					new(
						Label,
						fontSize: M.FontSize
						),
					WindowItemLayout(width)
				).SetSubItems(subs);
			}
		}

		public class Button : Item {
			public event Action OnButtonClick;

			public Button(
				Action onButtonClick,
				string label,
				string description = null,
				string iconName = null,
				string iconPath = null,
				Sprite iconSprite = null)
				: base(label, description, iconName, iconPath, iconSprite) {

				OnButtonClick = onButtonClick;
			}

			public void ButtonClicked() {
				OnButtonClick?.Invoke();
			}

			public override WindowItem ConvertToItem(WindowItem[] subs, float width) {
				return WindowItem.NewButton(
					Label,
					new(
						ButtonClicked,
						normalColor: Config.UI.Visual.BackgroundColor
						),
					WindowItemLayout(width)
					).SetSubItems(subs)
					.AddComponents(new PComponents.FlyoutHider());
			}
		}

		public class InputField : Item {
			public event Action<string> OnValueChanged;
			public InputField(
				Action<string> onValueChanged,
				string label,
				string description = null,
				string iconName = null,
				string iconPath = null,
				Sprite iconSprite = null)
				: base(label, description, iconName, iconPath, iconSprite) {
				OnValueChanged = onValueChanged;
			}
			public void InputFieldChanged(string newValue) {
				OnValueChanged?.Invoke(newValue);
			}

			public override WindowItem ConvertToItem(WindowItem[] subs, float width) {
				return WindowItem.NewInputField(
					Label,
					new PComponents.InputField(
						InputFieldChanged,
						Label,
						fontSize: M.FontSize),
					WindowItemLayout(width)
					).SetSubItems(subs);

			}
		}

		public class Flyout : Item {
			public CWindow SubCWindow;
			public Window SubWindow;
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

				SubWindow = subWindow;
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

				SubCWindow = subWindow;
				AddIndicator = addIndicator;
			}

			public override WindowItem ConvertToItem(WindowItem[] subs, float width) {
				WindowItem indicator = null;

				if (AddIndicator) {
					indicator = WindowItem.NewImage(
						$"Flyout Indicator {DateTime.Now.Second}",
						new(),
						WindowItem.LayoutConfig.FixedLayout(
							UIPosition.AnchoredAt(UIPosition.MiddleRight),
							new(M.FlyoutIndicatorSize, M.FlyoutIndicatorSize)
						)
					);
				}

				CWindow window = SubCWindow;
				window ??= SubWindow.CWindow;
				//Debug.Log($"call convert on {Label} cw {window.CreationTime}");

				if (window == null)
					Debug.LogError($"Forgot to generate the subwindow of flyout {Label}");

				var newitem = WindowItem.NewFlyoutTrigger(
					Label,
					new PComponents.FlyoutTrigger(window, indicator),
					WindowItemLayout(width),
					new PComponents.HoverTarget(normalColor: Config.UI.Visual.BackgroundColor)
					).SetSubItems(subs)
					.AddComponents(new PComponents.FlyoutHider());
				return newitem;
			}
		}

		public class CustomItem : Item {
			// doesn't acutally use item's stuff but inherits so it can be stored together
			public WindowItem item;

			public CustomItem(WindowItem item)
				: base("", null, null) {
				this.item = item;
			}

			public override WindowItem ConvertToItem(WindowItem[] subs, float width) {
				return item;
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

	private static CWindow GenerateCWindow(Window rcw) {
		CWindow cw = new();
		UpdateWindow(ref cw, rcw);
		return cw;
	}

	// workaround to prevent creating a new cwindow object when regenerating
	// and keep referneces to original
	private static void UpdateWindow(ref CWindow cw, Window rcw) {
		if (rcw == null) {
			throw new ("Window is null!");
		}

		cw.Name =
			rcw.Title != null
			? $"[M] {rcw.Title}"
			: "Menu";

		cw.Config = new() {
			Resizable = false,
			Movable = rcw.Movable,
			ContentDynamic = true,
			DynamicPadding = FourSides.Even(Config.UI.RightClick.WindowPadding),
			IsFlyout = rcw.IsFlyout,
			Closable = rcw.Closable,
			HideOnStart = rcw.HideOnStart,
		};

		List<WindowItem> items = new();

		// generate title
		WindowItem title = null;
		if (rcw.ShowTitle) {
			title = WindowItem.NewText(
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
					M.ItemSpacing + rcw.ExtraSpacing,
					TextAnchor.UpperLeft
				),
				WindowItem.LayoutConfig.FillLayout,
				items
			);

		cw.Items = new[] { finalLayoutItem };

		//cw.Config.IsFlyout = true; // all menus should be flyouts? might change this later idk
		// yeah i changed it

		// add dynamic menu and items
		if (rcw.Customizable) {
			var tempcw = cw;

			cw.AddEvent(TimedEventInvoker.Timing.Start, (_) => {
				rcw.CustomizationComponent =
					tempcw.RealisedWindow.gameObject.AddComponent<CustomizableMenu>();

				rcw.CustomizationComponent.title = title;
				rcw.CustomizationComponent.items = new(items);

				if (rcw.ShowTitle) // don't want to include the title
					rcw.CustomizationComponent.items.RemoveAt(0);
			});
		}
	}

	static WindowItem GenerateItem(Window.Item item, Window rcw) {
		List<WindowItem> subList = new();

		// add label if its not empty or null or input field
		if (!(item.Label == null || item.Label == "" || item is Window.InputField)) {
			var label = WindowItem.NewText(
				"Label",
				new(
					item.Label,
					fontSize: M.FontSize,
					alignment : TextAlignmentOptions.Left
				),
				WindowItem.LayoutConfig.DynamicLayout(
					margin: new FourSides(0, 0, 0, M.IconSize + M.IconLabelSpacing)
				)
			);
			subList.Add(label);
		}

		// add icon if it exists
		WindowItem icon;
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
		WindowItem newItem = item.ConvertToItem(subs, rcw.Width);

		if (item.HasDescription)
			newItem.AddDescription(item.Description);

		item.RealItemMade(newItem);

		return newItem;
	}
}