using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Memory {
	public CableConnection InterpreterCC;
	public Interpreter GetInterpreter()
		=> InterpreterCC.Cable.OtherCC(InterpreterCC).Part as Interpreter;

	public Dictionary<string, Data> Data;
	public Dictionary<string, Type> Types;
	public Section Script;

	public void Initialize() {
		Data = new() {
			{ "print",		new Primitive.Function(InternalFunctions.print) },
			{ "true",		new Primitive.Bool(true) },
			{ "false",		new Primitive.Bool(false) }
		};

		Types = new() {
			{ "Number",		Primitive.Number.	InternalType },
			{ "String",		Primitive.String.	InternalType },
			{ "Bool",		Primitive.Bool.		InternalType },
			{ "List",		Primitive.List.		InternalType },
			{ "Dict",		Primitive.Dict.		InternalType },
			{ "Function",	Primitive.Function.	InternalType },
			{ "Error",				  Error.	InternalType }
		};
	}

	public Memory(Dictionary<string, Data> data, Dictionary<string, Type> types, Section script) {
		Data = data;
		Types = types;
		Script = script;
	}
	public Memory(CableConnection interpreterCC) {
		Data = new();
		Types = new();
		Script = null;
		InterpreterCC = interpreterCC;
	}

	public Memory Copy() {
		return new(
			new Dictionary<string, Data>(Data),
			new Dictionary<string, Type>(Types),
			new(Script.Lines)
			);
	}

	public bool Exists(string name) {
		return Data.ContainsKey(name);
	}

	/// <summary>
	/// Returns data value if found, otherwise error
	/// </summary>
	public Data Get(string name) {
		if (Data.ContainsKey(name)) return Data[name];
		return Errors.UnknownVariable(name);
	}

	public void Set(string name, Data data) {
		Data[name] = data;

		if (Types.ContainsKey(name))
			Types.Remove(name);
	}

	public Data Set(Token.Reference reference, Data data) {
		return reference.SetData(data);
	}

	public void NewType(string name, Type type) {
		Types[name] = type;

		if (Data.ContainsKey(name))
			Data.Remove(name);
	}

	public override string ToString() {
		return $"Memory object";
	}

	public static Data GetEvaluator(Data thisRef, out Evaluator evaluator) {
		evaluator = null;
		Memory memory = thisRef.Memory;
		Interpreter interpreter = memory.GetInterpreter();
		if (interpreter == null) return Errors.MissingOrInvalidConnection("Interpreter", "Memory"); // TODO: FIGURE THIS OUT???
		evaluator = interpreter.Evaluator;
		return global::Data.Success;
	}
}
