using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Token;

public class Memory {
	public string Nick; // nickname for debugging, remove in final

	public Interpreter Interpreter;

	public Dictionary<string, Data> Data;
	public Dictionary<string, Type> Types;

	public static Dictionary<string, Data> StaticData = new() {
		// normal functions
		{ "breakpoint",	new Primitive.Function("breakpoint",InternalFunctions.breakpoint)},
		{ "print",		new Primitive.Function("print",		InternalFunctions.print)	},

		// castings
		{ "num",		new Primitive.Function("num",		InternalFunctions.num)		},
		{ "bool",		new Primitive.Function("bool",		InternalFunctions.@bool)	},
		{ "str",		new Primitive.Function("str",		InternalFunctions.str)		},
		{ "list",		new Primitive.Function("list",		InternalFunctions.list)		},
		{ "dict",		new Primitive.Function("dict",		InternalFunctions.dict)		},

		// arithmetic
		{ "abs",		new Primitive.Function("abs",		InternalFunctions.abs)		},
		{ "sqrt",		new Primitive.Function("sqrt",		InternalFunctions.sqrt)		},
		{ "round",		new Primitive.Function("round",		InternalFunctions.round)	},
		{ "sum",		new Primitive.Function("sum",		InternalFunctions.sum)		},
		{ "max",		new Primitive.Function("max",		InternalFunctions.max)		},
		{ "min",		new Primitive.Function("min",		InternalFunctions.min)		},

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
	public Memory(Interpreter interpreter, string nick) {
		Data = new();
		Types = new();
		Interpreter = interpreter;
		Nick = nick;
	}
	public Memory(Memory original) {
		Data = new Dictionary<string, Data>(original.Data);
		Types = new Dictionary<string, Type>(original.Types);
		Interpreter = original.Interpreter;
		Nick = $"Copy of {original.Nick}";
	}

	public Memory Copy() {
		return new(this);
	}

	public bool Exists(string name) {
		return Data.ContainsKey(name);
	}

	public string MemoryDump() {
		return $"memory dump: \n{string.Join("\n", Data)}";
	}

	/// <summary>
	/// Returns data value if found, otherwise error
	/// </summary>
	public Data Get(string name) {
		if (LanguageConfig.DEBUG) HF.WarnColor($"{Nick}: getting {name}\n{MemoryDump()}", Color.yellow);

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
		if (LanguageConfig.DEBUG) HF.WarnColor($"{Nick}: name setting {name} {data}\n{MemoryDump()}", Color.yellow);

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
		if (LanguageConfig.DEBUG) HF.WarnColor($"{Nick}: ref setting {reference.Name} {data}\n{MemoryDump()}", Color.yellow);

		if (reference.Name == "")
			return Errors.CannotSetLiteral();
		if (StaticTypes.ContainsKey(reference.Name))
			return Errors.CannotOverwriteBuiltin(reference.Name);

		//data.Memory = this; // this might have served a purpose but comment it once i figure out what it was
		return reference.SetData(this, data);
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
		Interpreter interpreter = memory.Interpreter;
		if (interpreter == null) return Errors.MissingOrInvalidConnection("Interpreter", "Memory"); // TODO: FIGURE THIS OUT???
		evaluator = interpreter.Evaluator;
		return global::Data.Success;
	}
}
