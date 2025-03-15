using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Memory {
	public CableConnection InterpreterCC;
	public Interpreter GetInterpreter()
		=> InterpreterCC.Cable.OtherCC(InterpreterCC).Part as Interpreter;

	public Dictionary<string, Data> Data;
	public Dictionary<string, Type> Types;

	public void Initialize() {
		Data = new() {
			// normal functions
			{ "print",		new Primitive.Function(InternalFunctions.print) },

			// castings
			{ "num",		new Primitive.Function(InternalFunctions.num)	},
			{ "bool",		new Primitive.Function(InternalFunctions.@bool)	},
			{ "str",		new Primitive.Function(InternalFunctions.str)	},
			{ "list",		new Primitive.Function(InternalFunctions.list)	},
			{ "dict",		new Primitive.Function(InternalFunctions.dict)	},

			// arithmetic
			{ "abs",		new Primitive.Function(InternalFunctions.abs)	},
			{ "sqrt",		new Primitive.Function(InternalFunctions.sqrt)	},
			{ "round",		new Primitive.Function(InternalFunctions.round)	},
			{ "sum",		new Primitive.Function(InternalFunctions.sum)	},
			{ "max",		new Primitive.Function(InternalFunctions.max)	},
			{ "min",		new Primitive.Function(InternalFunctions.min)	},

			// bool 
			{ "true",		new Primitive.Bool(true) },
			{ "false",		new Primitive.Bool(false) }
		};
		foreach (Data d in Data.Values) d.Memory = this;

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

	public Memory(Dictionary<string, Data> data, Dictionary<string, Type> types) {
		Data = data;
		Types = types;
	}
	public Memory(CableConnection interpreterCC) {
		Data = new();
		Types = new();
		InterpreterCC = interpreterCC;
	}

	public Memory Copy() {
		
		return new(
			new Dictionary<string, Data>(Data),
			new Dictionary<string, Type>(Types)
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
		return Errors.UnknownName(name);
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
