using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part_CPU : NonStaticPart {

	static Script currentlyEditingScript;
	public static void SetupStatic() {

		CPU_UI.GetCurrentScript = null;
		CPU_UI.GetCurrentScript += () => {
			Part_CPU cpu = SelectionManager.Instance.PartSelection[0].GetComponent<Part_CPU>();
			var script = cpu.Script;

			if (script == null) {
				Tokenizer tokenizer = new();

				// should always tokenize properly??
				(Script newScript, _) = tokenizer.Tokenize(
					"setup():\n\t\n\ntick():\n\t\n");

				newScript.Name = "New Script";

				script = newScript;
				cpu.Script = script;
			}

			currentlyEditingScript = script;

			return (
				script.OriginalText.Split('\n'),
				script.Name
			);
		};

		// probably will be changed cuz this is kinda spaghetti
		SEProcedural.OnFileNameChanged = null;
		SEProcedural.OnFileNameChanged +=
			name => currentlyEditingScript.Name = name;

		SEProcedural.OnSetup = null;
		SEProcedural.OnSetup += () => {
			SEProcedural.ScriptEditor.OnScriptUpdated = null;
			SEProcedural.ScriptEditor.OnScriptUpdated += content => {
				currentlyEditingScript.OriginalText = string.Join('\n', content);
			};
		};

	}

	public Script Script;
	public string DEBUG_CurrentScriptText; // for debugging purposes
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

	Interpreter Interpreter;
	Memory Memory;
	Evaluator Evaluator;

	bool hasTick;
	Primitive.Function tickFunc;

	public override void OnStopSimulating() {
		running = false;

		hasTick = false;
		tickFunc = null;
	}
	
	public override void OnStartSimulating() {
		if (Script == null) {
			Debug.Log($"script is null, no run");
			return;
		}

		var tokenizer = new Tokenizer();
		var tryTokenize = tokenizer.Tokenize(Script.OriginalText);

		if (tryTokenize.Item2 is Error err) {
			PDialog.GenerateDialog(new(
				"An error occurred while tokenizing the script",
				new PDialog.Option[] {
					new("Ok", null)
				},
				new(300, 200),
				WindowItem.NewText(
					new PComponents.Text(
						'\"'+err.Value+'\"',
						color: Config.ScriptEditor.SyntaxColors.Literal
					),
					WindowItem.LayoutConfig.LayoutElementDynamic()
				)
			));

			return;
		}
		Script = tryTokenize.Item1;

		// reset modules
		Interpreter = new();
		Evaluator = new();
		Memory = new(Interpreter, "main");

		Interpreter.Evaluator = Evaluator;
		Interpreter.Memory = Memory;
		Evaluator.Interpreter = Interpreter;
		Debug.Log($"start running");

		if (Script == null) return;

		Interpreter.Run(Memory, Script);

		// check for functions
		if (hasFunction("setup", 0, out var setupfunc)) // may change name later
			Interpreter.RunFunction(Memory, setupfunc, null, new()); // no args 

		hasTick = hasFunction("tick", 0, out tickFunc);

		running = true; // dont run if no script present
		Debug.Log($"run");

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
		if (hasTick && running)
			Interpreter.RunFunction(Memory, tickFunc, null, new());
			// dont copy memory, persistent memory to allow state persistence between ticks
	}

	public class SPart_CPU : Assembly.SPart {
		public string Script; // could use bytearray but dont wanna risk issues w encoding into json
	}

	public override void FinalizeSPartConversion(ref Assembly.SPart SPart) {
		var sp = new SPart_CPU { // did you know you dont actually need the ()
			basePartID = SPart.basePartID,
			id = SPart.id,
			position = SPart.position,
			rotation = SPart.rotation,
			scale = SPart.scale,
			color = SPart.color,
			compositionID = SPart.compositionID,
		};

		sp.Script = ScriptSaveLoad.ConvertScriptToString(Script);

		SPart = sp;
	}

	public override void FinalizeSPartReconstruction(Assembly.SPart originalSPart, Part unfinishedPart) {
		var sp = (SPart_CPU)originalSPart; // if this errors then something has gone wrong

		Script = ScriptSaveLoad.ConvertStringToScript(sp.Script);
	}
}