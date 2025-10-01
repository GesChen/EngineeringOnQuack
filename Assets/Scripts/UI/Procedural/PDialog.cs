using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// disposable dialog box, dont forget to destroy it or else it'll clog the viewport and memory
/// </summary>
public class PDialog {
	public string DialogTitle;
	public string DialogMessage;
	public Option[] DialogOptions;
	public WindowItem[] ExtraItems;

	public Vector2 Size;

	public struct Option {
		public string Label;
		public Action OnChosen;
		/// <param name="onChosen">can be null if this is ignore</param>
		public Option(string label, Action onChosen) {
			Label = label;
			OnChosen = onChosen;
		}
	}

	void ChooseOption(Option option) {
		option.OnChosen?.Invoke();

		Destroy();
	}

	/// <summary>
	/// Create a new PDialog object
	/// </summary>
	/// <param name="dialogMessage"></param>
	/// <param name="dialogOptions"></param>
	/// <param name="size"></param>
	/// <param name="extra">layout: LED</param>
	public PDialog(
		string dialogMessage,
		Option[] dialogOptions,
		Vector2 size,
		params WindowItem[] extra) {

		//DialogTitle = dialogTitle;
		DialogMessage = dialogMessage;
		DialogOptions = dialogOptions;
		Size = size;
		ExtraItems = extra;
	}

	public static CWindow GenerateDialog(PDialog pd) {
		pd.Set();

		WindowManager.Instance.RealiseWindows(pd.Window);

		return pd.Window;
	}

	public CWindow Window;

	public void Set() {
		WindowItem generateOption(Option option) =>
			WindowItem.NewButtonCustomText(
				new PComponents.Button(() => ChooseOption(option)),
				new PComponents.Text(option.Label,
					alignment: TMPro.TextAlignmentOptions.Center),
				WindowItem.LayoutConfig.LayoutElementDynamic()
			);

		var items = DialogOptions.Select(o => generateOption(o)).ToList();

		Window = new() {
			Name = $"Dialog \"{DialogTitle}\"",
			Config = new() {
				Resizable = true,
				Movable = true,
				Size = CWindow.Configuration.FreeSizeMinimum(Size),
				HideOnStart = false, // i think?
				Closable = false // ignore the problem of destroying on close
			},
			Items = new[] {
				WindowItem.NewLayout(
					PComponents.Layout.Vertical.Fixed(true, true),
					WindowItem.LayoutConfig.FillLayout,
					new[] {
						WindowItem.NewText(
							new PComponents.Text(
								DialogMessage,
								alignment: TMPro.TextAlignmentOptions.Center,
								wrap: true),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						).AddComponents(
							new PComponents.LayoutElement(
								Config.UI.PDialog.MessageFlexHeight
						))
						.Wrap()
					}.Concat(
						ExtraItems.Select(i => i.AddComponents(
							new PComponents.LayoutElement(Config.UI.PDialog.ExtraItemsFlexHeight)
							).Wrap())
					).Concat(
						new[] {
						WindowItem.NewLayout(
							PComponents.Layout.Horizontal.Fixed(true, true),
							WindowItem.LayoutConfig.LayoutElementDynamic(),
							items
						).AddComponents(
							new PComponents.LayoutElement(
								Config.UI.PDialog.OptionsFlexHeight
						))
						.Wrap()
					}).ToList()
				)
			}
		};
	}

	public void Destroy() {
		WindowManager.Instance.DestroyWindow(Window);
	}
}