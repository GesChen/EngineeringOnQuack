using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using W = PMenu.Window;

public static class OperatingMainUI {

	public static class TopBar {

		// todo: consolidate bar values into config
		static readonly float size = 50;
		static readonly float innerpadding = 10;

		static readonly float listboxheight = 150;

		public static Action OnExitPressed;
		public static Action OnEditPressed;
		public static Action OnDestroyPressed;

		private static TextMeshProUGUI NameText;
		public static void SetName(string name) {
			NameText.text = name;
		}

		public static Action OnBarCreated;

		public static class Outputs {
			public static Dictionary<string, OutputWindow> OutputWindows = new();
			
			static readonly Vector2 OutputDefaultSize = new(200, 300);
			static readonly Vector2 OutputMinSize = new(100, 50);

			public static Action OnHideAll;
			public static Action OnShowAll;

			public static Action OnRequestOutputs;
			internal static bool outputsUpdated = false;

			public static void UpdateOutputs(string[] names) {
				OutputsLayoutItem.SetSubItems(
					names.Select(OutputItem).ToArray() // LMAOOOOO THIS WORKS????? OK??? THE SIGNATURES MATCH IG??? LMAOO
				);

				Window.RequestRegeneration();

				WindowRealiser.Instance.UpdateWindow(Window.CWindow);
			}

			static readonly Dictionary<string, Image> ToggleIcons = new();
			public static void ClearItemToggled() { OnItemToggled = null; }
			public static Action<string> OnItemToggled;

			static Sprite m_VisibleIcon;
			static Sprite VisibleIcon => HF.LoadResource(ref m_VisibleIcon, Config.UI.Sprites.OutputVisible);
			
			static Sprite m_HiddenIcon;
			static Sprite HiddenIcon => HF.LoadResource(ref m_HiddenIcon, Config.UI.Sprites.OutputHidden);

			public static void UpdateOutputStates((string name, bool state)[] states) {
				foreach (var (name, state) in states) {
					if (ToggleIcons.TryGetValue(name, out var img))
						img.sprite = state ? VisibleIcon : HiddenIcon;
					
					if (OutputWindows.Values.TryFind(w => w.Name == name, out var window)) 
						window.Window.RealisedWindow.SetState(state);
				}
			}

			public static WindowItem OutputsLayoutItem;
			public static W Window;
			internal static void SetWindow() {
				Window = new W(
					"Outputs", 250, true, new() {
					new W.CustomItem(
						WindowItem.NewLayout(
							PComponents.Layout.Horizontal.Fixed(true, true),
							PMenu.WindowItemLayout(250),
							new() {
								WindowItem.NewButtonCustomText(
									new PComponents.Button(() => OnHideAll?.Invoke()),
									new PComponents.Text(
										"Hide All",
										alignment: TextAlignmentOptions.Center
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
								RequestOutputWindowsGeneration?.Invoke();
							}
						}
					)
				);
			}

			public static WindowItem OutputItem(string name) =>
				WindowItem.NewButton(
					new PComponents.Button(() => OnItemToggled?.Invoke(name)),
					WindowItem.LayoutConfig.LayoutElement(
						Config.UI.Menu.ItemHeight * Vector2.one,
						new(Config.UI.Menu.ItemPadding)
					)
				).SetSubItems(
					WindowItem.NewImage( // indicator icon
						new PComponents.Image(
							HiddenIcon
						),
						WindowItem.LayoutConfig.FixedLayout(
							UIPosition.AnchoredAt(UIPosition.MiddleLeft),
							Config.UI.Menu.IconSize * Vector2.one
						)
					).OnRealized((rt, _) =>
						ToggleIcons[name] = rt.GetComponent<Image>()
					),
					WindowItem.NewText( // label
						new PComponents.Text(name),
						WindowItem.LayoutConfig.DynamicLayout(
							margin: new FourSides(0, 0, 0, Config.UI.Menu.IconSize + Config.UI.Menu.IconLabelSpacing)
						)
					)
				);

			public class OutputWindow {
				public string Name;
				public CWindow Window;
				public RectTransform ContentsRect;
				private List<RectTransform> LineObjects = new();

				public void AddLine(string data) {
					var newLine = WindowRealiser.Instance.RealiseItem(
						WindowItem.NewText(
							new PComponents.Text(
								data,
								alignment: TextAlignmentOptions.Left
							),
							WindowItem.LayoutConfig.LayoutElement(
								new(0, Config.UI.Menu.ItemHeight)
							)
						),
						ContentsRect
					);

					LineObjects.Add(newLine);

					if (LineObjects.Count > Config.Language.MaxOutputHistory) {
						UnityEngine.Object.Destroy(LineObjects[0].gameObject);
						LineObjects.RemoveAt(0);
					}
					
				}
			}

			public static Action RequestOutputWindowsGeneration;
			// uses menu config sizes
			public static OutputWindow GenerateOutputWindow(string name, int uses) {
				var window = new CWindow {
					Name = $"Output {name} ({uses} uses)",
					Config = new() {
						Resizable = true,
						Movable = true,
						Size = CWindow.Configuration.FreeSizeMinimum(
							OutputDefaultSize,
							OutputMinSize
						),
						Position = UIPosition.AnchoredAt(UIPosition.MiddleCenter),
						Closable = true
					},
					Items = new WindowItem[] {
						WindowItem.NewEmpty(
							WindowItem.LayoutConfig.DynamicLayout(
								padding: FourSides.Even(5) // too much work to turn this into a config
							),
							new() {
								WindowItem.NewText(
									"Name",
									new PComponents.Text(
										$"{name} <sub>{uses} uses</sub>", // may be changed
										alignment: TextAlignmentOptions.Left
									),
									WindowItem.LayoutConfig.Custom(
										position: new(1, 0, 0, 0),
										sizeDelta: new(0, Config.UI.Menu.TitleHeight),
										fixedPosition: new() {
											Pivot = UIPosition.TopCenter
										}
									)
								),
								WindowItem.NewScrollView(
									new PComponents.ScrollView(
										horizontalScrolling: false
									),
									WindowItem.LayoutConfig.DynamicLayout(
										margin:
								(Config.UI.Menu.TitleHeight + Config.UI.Menu.ItemSpacing) * FourSides.UpConst
									),
									new(){
										WindowItem.NewLayout(
											PComponents.Layout.Vertical.Fixed(
												false,
												true
												),
											WindowItem.LayoutConfig.Custom(
												position: new(1, 0, 0, 0),
												sizeDelta: new(0, 0)
											),
											new()
										).OnRealized((rt, _) =>
											OutputWindows[name].ContentsRect = rt
										)
									}
								)
							})
					}
				};

				OutputWindows[name] = new() {
					Name = name,
					Window = window,
					ContentsRect = null // set later
				};

				window.SetGroup("Outputs");
				WindowManager.Instance.RealiseWindows(window);

				return OutputWindows[name];
			}
		}

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
UIBarUtils.DynamicBarButton(1, "Exit", () => OnExitPressed?.Invoke()),
UIBarUtils.DynamicBarButton(1, "Edit", () => OnEditPressed?.Invoke()),
UIBarUtils.DynamicBarButton(1, "Destroy", () => OnDestroyPressed?.Invoke()),
UIBarUtils.DynamicBarSpace(.5f),
UIBarUtils.DynamicBarText(3, "name", .5f)
	.OnRealized((_, wi) => { // get the ugui component off subitem 0 
		NameText = wi.SubItems[0]
		.GetComponent<PComponents.Text>().RealComponent
		as TextMeshProUGUI;
		}),
UIBarUtils.DynamicBarSpace(1),
UIBarUtils.DynamicBarFlyout(2, "Outputs", Outputs.Window.CWindow, 2, true)
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
		TopBar.Outputs.outputsUpdated = false;
		TopBar.Outputs.SetWindow();
		TopBar.SetBar();
	}
	public static CWindow[] Windows => new[] {
		TopBar.Bar,
		TopBar.Outputs.Window.CWindow
	};
	public static W[] Menus => new[] {
		TopBar.Outputs.Window
	};
}