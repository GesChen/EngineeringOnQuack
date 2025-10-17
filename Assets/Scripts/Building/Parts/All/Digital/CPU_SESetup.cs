using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// this codebase gets worse by the minute
// its called CPU_ cuz its for the cpu ortherwise SE should be in uiassembly
public static class CPU_SESetup {
	public static void Setup() {

		CPU_UI.GetCurrentScript = () => {
			Part_CPU cpu = SelectionManager.Instance.PartSelection[0].GetComponent<Part_CPU>();
			var script = cpu.Script;

			if (script == null) {
				Tokenizer tokenizer = new();

				// should always tokenize properly??
				(Script newScript, _) = tokenizer.Tokenize(
					"setup():\n\t\n\treturn 0\n\ntick():\n\t\n\treturn 0");

				newScript.Name = "New Script";

				script = newScript;
				cpu.Script = script;
			}

			Part_CPU.currentlyEditingScript = script;

			return (
				script.OriginalText.Split('\n'),
				script.Name
			);
		};

		// probably will be changed cuz this is kinda spaghetti
		SEProcedural.OnFileNameChanged =
			name => Part_CPU.currentlyEditingScript.Name = name;

		SEProcedural.OnSetup = () => {
			SEProcedural.ScriptEditor.OnScriptUpdated = null;
			SEProcedural.ScriptEditor.OnScriptUpdated += content => {
				Part_CPU.currentlyEditingScript.OriginalText = string.Join('\n', content);
			};
		};

		SEProcedural.OnNewPressed = () => {
			// auto save unless current file is not already saved
			if (!Part_CPU.currentlyEditingScript.Saved) {
				UnsavedNotification(CreateNewFile);
				return;
			}
			CreateNewFile();
		};

		SEProcedural.OnSaveAsPressed = RequestSave;
		SEProcedural.OnSavePressed = TrySaveNotAs;

		SEProcedural.OnLoadPressed = RequestLoad;
	}

	static void UnsavedNotification(Action intendedAction) {
		PDialog.GenerateDialog(new(
			"This script hasn't been saved!\nWould you like to save it?",
			new PDialog.Option[] {
				new("Save", () => RequestSave()),
				new("Don't Save", intendedAction),
				new("Cancel", null)
			},
			new(350, 150)
		));
	}

	// the regular save option not the save as so it should serve dual purpose
	static void TrySaveNotAs() {
		if (Part_CPU.currentlyEditingScript.Saved) {
			try {
				string path = Part_CPU.currentlyEditingScript.SaveLocation;

				byte[] data = ScriptSaveLoad.ConvertScriptToBytes(Part_CPU.currentlyEditingScript);

				File.WriteAllBytes(path, data);
			} catch (Exception e) {
				PDialog.GenerateDialog(new(
					$"An error occurred while saving the file:\n{e}",
					new PDialog.Option[] {
					new("OK", null)
					},
					new(300, 200)
				));
			}
		} else {
			RequestSave();
		}
	}

	static void RequestSave() {
		FileExplorer.CreateNewFE(
			HF.GuaranteePath(
				Config.Path.LocalPath("Scripts").ToString()
			),
			new(
				FileExplorer.Type.SaveFile,
				new string[] { ".qk" },
				path => {
					FileInfo info = new (path);
					long sizeBytes = info.Length;
					return new[] {
						($"{sizeBytes} bytes", 2f)
					};
				},
				"Save",
				TrySave,
				5,
				"New Script.qk",
				10
			)
		);
	}
	static void TrySave(string filePath) {
		try {
			// may change to allow string saving later and nongzipped perhaps
			byte[] data = ScriptSaveLoad.ConvertScriptToBytes(Part_CPU.currentlyEditingScript);
			
			File.WriteAllBytes(filePath, data);
			Part_CPU.currentlyEditingScript.Saved = true;
			Part_CPU.currentlyEditingScript.SaveLocation = filePath;

			SEProcedural.SetFileName(Path.GetFileNameWithoutExtension(filePath));
			PDialog.GenerateDialog(new(
				"File saved successfully!",
				new PDialog.Option[] {
					new("OK", null)
				},
				new(250, 150)
			));
		} catch (Exception e) {
			PDialog.GenerateDialog(new(
				$"An error occurred while saving the file:\n{e}",
				new PDialog.Option[] {
					new("OK", null)
				},
				new(300, 200)
			));
		}
	}

	static void RequestLoad() {
		FileExplorer.CreateNewFE(
			HF.GuaranteePath(
				Config.Path.LocalPath("Scripts").ToString()
			),
			new(
				FileExplorer.Type.OpenFile,
				new string[] { ".qk" },
				path => {
					FileInfo info = new (path);
					long sizeBytes = info.Length;
					return new[] {
						($"{sizeBytes} bytes", 2f)
					};
				},
				"Load",
				TryLoad,
				5,
				".qk",
				0
			)
		);
	}

	static void TryLoad(string filePath) {
		try {
			byte[] bytes = File.ReadAllBytes(filePath);

			Script script = ScriptSaveLoad.ConvertBytesToScript(bytes);

			Part_CPU.currentlyEditingScript = script;

			SEProcedural.SetFileName(script.Name);
			SEProcedural.ScriptEditor.Load(script.OriginalText.Split('\n'));

		} catch(Exception e) {
			PDialog.GenerateDialog(new(
				$"An error occurred while loading the file:\n{e}",
				new PDialog.Option[] {
					new("OK", null)
				},
				new(300, 200)
			));
		}
	}

	static void CreateNewFile() {
		Tokenizer tokenizer = new();
		// should always tokenize properly??
		(Script newScript, _) = tokenizer.Tokenize(
			"setup():\n\t\n\treturn 0\n\ntick():\n\t\n\treturn 0");

		newScript.Name = "New Script";

		Part_CPU.currentlyEditingScript = newScript;

		SEProcedural.SetFileName(newScript.Name);
		SEProcedural.ScriptEditor.Load(newScript.OriginalText.Split('\n'));
	}
}