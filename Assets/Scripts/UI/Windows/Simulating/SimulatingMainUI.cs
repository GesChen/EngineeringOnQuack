using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using W = PMenu.Window;

public static class SimulatingMainUI {

	public static class TopBar {

		// todo: consolidate bar values into config
		static readonly float size = 50;
		static readonly float innerpadding = 10;

		static readonly float listboxheight = 150;

		public static void ClearReturnToEditing() { OnReturnToEditing = null; }
		public static event Action OnReturnToEditing;

		public static event Action OnHideAll;
		public static event Action OnShowAll;

		private static TextMeshProUGUI NameText;
		public static void SetName(string name) {
			NameText.text = name;
		}

		public static void ClearBarCreated() { OnBarCreated = null; }
		public static event Action OnBarCreated;

		public static void ClearRequestOutputs() { OnRequestOutputs = null; }
		public static event Action OnRequestOutputs;
		internal static bool outputsUpdated = false;

		public static void UpdateOutputs(string[] names) {
			OutputsLayoutItem.SetSubItems(
				names.Select(OutputItem).ToArray() // LMAOOOOO THIS WORKS????? OK??? THE SIGNATURES MATCH IG??? LMAOO
			);

			Outputs.RequestRegeneration();

			WindowRealiser.Instance.UpdateWindow(Outputs.CWindow);
		}

		static Dictionary<int, Image> ToggleIcons = new();
		public static void ClearItemToggled() { OnItemToggled = null; }
		public static event Action<int> OnItemToggled;

		public static WindowItem OutputsLayoutItem;
		public static W Outputs;
		internal static void SetOutputs() {
			Outputs = new W(
				"Outputs", 250, new() {
					new W.CustomItem(
						WindowItem.NewLayout(
							PComponents.Layout.Horizontal.Fixed(true, true),
							PMenu.WindowItemLayout(250),
							new() {
								WindowItem.NewButtonCustomText(
									new PComponents.Button(() => OnHideAll?.Invoke()),
									new PComponents.Text(
										"Hide All",
										alignment: TMPro.TextAlignmentOptions.Center
									),
									WindowItem.LayoutConfig.LayoutElementDynamic()
								),
								WindowItem.NewButtonCustomText(
									new PComponents.Button(() => OnShowAll?.Invoke()),
									new PComponents.Text(
										"Show All",
										alignment: TMPro.TextAlignmentOptions.Center
									),
									WindowItem.LayoutConfig.LayoutElementDynamic()
								)
							}
						)
					),
					new W.CustomItem(
						WindowItem.NewScrollView(
							new PComponents.ScrollView(
								horizontalScrolling: false
							),
							WindowItem.LayoutConfig.FixedLayout(
								UIPosition.AnchoredAt(UIPosition.TopLeft),
								new(250, listboxheight),
								new FourSides(10)
							),
							new() {
								WindowItem.NewLayout(
									PComponents.Layout.Vertical.Fixed(
										false,
										true
									),
									WindowItem.LayoutConfig.Custom(
										position: new(1, 0, 0, 0),
										sizeDelta: new(0, 0)
									),
									new() { }
								).OnRealized((_, wi) => OutputsLayoutItem = wi )
							}
						)
					)
				}
			).SetCWEvents(
				new TimedEventInvoker.TimedEvent(
					TimedEventInvoker.Timing.Awake,
					(_) => {
						if (!outputsUpdated) {
							outputsUpdated = true;
							OnRequestOutputs?.Invoke();
						}
					}
				)
			);
		}

		public static WindowItem OutputItem(string name, int i) =>
			WindowItem.NewButton(
				new PComponents.Button(() => OnItemToggled?.Invoke(i)),
				WindowItem.LayoutConfig.LayoutElement(
					Config.UI.Menu.ItemHeight * Vector2.one,
					new(Config.UI.Menu.ItemPadding)
				)
			).SetSubItems(
				WindowItem.NewImage( // indicator icon
					new PComponents.Image(),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.MiddleLeft),
						Config.UI.Menu.IconSize * Vector2.one
					)
				).OnRealized((rt, _) =>
					ToggleIcons[i] = rt.GetComponent<Image>()
				),
				WindowItem.NewText( // label
					new PComponents.Text(name),
					WindowItem.LayoutConfig.DynamicLayout(
						margin: new FourSides(0, 0, 0, Config.UI.Menu.IconSize + Config.UI.Menu.IconLabelSpacing)
					)
				)
			);

		public static CWindow Bar;
		internal static void SetBar() {
			Bar = new() {
				Name = "Top Bar",
				Config = new() {
					Resizable = false,
					Movable = false,
					Size = CWindow.Configuration.FixedSize(new(0, size)),
					Position = new(
						new(0, 1),
						new(1, 1),
						new(.5f, 1),
						new(0, 0)
					),
					Closable = false,
					HideOnStart = false
				},
				Items = new WindowItem[] {
				WindowItem.NewLayout(
					PComponents.Layout.Horizontal.Fixed(
						true,
						true,
						10
						),
					WindowItem.LayoutConfig.DynamicLayout(
						padding: new(innerpadding)
					),
					new(){
UIBarUtils.DynamicBarButton(2, "Return to Editing", () => OnReturnToEditing?.Invoke()),
UIBarUtils.DynamicBarSpace(1),
UIBarUtils.DynamicBarText(3, "name", .5f)
	.OnRealized((_, wi) => { // get the ugui component off subitem 0 
		NameText = wi.SubItems[0]
		.GetComponent<PComponents.Text>().RealComponent
		as TextMeshProUGUI;
		}),
UIBarUtils.DynamicBarSpace(1),
UIBarUtils.DynamicBarFlyout(2, "Outputs", Outputs.CWindow, 2, false)
					})
				},
				CustomEvents = new() {
					new TimedEventInvoker.TimedEvent(
						TimedEventInvoker.Timing.Awake,
						(_) => {
		Bar.RealisedWindow.backgroundImage.enabled = false;
		OnBarCreated?.Invoke();
						})
				}
			};
		}
	}

	public static void Set() {
		TopBar.SetOutputs();
		TopBar.SetBar();

		TopBar.outputsUpdated = false;
	}
	public static CWindow[] Windows => new[] {
		TopBar.Bar,
		TopBar.Outputs.CWindow
	};
	public static W[] Menus => new[] {
		TopBar.Outputs
	};
}