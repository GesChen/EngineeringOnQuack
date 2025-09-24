using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part_CPU : NonStaticPart {

	public Script Script;

	bool running = false; // not sure if this is the best way of doing this
						  // might instead have override onsimulatingupdate or something like that instead
						  // but we'll see
						  // this shouldnt be hard to modify either

	Interpreter Interpreter;
	Memory Memory;
	Evaluator Evaluator;

	bool hasTick;
	Primitive.Function tickFunc;

	// call tokenization where this is called
	public void UpdateScript(Script script) { Script = script; }

	public override void OnStopSimulating() {
		running = false;

		hasTick = false;
		tickFunc = null;
	}
	
	public override void OnStartSimulating() {
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
		if (!running) return;

		if (hasTick)
			Interpreter.RunFunction(Memory, tickFunc, null, new());
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