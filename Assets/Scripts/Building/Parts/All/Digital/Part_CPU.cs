using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part_CPU : NonStaticPart {
	public override string PartName => "CPU";
	public List<Port> Ports; // assign in inspector

	public override void SetupPart(Part main) {
		main.Ports = Ports.ToArray();
	}

	internal static Script currentlyEditingScript;

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

	Port[] TransceiverPorts;

	Interpreter Interpreter;
	Memory Memory;
	Evaluator Evaluator;
	bool hasTick;
	Primitive.Function tickFunc;

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
	public T_Data InternalDataObject = new(Type_CPU);
	public override T_Data InternalLanguageDataObject() => InternalDataObject;

	public override void OnStopSimulating() {
		running = false;

		hasTick = false;
		tickFunc = null;
	}

	public override void FinalizeInstantiation(GameObject instantiatedPart) {
		var newCPU = instantiatedPart.GetComponent<Part_CPU>();

		newCPU.Script = Script;
		newCPU.running = running;
		newCPU.Interpreter = Interpreter;
		newCPU.Memory = Memory;
		newCPU.Evaluator = Evaluator;
		newCPU.hasTick = hasTick;
		newCPU.tickFunc = tickFunc;

		// subscribe to print on a delay
		// need to delay so internalfunctions.onprint is guaranteed
		// to have been nulled
		// cuz it all runs off the same onstartsimulating event
		// and the order is random
		// but the fields can be copied over first so that's what we do here 
		newCPU.StartCoroutine(newCPU.DelayScriptSetup());
	}

	IEnumerator DelayScriptSetup() {
		yield return null;

		InternalFunctions.OnPrintCalled += TryPrint;
		PartInternalFunctions.CPU.OnPortCalled += GetPort;
		Memory.CPUGet += CPUGet;

		// find transceiever ports
		TransceiverPorts = Ports.Where(
			p => p.Connector.ConnectedPart
				.TryGetComponent<Part_Transceiver>(out _))
			.ToArray(); // ugly

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
		Interpreter = new();
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
		if (!string.IsNullOrWhiteSpace(string.Join("",
			tickFunc.Script.Lines.Select(l => 
				l.OriginalString.Contains("return")
				? "" : l.OriginalString)))) {
			hasTick = false;
			tickFunc = null;
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
		intID == Interpreter.ID
		? InternalLanguageDataObject()
		: null;

	void TryPrint(int interpreterID, string message) {
		if (interpreterID != Interpreter.ID) return;

		foreach (var port in TransceiverPorts)
			port.CallCommand("print", new[] { message });
	}

	T_Data GetPort(int interpreterID, int id) {
		if (interpreterID != Interpreter.ID) return null; // it will handle nulls 
														  // however illegal this may feel

		Ports[id].OtherPart.IsNonStaticPart(out var connectedPart);
		var data = connectedPart.InternalLanguageDataObject();

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

		sp.Script =
			Script != null
			? ScriptSaveLoad.ConvertScriptToString(Script)
			: null;

		SPart = sp;
	}

	public override void FinalizeSPartReconstruction(Assembly.SPart originalSPart, Part unfinishedPart, Assembly unfinishedAssembly) {
		var sp = (SPart_CPU)originalSPart; // if this errors then something has gone wrong

		Script = 
			sp.Script != null
			? ScriptSaveLoad.ConvertStringToScript(sp.Script)
			: null;
	}

	public override void HandleCommand(string command, object[] args) {
		
		Debug.LogError(UnknownCommand(command));
	}
}