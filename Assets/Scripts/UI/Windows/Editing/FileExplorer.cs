using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FileExplorer {
	
	// config
	static string IconPath = "Icons/add to group";
	static string UseButtonLabel = "Load"; // to be changed per instance
	static float NameWidth = 5;

	// properties will be instance members too
	static float FooterItemsHeights = 30;
	static float ItemHeight = 30;
	static float IconNameSpacing = 10;
	static WindowItem ItemsLayout;

	// state
	static string CurrentDirectory;
	static EntryData[] CurrentEntries;

	static string[] LastLoadExtensions;
	static Func<string, (string data, float width)[]> LastLoadMetadataGetter;

	// temp, signatures to change
	static event Action OnUsePressed;

	static int CurrentlySelected = -1;
	static string CurrentFieldContents = "";
	static TMP_InputField InputField;

	public static void ClearEvents() {
		OnUsePressed = null;
	}

	public void Show() {
		ExplorerWindow.RealisedWindow.PlaceAtCenter();
		ExplorerWindow.RealisedWindow.Show();
	}

	static void Cancel() {
		// do nothing back and just close
		ExplorerWindow.RealisedWindow.Hide();
	}

	static TMP_InputField RenameField;
	static void RequestRename() {
		var dialog = PDialog.GenerateDialog(
			new(
				"Rename this file",
				new PDialog.Option[] {
					new("Cancel", null),
					new("Confirm", TryRename)
				},
				new(200, 100),
				WindowItem.NewInputField(
					new PComponents.InputField(null, "New name for this file..."),
					WindowItem.LayoutConfig.LayoutElementDynamic()
				)
			)
		);
		// get renamefield
		dialog.Items[0].SubItems[1].OnRealized(
			(rt, _) => RenameField = rt.GetComponent<TMP_InputField>());

		WindowRealiser.Instance.UpdateWindow(dialog); // force call to onrealised

		// set field for renaming
		string curname = CurrentEntries[CurrentlySelected].Name;
		RenameField.text = curname;

		// have to use the monobehaviour
		// needed cuz text hasnt updated yet
		ExplorerWindow.RealisedWindow.StartCoroutine(DelaySelect());
	}

	static IEnumerator DelaySelect() {
		yield return null;

		string curname = CurrentEntries[CurrentlySelected].Name;

		int doti = curname.LastIndexOf('.');
		if (doti == -1) doti = curname.Length - 1;

		int start = 0;

		RenameField.ActivateInputField();
		RenameField.caretPosition = start;
		RenameField.selectionAnchorPosition = start;
		RenameField.selectionFocusPosition = doti;
		RenameField.ForceLabelUpdate();
	}
	
	static void TryRename() {
		string name = RenameField.text;

		if (CurrentEntries.Any(e => e.Name == name))
			OverwriteConfirmation(() => Rename(name));
		else
			Rename(name);
	}
	static void Rename(string newName) {
		string src = Path.Join(CurrentDirectory, CurrentEntries[CurrentlySelected].Name);
		string dst = Path.Combine(CurrentDirectory, newName);

		File.Move(src, dst);

		Refresh();
	}

	static void RequestDelete() {
		if (CurrentlySelected == -1) return;

		// show confirmation
		PDialog.GenerateDialog(
			new(
				"Are you sure you want to delete this file?",
				new PDialog.Option[] {
					new("No", null),
					new("Yes", Delete),
				},
				new(400, 100),
				WindowItem.NewText(
					new PComponents.Text(
						CurrentEntries[CurrentlySelected].Name,
						alignment:TextAlignmentOptions.Center),
					WindowItem.LayoutConfig.LayoutElementDynamic()
				)
			)
		);
	}
	static void Delete() {
		string path = Path.Join(CurrentDirectory, CurrentEntries[CurrentlySelected].Name);

		File.Delete(path);

		Refresh();
	}

	static void Use() {

	}

	static void OverwriteConfirmation(Action onConfirm) {
		PDialog.GenerateDialog(new(
			"Another file with this name already exists here.\nDo you want to replace it?",
			new PDialog.Option[] {
				new("No", ()=>{}),
				new("Yes", onConfirm),
			},
			new(500, 150)
		));
	}

	static void Select(int i) {
		OptionSelectionUIHelper.SetColors(ItemsLayout.SubItems.ToArray(), i);

		CurrentlySelected = i;
	}
	
	static void ClearField() { InputField.text = ""; }

	// temporarily static stuff for now for testing 
	public static CWindow ExplorerWindow;
	static void SetEW() {
		ExplorerWindow = new() {
			Name = "Explorer",
			Config = new() {
				Resizable = true,
				Movable = true,
				Size = CWindow.Configuration.FreeSizeMinimum(
					new(500, 500),
					new(0, FooterItemsHeights)),
				HideOnStart = false
			},
			Items = new[] {
				WindowItem.NewScrollView(
					new PComponents.ScrollView(horizontalScrolling: false),
					WindowItem.LayoutConfig.DynamicLayout(
						(FooterItemsHeights * 2 + Config.UI.Visual.DefaultLayoutSpacing) 
						* FourSides.DownConst),
					new() {
						WindowItem.NewLayout(
							PComponents.Layout.Vertical.Fixed(false, true),
							WindowItem.LayoutConfig.FillLayout,
							new()
						).OnRealized((_, wi) => ItemsLayout = wi)
					}
				),
				/*WindowItem.NewInputField(
					new PComponents.InputField(
						v => CurrentFieldContents = v,
						"Enter name..."
					),
					WindowItem.LayoutConfig.Custom(
						position: new(0, 0, 1, 0),
						sizeDelta: new(0, FooterItemsHeights),
						fixedPosition: UIPosition.AnchoredOffset(
							UIPosition.BottomCenter,
							new(0,FooterItemsHeights + Config.UI.Visual.DefaultLayoutSpacing)
						)
					)
				).OnRealized((_, wi) => 
					InputField = (TMP_InputField)(wi.GetComponent<PComponents.InputField>()
					.RealComponent)),*/
				WindowItem.NewLayout(
					"Buttons",
					PComponents.Layout.Horizontal.Fixed(true, true),
					WindowItem.LayoutConfig.Custom(
						position: new(0, 0, 1, 0),
						sizeDelta: new(0, FooterItemsHeights),
						fixedPosition: new() {
							Pivot = UIPosition.BottomCenter
							}
					),
					new() {
						WindowItem.NewButtonCustomText(
							"Cancel",
							new PComponents.Button(() => Cancel()),
							new("Cancel", alignment: TextAlignmentOptions.Center),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						),
						WindowItem.NewButtonCustomText(
							"Rename",
							new PComponents.Button(() => RequestRename()),
							new("Rename", alignment: TextAlignmentOptions.Center),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						),
						WindowItem.NewButtonCustomText(
							"Delete",
							new PComponents.Button(() => RequestDelete()),
							new("Delete", alignment: TextAlignmentOptions.Center),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						),
						WindowItem.NewButtonCustomText(
							"Use", // naming will be changed later to more specific
							new PComponents.Button(() => Use()),
							new(UseButtonLabel, alignment: TextAlignmentOptions.Center), 
							WindowItem.LayoutConfig.LayoutElementDynamic()
						)
					}
				)
			}
		};
	}

	public struct EntryData {
		public string Name;
		public float NameWidth;
		public (string label, float width)[] Metadata;

		public EntryData(string name, float namewidth, params (string label, float width)[] metadata) {
			Name = name;
			NameWidth = namewidth;
			Metadata = metadata;
		}
	}
	static WindowItem FileEntry(int id, EntryData entry) =>
		WindowItem.NewButton(
			"File Entry",
			new PComponents.Button(() => Select(id)),
			WindowItem.LayoutConfig.LayoutElement(new(0, ItemHeight))
		).SetSubItems(
			WindowItem.NewLayout(
				PComponents.Layout.Horizontal.Fixed(true, true),
				WindowItem.LayoutConfig.FillLayout,
				new WindowItem[] {
					WindowItem.NewEmpty(
						WindowItem.LayoutConfig.LayoutElementDynamic(),
						new() {
							WindowItem.NewText(
							new PComponents.Text(entry.Name),
							WindowItem.LayoutConfig.DynamicLayout(
								FourSides.LeftConst * (ItemHeight + IconNameSpacing) // room for icon
							)
						),
						WindowItem.NewImage(
							"Icon",
							new PComponents.Image(IconPath),
							WindowItem.LayoutConfig.FixedLayout(
								UIPosition.AnchoredOffset(
									UIPosition.MiddleLeft,
									new(Config.UI.Menu.ItemPadding, 0)
								),
								Vector2.one * (ItemHeight - 2 * Config.UI.Menu.ItemPadding)
							)
						)
						}
					).AddComponents(new PComponents.LayoutElement(entry.NameWidth))
					.Wrap()
				}.Concat(entry.Metadata.Select(md =>
					WindowItem.NewText(
						new PComponents.Text(md.label),
						WindowItem.LayoutConfig.LayoutElementDynamic()
					).AddComponents(new PComponents.LayoutElement(md.width))
					.Wrap()
				)).ToList()
			)
		);

	public static void SetEntries(params EntryData[] entries) {
		ItemsLayout.SetSubItems(entries.Select((e, i) => FileEntry(i, e)).ToArray());
		 
		CurrentEntries = entries;

		WindowRealiser.Instance.UpdateWindow(ExplorerWindow);
	}

	/// <summary>
	/// Loads all files from a directory into this FE with optional metadata and ext filtering
	/// </summary>
	/// <param name="path">The directory path</param>
	/// <param name="extensions">Extensions to show (include .), null to show all</param>
	/// <param name="metadataGetter">Functions to take filename and return metadata as string, provide width</param>
	public static void LoadDirectory(
		string path,
		string[] extensions,
		Func<string, (string data, float width)[]> metadataGetter) {

		CurrentDirectory = path;
		LastLoadExtensions = extensions;
		LastLoadMetadataGetter = metadataGetter;

		extensions = extensions?.Select(e => e.ToLowerInvariant()).ToArray();

		string[] fps = 
			Directory.GetFiles(path)
			.Where(f => {

				// ignore hidden & system files
				var attr = File.GetAttributes(f);
				if ((attr & (FileAttributes.Hidden | FileAttributes.System)) != 0)
					return false;

				if (extensions == null || extensions.Length == 0)
					return true;

				// only return correct extension
				var ext = Path.GetExtension(f).ToLowerInvariant();
				return extensions.Contains(ext);
			})
			.ToArray();

		EntryData[] entries = new EntryData[fps.Length];

		for (int i = 0; i < fps.Length; i++) {
			string fp = fps[i];
			string name = Path.GetFileName(fp);

			var metadata =
				metadataGetter?.Invoke(fp) ??
				Array.Empty<(string, float)>();

			entries[i] = new(
				name,
				NameWidth,
				metadata
			);
		}

		SetEntries(entries);
	}

	public static void Refresh() {
		LoadDirectory(
			CurrentDirectory,
			LastLoadExtensions,
			LastLoadMetadataGetter
		);
	}

	public static void Set() {
		SetEW();

		
	}
	public static CWindow[] Windows => new[] {
		ExplorerWindow
	};
	
}