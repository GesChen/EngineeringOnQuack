using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class Part_CPU : NonStaticPart {
	public override string PartName => "CPU";

	public Script Script;
	public string DEBUG_CurrentScriptText; // for debugging purposes
	public double CreationTime;

	void Update() {
		if (Script != null)
			DEBUG_CurrentScriptText = Script.OriginalText;
		else
			DEBUG_CurrentScriptText = "null";
	}

	bool running = false; // not sure if this is the best way of doing this
						  // might instead have override onsimulatingupdate or something like that instead
						  // but we'll see
						  // this shouldnt be hard to modify either

	int[] TransceiverPorts;

	Interpreter Interpreter;
	Memory Memory;
	Evaluator Evaluator;
	bool hasTick;
	Primitive.Function tickFunc;

	#region Language
	public static Type Type_CPU = new(
		"CPU",
		new Memory(
			new Dictionary<string, T_Data>() {
				{ "port", new Primitive.Function("port", PartInternalFunctions.CPU.port) }
			},
			new Dictionary<string, Type>(),
			"CPU Type Snapshot"
			)
		);

	readonly T_Data InternalDataObject = new(Type_CPU);

	public override T_Data GetInternalLanguageDataObject() => InternalDataObject;
	#endregion

	#region ui
	public static void SetupUI() {
		CPU_UI.GetCurrentScript = () => {
			// rcm extensions only does ss for now
			// this fixes the selection bcoming null 

			var cpuTransforms = GetSelectedCPUs();
			Part_CPU cpu =
				cpuTransforms.Count > 0
				? cpuTransforms[0].GetComponent<Part_CPU>()
				: null;
			var script = cpu != null ? cpu.Script : null;

			if (script == null) {
				Tokenizer tokenizer = new();

				// should always tokenize properly??
				(Script newScript, _) = tokenizer.Tokenize(
					"setup():\n\t\n\treturn 0\n\ntick():\n\t\n\treturn 0");

				newScript.Name = "New Script";

				script = newScript;
			}

			foreach (var ct in cpuTransforms) {
				var thisCPU = ct.GetComponent<Part_CPU>();
				thisCPU.Script ??= script;
			}

			return (
				script.OriginalText,
				script.Name
			);
		};

		// might be the worst thing ive ever written
		// set up all selected cpus with the new script editor
		/*CPU_UI.OnEdit = editor =>
			SelectionManager.Instance.PartSelection
			.Select(p => p.GetComponent<Part_CPU>()).Where(c => c != null)
			.ToList().ForEach(c => c.SetupScriptEditor(editor));*/
		CPU_UI.OnEdit = e =>
			GetSelectedCPUs().ForEach(t => t.GetComponent<Part_CPU>().SetupScriptEditor(e));
	}

	static List<Transform> GetSelectedCPUs() {
		List<Transform> cpus = new();
		if (RightClick.Instance.ContextAtClick is Contexts.Editing.SingleSelection ss)
			cpus = new() { ss.Selected };
		else if (RightClick.Instance.ContextAtClick is Contexts.Editing.MultiSelection ms)
			cpus = new(ms.Selected);

		return cpus.Where(t => t.GetComponent<Part_CPU>() != null).ToList();
	}

	ScriptEditorRewritten UsingEditor;
	public void SetupScriptEditor(ScriptEditorRewritten editor) {
		if (UsingEditor != null) {
			UsingEditor.Destroy();
		}

		UsingEditor = editor;

		// hope this is alright
		editor.OnContentsChanged += s => Script.OriginalText = s;
		editor.OnFileNameChanged += Rename;

		editor.OnNewPressed += TryNew;
		editor.OnSavePressed += TrySaveNotAs;
		editor.OnSaveAsPressed += RequestSave;
		editor.OnLoadPressed += RequestLoad;

		editor.OnEditorClosed += () => UsingEditor = null;
	}

	void Rename(string name) {
		Script.Name = name;

		UsingEditor.SetFileName(name);
	}

	void TryNew() {
		// auto save unless current file is not already saved
		if (!Script.SavedAsFile) {
			UnsavedNotification(CreateNewFile);
			return;
		}
		CreateNewFile();
	}

	void UnsavedNotification(Action intendedAction) {
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
	void TrySaveNotAs() {
		if (Script.SavedAsFile) {
			try {
				string path = Script.SaveLocation;

				byte[] data = ScriptSaveLoad.ConvertScriptToBytes(Script);

				File.WriteAllBytes(path, data);
			} catch (Exception e) {
				PDialog.GenerateDialog(new(
					$"An error occurred while saving the file:\n{e.Message}",
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

	void RequestSave() {
		FileExplorer.CreateNewFE(
			Config.SaveLoad.ScriptsConfig.SaveLocation,
			new(
				FileExplorer.Type.SaveFile,
				new string[] { Config.SaveLoad.ScriptsConfig.SaveExtension },
				FileExplorer.MetadataGetters.GetBytes,
				"Save",
				TrySave,
				5,
				"New Script" + Config.SaveLoad.ScriptsConfig.SaveExtension,
				10
			)
		);
	}

	void TrySave(string filePath) {
		try {
			// may change to allow string saving later and nongzipped perhaps
			byte[] data = ScriptSaveLoad.ConvertScriptToBytes(Script);

			File.WriteAllBytes(filePath, data);
			Script.SavedAsFile = true;
			Script.SaveLocation = filePath;

			string name = Path.GetFileNameWithoutExtension(filePath);

			UsingEditor.SetFileName(name);
		} catch (Exception e) {
			PDialog.GenerateDialog(new(
				$"An error occurred while saving the file:\n{e.Message}",
				new PDialog.Option[] {
					new("OK", null)
				},
				new(300, 200)
			));
		}
	}

	void RequestLoad() {
		FileExplorer.CreateNewFE(
			Config.SaveLoad.ScriptsConfig.SaveLocation,
			new(
				FileExplorer.Type.OpenFile,
				new string[] { Config.SaveLoad.ScriptsConfig.SaveExtension },
				FileExplorer.MetadataGetters.GetBytes,
				"Load",
				TryLoad,
				5,
				Config.SaveLoad.ScriptsConfig.SaveExtension,
				0
			)
		);
	}

	void TryLoad(string filePath) {
		try {
			byte[] bytes = File.ReadAllBytes(filePath);

			Script script = ScriptSaveLoad.ConvertBytesToScript(bytes);

			Script = script;

			UsingEditor.SetFileName(script.Name);
			UsingEditor.Load(script.OriginalText);

		} catch (Exception e) {
			PDialog.GenerateDialog(new(
				$"An error occurred while loading the file:\n{e.Message}",
				new PDialog.Option[] {
					new("OK", null)
				},
				new(300, 200)
			));
		}
	}

	void CreateNewFile() {
		Tokenizer tokenizer = new();
		// should always tokenize properly??
		(Script newScript, _) = tokenizer.Tokenize(
			"setup():\n\t\n\treturn 0\n\ntick():\n\t\n\treturn 0");

		Script = newScript;

		newScript.Name = "New Script";

		UsingEditor.SetFileName(newScript.Name);
		UsingEditor.Load(newScript.OriginalText);
	}
	#endregion

	IEnumerator DelayScriptSetup() {
		yield return null;

		CreationTime = Time.timeAsDouble;

		InternalFunctions.OnPrintCalled += TryPrint;
		InternalFunctions.OnRequestTime += TryGiveTime;
		PartInternalFunctions.CPU.OnPortCalled += GetPort;
		Memory.CPUGet += CPUGet;

		// find transceiever ports
		TransceiverPorts = 
			Ports
			.Select((port, i) => (i, port))
			.Where(
				ip => {
					if (ip.port.OtherPart == null) return false;
					ip.port.OtherPart.IsNonStaticPart(out var nsp);
					return nsp is Part_Transceiver;
				})
			.Select(ip => ip.i)
			.ToArray();

		if (Script == null)
			yield break;

		var tokenizer = new Tokenizer();
		var tryTokenize = tokenizer.Tokenize(Script.OriginalText);

		if (tryTokenize.Item2 is Error err) {
			StartCoroutine(DelayError(err));
			yield break;
		}
		Script = tryTokenize.Item1;

		// reset modules
		Interpreter = new(CreationID);
		Evaluator = new();
		Memory = new(Interpreter, "main");

		Interpreter.Evaluator = Evaluator;
		Interpreter.Memory = Memory;
		Evaluator.Interpreter = Interpreter;

		if (Script == null) yield break;

		Interpreter.Run(Memory, Script);

		// check for functions
		if (hasFunction("setup", 0, out var setupfunc)) // may change name later
			TryRun(setupfunc);

		hasTick = hasFunction("tick", 0, out tickFunc);

		// bit of a meta analysis
		// for a perchance of performance save
		if (hasTick) {
			string totalTickFunc = string.Join("",
				tickFunc.Script.Lines.Select(l =>
				l.OriginalString.Contains("return")
				? "" : l.OriginalString));

			if (string.IsNullOrWhiteSpace(totalTickFunc)) {
				hasTick = false;
				tickFunc = null;
			}
		}

		running = true; // dont run if no script present
	}

	void TryRun(Primitive.Function func) {
		var run = Interpreter.RunFunction(Memory, func, null, new()); // no args 

		if (run is Error e) {
			TryPrint(Interpreter.ID, e.Value);
		}
	}
	T_Data CPUGet(int intID) =>
		intID == Interpreter?.ID // false if int is null
		? GetInternalLanguageDataObject()
		: null;

	void TryPrint(int interpreterID, string message) {
		if (Interpreter == null) return;
		if (interpreterID != Interpreter.ID) return;

		// now do this properly
		foreach (int port in TransceiverPorts) {
			var ILPort = GetPort(interpreterID, port);

			// straight up call this lmfao
			// should work
			PartInternalFunctions.Transceiver
				.print(ILPort, new() { new Primitive.String(message) });
		}
	}

	double? TryGiveTime(int intID) {
		if (intID != Interpreter.ID) return null;

		return Time.timeAsDouble - CreationTime;
	}

	T_Data GetPort(int interpreterID, int id) {
		if (Interpreter == null) return null;
		if (interpreterID != Interpreter.ID) return null; // it will handle nulls 
														  // however illegal this may feel

		if (id < 0 || id >= Ports.Length)
			return new Error($"Port index out of range: {id}");

		var other = Ports[id].OtherPart;
		// somehow return null
		if (other == null) return null;

		other.IsNonStaticPart(out var connectedPart);
		var data = connectedPart.GetInternalLanguageDataObject();

		return data;
	}

	static IEnumerator DelayError(Error err) {
		// wait until simulating ui has been made or this will get cleared
		yield return null;
		// ? i think? idrk. its hard to get ts

		PDialog.GenerateDialog(new(
				"An error occurred while tokenizing the script",
				new PDialog.Option[] {
					new("Ok", null)
				},
				new(300, 200),
				WindowItem.NewText(
					new PComponents.Text(
						'\"' + err.Value + '\"',
						TMPro.TextAlignmentOptions.Left,
						color: Config.ScriptEditor.SyntaxColors.Literal
					),
					WindowItem.LayoutConfig.LayoutElementDynamic()
				)
			));
	}

	bool hasFunction(string name, int paramcount, out Primitive.Function func) {
		var data = Memory.Get(name);
		func = null;
		if (data is Error) return false;

		func = data as Primitive.Function;
		return data is Primitive.Function
			&& func.Parameters.Length == paramcount;
	}

	// for script run rate consistency, run this in fixedupdate
	void FixedUpdate() {
		if (hasTick && running) {
			// dont copy memory, persistent memory to allow state persistence between ticks
			TryRun(tickFunc);
		}
	}

	public class CPart : Construct.Part {
		public string Script; // could use bytearray but dont wanna risk issues w encoding into json

		public override void FinalizeInstantiation(GameObject instantiatedPart, GameObject creation) {
			var newCPU = instantiatedPart.GetComponent<Part_CPU>();

			newCPU.Script = 
				Script != null
				? ScriptSaveLoad.ConvertStringToScript(Script)
				: null;

			// subscribe to print on a delay
			// need to delay so internalfunctions.onprint is guaranteed
			// to have been nulled
			// cuz it all runs off the same onstartsimulating event
			// and the order is random
			// but the fields can be copied over first so that's what we do here 
			newCPU.StartCoroutine(newCPU.DelayScriptSetup());
		}
	}

	public override void FinalizeCPartConversion(ref Construct.Part CPart) {
		var cpu = new CPart();

		cpu.CopyMembers(CPart);
		cpu.Script =
			Script != null
			? ScriptSaveLoad.ConvertScriptToString(Script)
			: null;

		CPart = cpu;
	}

	public override void FinalizeCPartReconstruction(Construct.Part originalCPart, Part unfinishedPart, Assembly unfinishedAssembly) {
		var cpa = (CPart)originalCPart; // if this errors then something has gone wrong

		Script = 
			cpa.Script != null
			? ScriptSaveLoad.ConvertStringToScript(cpa.Script)
			: null;
	}
}