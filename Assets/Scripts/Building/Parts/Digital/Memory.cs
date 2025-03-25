using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Token;

public class Memory {
	public string Nick; // nickname for debugging, remove in final

	public CableConnection InterpreterCC;
	public Interpreter GetInterpreter()
		=> InterpreterCC.Cable.OtherCC(InterpreterCC).Part as Interpreter;

	public Dictionary<string, Data> Data;
	public Dictionary<string, Type> Types;

	public static Dictionary<string, Data> StaticData = new() {
		// normal functions
		{ "breakpoint",	new Primitive.Function(InternalFunctions.breakpoint)},
		{ "print",		new Primitive.Function(InternalFunctions.print)	},

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
	public static Dictionary<string, Type> StaticTypes = new() {
		{ "Number",		Primitive.Number.	InternalType },
		{ "String",		Primitive.String.	InternalType },
		{ "Bool",		Primitive.Bool.		InternalType },
		{ "List",		Primitive.List.		InternalType },
		{ "Dict",		Primitive.Dict.		InternalType },
		{ "Function",	Primitive.Function.	InternalType },
		{ "Error",				  Error.	InternalType }
	};

	public void Initialize() {
		foreach (Data d in Data.Values) d.Memory = this;
	}

	public Memory(Dictionary<string, Data> data, Dictionary<string, Type> types, string nick) {
		Data = data;
		Types = types;
		Nick = nick;
	}
	public Memory(CableConnection interpreterCC, string nick) {
		Data = new();
		Types = new();
		InterpreterCC = interpreterCC;
		Nick = nick;
	}
	public Memory(Memory original) {

	}

	public Memory Copy() {
		return new(
			new Dictionary<string, Data>(Data),
			new Dictionary<string, Type>(Types),
			$"copy of {Nick}"
			);
	}

	public bool Exists(string name) {
		return Data.ContainsKey(name);
	}

	/// <summary>
	/// Returns data value if found, otherwise error
	/// </summary>
	public Data Get(string name) {
		if (LanguageConfig.DEBUG) HF.LogColor($"{Nick}: getting {name}", Color.yellow);

		if (StaticData.ContainsKey(name)) {
			Data staticCopy = StaticData[name].Copy();
			staticCopy.Memory = this;
			return staticCopy;
		}
		if (Data.ContainsKey(name)) return Data[name];
		if (StaticTypes.ContainsKey(name) ||
			Types.ContainsKey(name))
			return Errors.TypeCannotBeUsedAsVariable(name);
		return Errors.UnknownName(name);
	}

	public Data Set(string name, Data data) {
		if (LanguageConfig.DEBUG) HF.LogColor($"{Nick}: setting {name} {data}", Color.yellow);

		if (StaticData.ContainsKey(name))
			return Errors.CannotSetBuiltin("value", name);
		if (StaticTypes.ContainsKey(name))
			return Errors.CannotSetBuiltin("type", name);
		if (Types.ContainsKey(name))
			return Errors.CannotSetType(name);

		Data[name] = data;

		return global::Data.Success;
	}

	public Data Set(Reference reference, Data data) {
		if (LanguageConfig.DEBUG) HF.LogColor($"{Nick}: setting {reference.Name} {data}", Color.yellow);

		if (reference.Name == "")
			return Errors.CannotSetLiteral();
		if (StaticTypes.ContainsKey(reference.Name))
			return Errors.CannotOverwriteBuiltin(reference.Name);

		data.Memory = this;
		return reference.SetData(data);
	}

	public Data NewType(Type type) {
		string name = type.Name;
		if (StaticTypes.ContainsKey(name))
			return Errors.CannotOverwriteBuiltin(name);
		Types[name] = type;
		return global::Data.Success;
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
