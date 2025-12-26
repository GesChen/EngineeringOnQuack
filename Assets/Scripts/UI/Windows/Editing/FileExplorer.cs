using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FileExplorer {
	public static class MetadataGetters {
		public static (string, float)[] GetBytes(string path) {
			FileInfo info = new (path);
			long sizeBytes = info.Length;
			return new[] {
				($"{sizeBytes} bytes", 2f)
			};
		}
	}

	public static void CreateNewFE(
		string initialDirectory,
		FileExplorer fe
		) {

		fe.SetEW();
		WindowManager.Instance.RealiseWindows(fe.ExplorerWindow);
		fe.LoadDirectory(initialDirectory, false);
		fe.Show();
	}

	/// <summary>
	/// Primary constructor. A directory must be manually loaded after creation.
	/// </summary>
	/// <param name="extensions">Include the . Null for all</param>
	/// <param name="onUsePressed">Returns the chosen file's full path</param>
	/// <example>
	/// <code>
	/// // example metadata getter for bytes
	/// path => {
	/// 	FileInfo info = new FileInfo(path);
	/// 	long sizeBytes = info.Length;
	/// 	return new[] {
	/// 		($"{sizeBytes} bytes", .5f)
	/// 	};
	/// }
	/// </code>
	/// </example>
	/// </param>
	public FileExplorer(
		Type type,
		string[] extensions,
		Func<string, (string data, float width)[]> metadataGetter,
		string useButtonLabel,
		Action<string> onUsePressed,
		float nameWidth,
		string initialFileName = null,
		int initialNameSelectionEndExc = -1
		) {

		ExplorerType = type;
		Extensions = extensions?.Select(e => e.ToLowerInvariant()).ToArray();
		MetadataGetter = metadataGetter;
		UseButtonLabel = useButtonLabel;
		OnUsePressed = onUsePressed;
		NameWidth = nameWidth;

		InitialFileName = (initialFileName, initialNameSelectionEndExc);
	}

	#region Config
	public string UseButtonLabel = "Load"; // to be changed per instance
	public event Action<string> OnUsePressed; // returns the file full path
	public float NameWidth = 5;
	public string[] Extensions; // include the . null for all
	public Func<string, (string data, float width)[]> MetadataGetter;
	public enum Type { OpenFile, SaveFile, SelectFolder }
	// intellisense added select folder but maybe probably wont use
	public Type ExplorerType;
	public (string name, int selectionEndExc) InitialFileName; // for save file dialog
	#endregion

	#region State
	TMP_InputField AddressBar;
	WindowItem ItemsLayout;
	Button NewFolderButton;
	string CurrentDirectory;
	Entry[] CurrentEntries;
	TMP_InputField NameField;

	float LastSelectTime;
	int LastSelectedI = -1;
	int CurrentlySelectedI = -1;
	string NewFolderFieldContents;
	Entry CurrentlySelected => CurrentEntries[CurrentlySelectedI];

	// 0 is most recent
	List<string> DirectoryHistory = new();
	int HistoryPosition;
	#endregion

	public void Show() {
		ExplorerWindow.RealisedWindow.PlaceAtCenter();
		ExplorerWindow.RealisedWindow.Show();
	}

	public void Close() {
		WindowManager.Instance.DestroyWindow(ExplorerWindow);
	}

	void Select(int i) {
		// double click check
		if (Time.time - LastSelectTime < Config.Input.extraClickMaxTimeMs / 1000f) {
			if (CurrentlySelected.Type == Entry.EntryType.File 
				&& ExplorerType == Type.OpenFile
				&& i == LastSelectedI)
				UseButton();
			else
				LoadDirectory(Path.Join(CurrentDirectory, CurrentlySelected.Name));

			CurrentlySelectedI = -1;
			return;
		}

		LastSelectTime = Time.time;
		LastSelectedI = i;

		OptionSelectionUIHelper.SetColors(ItemsLayout.SubItems.ToArray(), i);

		CurrentlySelectedI = i;

		NameField.text = CurrentlySelected.Name;
	}

	#region Top Bar Options
	public void ResetHistory() {
		HistoryPosition = 0;
		DirectoryHistory.Clear();
	}

	// remember the current location in history, break it if needed
	void HistoryRemember() {
		DirectoryHistory.RemoveRange(0, HistoryPosition);

		DirectoryHistory.Insert(0, CurrentDirectory);

		if (DirectoryHistory.Count > Config.FileExplorer.MaxHistoryLength)
			DirectoryHistory.RemoveAt(DirectoryHistory.Count - 1);

		HistoryPosition = 0;
	}

	void Back() {
		if (HistoryPosition == DirectoryHistory.Count) return;

		LoadDirectory(
			DirectoryHistory[HistoryPosition++], // increment after
			false
		);
	}

	void Forward() {
		if (HistoryPosition == -1) return;

		HistoryPosition--;

		LoadDirectory(
			DirectoryHistory[HistoryPosition],
			false
		);
	}

	void Up() {
		var path = CurrentDirectory;
		string clean = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var parent = Directory.GetParent(clean);

		if (parent == null) return;

		LoadDirectory(
			parent.FullName);
	}

	void NewFolder() {
		NewFolderButton.interactable = false;

		PDialog.GenerateDialog(new(
			"New Folder Name",
			new PDialog.Option[] {
				new("Cancel", null),
				new("Confirm", TryNewFolder)
			},
			new(300, 150),
			WindowItem.NewInputField(
				new PComponents.InputField(
					n => NewFolderFieldContents = n,
					placeholderText: "Name..."
				),
				WindowItem.LayoutConfig.LayoutElementDynamic()
			)
		));
	}
	void TryNewFolder() {
		NewFolderButton.interactable = true;

		if (!IsValidFileName(NewFolderFieldContents, out var message)) {
			PDialog.GenerateDialog(FileNameError(message));
			return;
		}

		var path = Path.Join(CurrentDirectory, NewFolderFieldContents);
		if (Directory.Exists(path)) {
			PDialog.GenerateDialog(new(
				$"A folder named {NewFolderFieldContents} already exists here.",
				new PDialog.Option[] {
					new("Ok", null),
				},
				new(400, 200)
			));
			return;
		}

		// then do it
		Directory.CreateDirectory(path);

		Refresh();
	}

	void TryChangeDirectories(string newDir) {

		if (!IsDirectoryAccessible(newDir)) {
			ExplorerWindow.RealisedWindow.StartCoroutine(DelayBadAddress());
			return;
		}

		LoadDirectory(
			newDir);
	}
	bool IsDirectoryAccessible(string path) {
		try {
			if (Directory.Exists(path)) {
				Directory.EnumerateFileSystemEntries(path).GetEnumerator().MoveNext();
				return true;
			}
		} catch {
			return false;
		}
		return false;
	}
	IEnumerator DelayBadAddress() {
		yield return null;
		AddressBar.interactable = false; // disable until ok 

		PDialog.GenerateDialog(
			new(
"Invalid path. This path does not exist, or you do not have sufficient permissions to view it.",
				new PDialog.Option[] {
					new("Ok", UpdateAddressBar)
				},
				new(500, 100)
		));
	}
	void UpdateAddressBar() {
		AddressBar.text = CurrentDirectory;
		AddressBar.interactable = true;
	}

	#endregion

	#region Bottom Bar Options
	
	void Cancel() {
		// do nothing back and just close
		ExplorerWindow.RealisedWindow.Hide();
	}

	TMP_InputField RenameField;
	void RequestRename() {
		var dialog = PDialog.GenerateDialog(
			new(
				"Rename this file",
				new PDialog.Option[] {
					new("Cancel", null),
					new("Confirm", TryRename)
				},
				new(200, 125),
				WindowItem.NewInputField(
					new PComponents.InputField(placeholderText: "New name for this file..."),
					WindowItem.LayoutConfig.LayoutElementDynamic()
				)
			)
		);
		// get renamefield
		dialog.Items[0].SubItems[1].SubItems[0].OnRealized(
			(rt, _) => RenameField = rt.GetComponent<TMP_InputField>());

		WindowRealiser.Instance.UpdateWindow(dialog); // force call to onrealised

		// set field for renaming
		string curname = CurrentlySelected.Name;
		RenameField.text = curname;

		// have to use the monobehaviour
		// needed cuz text hasnt updated yet
		ExplorerWindow.RealisedWindow.StartCoroutine(DelaySelect());
	}

	IEnumerator DelaySelect() {
		yield return null;

		string curname = CurrentlySelected.Name;

		int doti = curname.LastIndexOf('.');
		if (doti == -1) doti = curname.Length;

		int start = 0;

		RenameField.ActivateInputField();
		RenameField.caretPosition = start;
		RenameField.selectionAnchorPosition = start;
		RenameField.selectionFocusPosition = doti;
		RenameField.ForceLabelUpdate();
	}
	
	void TryRename() {
		string name = RenameField.text;

		if (!IsValidFileName(name, out var message)) {
			PDialog.GenerateDialog(FileNameError(message));
			return;
		}

		if (CurrentEntries.Any(e => e.Name == name))
			OverwriteConfirmation(() => Rename(name));
		else
			Rename(name);
	}
	bool IsValidFileName(string name, out string message) {
		if (string.IsNullOrWhiteSpace(name)) {
			message = "Name is blank or whitespace.";
			return false;
		}

		if (name.EndsWith(" ") || name.EndsWith(".")) {
			message = "Name cannot end with a space or period.";
			return false;
		}

		char[] invalidChars = Path.GetInvalidFileNameChars();
		int idx = name.IndexOfAny(invalidChars);
		if (idx != -1) {
			message = $"Invalid character '{name[idx]}' in name.";
			return false;
		}

		string upper = name.ToUpperInvariant();
		if (upper is "CON" or "PRN" or "AUX" or "NUL") {
			message = "Name is a reserved device identifier.";
			return false;
		}

		if (upper.StartsWith("COM") || upper.StartsWith("LPT")) {
			if (upper.Length == 4 && char.IsDigit(upper[3])) {
				message = "Name matches a reserved device pattern.";
				return false;
			}
		}

		message = null;
		return true;
	}

	PDialog FileNameError(string message) => 
		new(
			$"Invalid file name:\n{message}",
			new PDialog.Option[] { new("Ok", null) },
			new(400, 150)
		);

	void Rename(string newName) {
		string src = Path.Join(CurrentDirectory, CurrentlySelected.Name);
		string dst = Path.Combine(CurrentDirectory, newName);

		if (CurrentlySelected.Type == Entry.EntryType.File)
			File.Move(src, dst);
		else
			Directory.Move(src, dst);

		Refresh();
	}

	void RequestDelete() {
		if (CurrentlySelectedI == -1) return;

		string type =
			CurrentlySelected.Type == Entry.EntryType.File
			? "File"
			: "Folder";

		// show confirmation
		PDialog.GenerateDialog(
			new(
				$"Are you sure you want to delete this {type}?",
				new PDialog.Option[] {
					new("No", null),
					new("Yes", Delete),
				},
				new(500, 150),
				WindowItem.NewText(
					new PComponents.Text(
						CurrentlySelected.Name,
						TextAlignmentOptions.Center),
					WindowItem.LayoutConfig.LayoutElementDynamic()
				)
			)
		);
	}
	void Delete() {
		string path = Path.Join(CurrentDirectory, CurrentlySelected.Name);

		try {
			if (CurrentlySelected.Type == Entry.EntryType.File)
				File.Delete(path);
			else
				Directory.Delete(path, recursive: true); // safely delete non-empty directories
		} catch (Exception ex) {
			PDialog.GenerateDialog(new(
				$"Failed to delete: {ex.Message}",
				new PDialog.Option[] { new("Ok", null) },
				new(400, 200)
			));
		}

		Refresh();
	}

	void UseButton() {
		if (CurrentlySelectedI == -1)
			Use(NameField.text);
		else
			Use(CurrentlySelected.Name);
	}

	void Use(string name) {
		var path = Path.Combine(CurrentDirectory, name);

		if (ExplorerType == Type.OpenFile) {
			if (!File.Exists(path)) {
				PDialog.GenerateDialog(new(
					$"File not found at path: {path}",
					new PDialog.Option[] { new("Ok", null) },
					new(500, 300)
				));

				// dont close if fail
				return;
			}
		} else
		if (ExplorerType == Type.SaveFile) {
			if (File.Exists(path)) {
				OverwriteConfirmation(() => InvokeUse(path));
				return;
			}
			if (!IsValidFileName(name, out var message)) {
				PDialog.GenerateDialog(FileNameError(message));
				return;
			}
		}

		InvokeUse(path);
	}

	void InvokeUse(string path) {
		OnUsePressed?.Invoke(path);
		Close();
	}

	void OverwriteConfirmation(Action onConfirm) {
		PDialog.GenerateDialog(new(
			"Another file with this name already exists here.\nDo you want to replace it?",
			new PDialog.Option[] {
				new("No", ()=>{}),
				new("Yes", onConfirm),
			},
			new(500, 150)
		));
	}
	#endregion

	public CWindow ExplorerWindow;
	void SetEW() {
		ExplorerWindow = new() {
			Name = "Explorer",
			Config = new() {
				Resizable = true,
				Movable = true,
				Size = CWindow.Configuration.FreeSizeMinimum(
					new(500, 500),
					new(200, 100)),
				HideOnStart = true
			},
			CustomEvents = new() {
				new(
					TimedEventInvoker.Timing.Close,
					_ => Close()
				)
			},
			Items = new[] {
				WindowItem.NewEmpty(
					"Navagation",
					WindowItem.LayoutConfig.Custom(
						position: new(1, 0, 0, 0),
						sizeDelta: new(0, Config.FileExplorer.FooterItemsHeights),
						fixedPosition: new() {
							Pivot = UIPosition.TopCenter
						}
					),
					new() {
	WindowItem.NewButtonCustomImageOverlay( // back
		"Back",
		new PComponents.Button(Back),
		new PComponents.Image(
			 Config.FileExplorer.BackIcon
		),
		WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredOffset(
				UIPosition.MiddleLeft, 
				Vector2.right * 0
			),
			Config.FileExplorer.NavgationHeight * Vector2.one
		)
	),
	WindowItem.NewButtonCustomImageOverlay( // forward
		"Forward",
		new PComponents.Button(Forward),
		new PComponents.Image(
			 Config.FileExplorer.ForwardIcon
		),
		WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredOffset(
				UIPosition.MiddleLeft, 
				Vector2.right * Config.FileExplorer.NavgationHeight
			),
			Config.FileExplorer.NavgationHeight * Vector2.one
		)
	),
	WindowItem.NewButtonCustomImageOverlay( // up
		"Up",
		new PComponents.Button(Up),
		new PComponents.Image(
			 Config.FileExplorer.UpIcon
		),
		WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredOffset(
				UIPosition.MiddleLeft, 
				Vector2.right * Config.FileExplorer.NavgationHeight * 2
			),
			Config.FileExplorer.NavgationHeight * Vector2.one
		)
	),
	WindowItem.NewButtonCustomImageOverlay( // refresh
		"Refresh",
		new PComponents.Button(Refresh),
		new PComponents.Image(
			 Config.FileExplorer.RefreshIcon
		),
		WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredOffset(
				UIPosition.MiddleLeft, 
				Vector2.right * (Config.FileExplorer.NavgationHeight * 3 + Config.UI.Visual.DefaultLayoutSpacing)
			),
			Config.FileExplorer.NavgationHeight * Vector2.one
		)
	),
	WindowItem.NewButtonCustomImageOverlay( // new folder
		"New Folder",
		new PComponents.Button(NewFolder)
			.OnRealised<PComponents.Button, Button>(c => NewFolderButton = c),
		new PComponents.Image(
			 Config.FileExplorer.NewFolderIcon
		),
		WindowItem.LayoutConfig.FixedLayout(
			UIPosition.AnchoredOffset(
				UIPosition.MiddleLeft, 
				Vector2.right * (Config.FileExplorer.NavgationHeight * 4 + Config.UI.Visual.DefaultLayoutSpacing * 2)
			),
			Config.FileExplorer.NavgationHeight * Vector2.one
		)
	),
	WindowItem.NewInputField( // address bar
		"Address Bar",
		new PComponents.InputField(
			onEndEdit: TryChangeDirectories,
			placeholderText: "",
			alignment: TextAlignmentOptions.Right
			)
		.OnRealised<PComponents.InputField, TMP_InputField>(c => AddressBar = c),
		WindowItem.LayoutConfig.DynamicLayout(
			FourSides.LeftConst * (Config.FileExplorer.NavgationHeight * 5 + Config.UI.Visual.DefaultLayoutSpacing * 3)
		)
	)
					}
				),
				WindowItem.NewScrollView(
					new PComponents.ScrollView(horizontalScrolling: false),
					WindowItem.LayoutConfig.DynamicLayout(
						margin: new(
							Config.FileExplorer.NavgationHeight + Config.UI.Visual.DefaultLayoutSpacing, 0,
							Config.FileExplorer.FooterItemsHeights * 2 + Config.UI.Visual.DefaultLayoutSpacing * 2, 0
						)
					),
					new() {
						WindowItem.NewLayout(
							PComponents.Layout.Vertical.Fixed(false, true),
							WindowItem.LayoutConfig.FillLayout,
							new()
						).OnRealized((_, wi) => ItemsLayout = wi)
					}
				),
				WindowItem.NewInputField(
					new PComponents.InputField(
						initialText: InitialFileName.name,
						initialSelection: 
							InitialFileName.selectionEndExc == -1
							? null 
							: (0, InitialFileName.selectionEndExc),
						onValueChanged: null,
						//onEndEdit: n => ManuallyUse(n), // why was this added?? someday ill figure out why
						placeholderText: ""
					),
					WindowItem.LayoutConfig.Custom(
						position: new(0, 0, 1, 0),
						sizeDelta: new(0, Config.FileExplorer.FooterItemsHeights),
						fixedPosition: UIPosition.AnchoredOffset(
							UIPosition.BottomCenter,
							new(0, Config.FileExplorer.FooterItemsHeights + Config.UI.Visual.DefaultLayoutSpacing)
						)
					)
				).OnRealized((_, wi) =>
					NameField = (TMP_InputField)(wi.GetComponent<PComponents.InputField>()
					.RealComponent)),
				WindowItem.NewLayout(
					"Buttons",
					PComponents.Layout.Horizontal.Fixed(true, true),
					WindowItem.LayoutConfig.Custom(
						position: new(0, 0, 1, 0),
						sizeDelta: new(0, Config.FileExplorer.FooterItemsHeights),
						fixedPosition: new() {
							Pivot = UIPosition.BottomCenter
						}
					),
					new() {
						WindowItem.NewButtonCustomText(
							"Cancel",
							new PComponents.Button(() => Cancel()),
							new("Cancel", TextAlignmentOptions.Center),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						),
						WindowItem.NewButtonCustomText(
							"Rename",
							new PComponents.Button(() => RequestRename()),
							new("Rename", TextAlignmentOptions.Center),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						),
						WindowItem.NewButtonCustomText(
							"Delete",
							new PComponents.Button(() => RequestDelete()),
							new("Delete", TextAlignmentOptions.Center),
							WindowItem.LayoutConfig.LayoutElementDynamic()
						),
						WindowItem.NewButtonCustomText(
							"Use", // naming will be changed later to more specific
							new PComponents.Button(() => UseButton()),
							new(UseButtonLabel, TextAlignmentOptions.Center), 
							WindowItem.LayoutConfig.LayoutElementDynamic()
						)
					}
				)
			}
		};
	}

	public struct Entry {
		public enum EntryType { File, Folder };
		public EntryType Type;
		public string Name;
		public float NameWidth;
		public (string label, float width)[] Metadata;

		public Entry(
			EntryType type,
			string name,
			float namewidth,
			params (string label, float width)[] metadata) {

			Type = type;
			Name = name;
			NameWidth = namewidth;
			Metadata = metadata;
		}
	}
	WindowItem FileEntry(int id, Entry entry) {
		PComponents.Image icon;
		if (entry.Type == Entry.EntryType.File) {
			string ext = Path.GetExtension(entry.Name).ToLowerInvariant();
			icon = new(Config.FileExplorer.GetFileIcon(ext));
		} else {
			icon = new(Config.FileExplorer.FolderEntryIcon);
		}

		return WindowItem.NewButton(
			"File Entry",
			new PComponents.Button(() => Select(id)),
			WindowItem.LayoutConfig.LayoutElement(new(0, Config.FileExplorer.ItemHeight))
		).SetSubItems(
			WindowItem.NewLayout( // primary layout
				PComponents.Layout.Horizontal.Fixed(true, true),
				WindowItem.LayoutConfig.FillLayout,
				new WindowItem[] {

					WindowItem.NewEmpty( // 
						WindowItem.LayoutConfig.LayoutElementDynamic(),
						new() {
							WindowItem.NewText(
							new PComponents.Text(entry.Name, TextAlignmentOptions.Left),
							WindowItem.LayoutConfig.DynamicLayout(
								FourSides.LeftConst * 
								(Config.FileExplorer.ItemHeight + Config.FileExplorer.IconNameSpacing) // room for icon
							)
						),
						WindowItem.NewImage(
							"Icon",
							icon,
							WindowItem.LayoutConfig.FixedLayout(
								UIPosition.AnchoredOffset(
									UIPosition.MiddleLeft,
									new(Config.UI.Menu.ItemPadding, 0)
								),
								Vector2.one * (Config.FileExplorer.ItemHeight - 2 * Config.UI.Menu.ItemPadding)
							)
						)
						}
					).AddComponents(new PComponents.LayoutElement(entry.NameWidth))
					.Wrap() // preserve layout ignore items owns layout wants 

				}.Concat(entry.Metadata.Select(md =>
					WindowItem.NewText(
						new PComponents.Text(md.label, TextAlignmentOptions.Left),
						WindowItem.LayoutConfig.LayoutElementDynamic()
					).AddComponents(new PComponents.LayoutElement(md.width))
					.Wrap()
				)).ToList()
			)
		);
	}

	void SetEntries(params Entry[] entries) {
		//if (entries.Length == 0) return;

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
	public void LoadDirectory(
		string path,
		bool rememberHistory = true) {

		if (rememberHistory)
			HistoryRemember();

		CurrentDirectory = path;

		// get files
		string[] fps = GetFilesSafe(path, Extensions);
		// get folders
		string[] dps = GetDirectoriesSafe(path);

		var entries =
			dps.Select(dp => {
				string name = Path.GetFileName(dp.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

				return new Entry(
					Entry.EntryType.Folder,
					name,
					NameWidth,
					Array.Empty<(string, float)>()
				);
			}).Concat(
			fps.Select(fp => {
				string name = Path.GetFileName(fp);

				var metadata =
				MetadataGetter?.Invoke(fp) ??
				Array.Empty<(string, float)>();

				return new Entry(
					Entry.EntryType.File,
					name,
					NameWidth,
					metadata
				);
			}));

		SetEntries(entries.ToArray());

		UpdateAddressBar();
	}

	string[] GetFilesSafe(string path, string[] extensions = null) {
		try {
			if (!Directory.Exists(path)) return Array.Empty<string>();

			return Directory.GetFiles(path)
				.Where(f => {
					try {
						var attr = File.GetAttributes(f);
						if ((attr & (FileAttributes.Hidden | FileAttributes.System)) != 0)
							return false;

						if (extensions == null || extensions.Length == 0)
							return true;

						var ext = Path.GetExtension(f).ToLowerInvariant();
						return extensions.Contains(ext);
					} catch (UnauthorizedAccessException) {
						return false;
					}
				})
				.ToArray();
		} catch (UnauthorizedAccessException) {
			return Array.Empty<string>();
		}
	}

	string[] GetDirectoriesSafe(string path) {
		try {
			if (!Directory.Exists(path)) return Array.Empty<string>();

			return Directory.GetDirectories(path)
				.Where(d => {
					try {
						var attr = File.GetAttributes(d);
						if ((attr & (FileAttributes.Hidden | FileAttributes.System)) != 0)
							return false;

						return true;
					} catch (UnauthorizedAccessException) {
						return false;
					}
				})
				.ToArray();
		} catch (UnauthorizedAccessException) {
			return Array.Empty<string>();
		}
	}

	public void Refresh() {
		LoadDirectory(CurrentDirectory);
	}
}