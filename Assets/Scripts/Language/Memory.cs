using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Token;

public class Memory {
	public string Nick; // nickname for debugging, remove in final

	public Interpreter Interpreter;

	public Dictionary<string, T_Data> Data;
	public Dictionary<string, Type> Types;

	// data from this wont have a type field cuz those fields are still null
	static Dictionary<string, T_Data> m_staticdata;
	public static Dictionary<string, T_Data> StaticData =>
		HF.LoadCached(ref m_staticdata, () => new() {
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
		{ "false",		new Primitive.Bool(false) },

		// extra
		{ "time",		new Primitive.Function("time",		InternalFunctions.time) },
		{ "input",		new InternalInputType() }
	});

	public static Dictionary<string, Type> StaticTypes = new() {
		{ "Number",		Primitive.Number.	InternalType },
		{ "String",		Primitive.String.	InternalType },
		{ "Bool",		Primitive.Bool.		InternalType },
		{ "List",		Primitive.List.		InternalType },
		{ "Dict",		Primitive.Dict.		InternalType },
		{ "Function",	Primitive.Function.	InternalType },
		{ "Error",				  Error.	InternalType },
		{ "input",		InternalInputType	.InternalType }
	};


	public void Initialize() {
		foreach (T_Data d in Data.Values) d.Memory = this;
	}

	public Memory(Dictionary<string, T_Data> data, Dictionary<string, Type> types, string nick) {
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
	
	// potential to be really fucking slow by the way
	public Memory(Memory original) {
		Data = new Dictionary<string, T_Data>(original.Data);
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

	public static void ClearCPUGet() { CPUGet = null; }
	public static event Func<int, T_Data> CPUGet;
	private T_Data GetCPU() {
		var intID = Interpreter.ID;

		foreach (var handler in CPUGet
			.GetInvocationList().Cast<Func<int, T_Data>>()) {

			var call = handler?.Invoke(intID);
			if (call != null) return call;
		}
		return Errors.BadCode();
	}

	/// <summary>
	/// Returns data value if found, otherwise error
	/// </summary>
	public T_Data Get(string name, bool memberAccess = false) {
		if (Config.Language.DEBUG) HF.WarnColor($"{Nick}: getting {name}\n{MemoryDump()}", Color.yellow);

		// special handling
		if (name == "cpu") {
			return GetCPU();
		}

		if (!memberAccess && StaticData.ContainsKey(name)) {
			T_Data staticCopy = StaticData[name].Copy();
			staticCopy.Memory = this;
			return staticCopy;
		}
		if (Data.ContainsKey(name)) return Data[name];
		if (!memberAccess && StaticTypes.ContainsKey(name) ||
			Types.ContainsKey(name))
			return Errors.TypeCannotBeUsedAsVariable(name);
		return Errors.UnknownName(name);
	}

	public T_Data Set(string name, T_Data data, bool member = false) {
		if (Config.Language.DEBUG) HF.WarnColor($"{Nick}: name setting {name} {data}\n{MemoryDump()}", Color.yellow);

		// cant override global members IF this is setting from a global scope
		// members can override static data names
		if (!member
			&& StaticData.ContainsKey(name))
			return Errors.CannotSetBuiltin("value", name);
		if (StaticTypes.ContainsKey(name))
			return Errors.CannotSetBuiltin("type", name);
		if (Types.ContainsKey(name))
			return Errors.CannotSetType(name);

		Data[name] = data;

		return T_Data.Success;
	}

	public T_Data Set(T_Reference reference, T_Data data) {
		if (Config.Language.DEBUG) HF.WarnColor($"{Nick}: ref setting {reference.Name} {data}\n{MemoryDump()}", Color.yellow);

		if (reference.Name == "")
			return Errors.CannotSetLiteral();
		if (StaticTypes.ContainsKey(reference.Name))
			return Errors.CannotOverwriteBuiltin(reference.Name);

		//data.Memory = this; // this might have served a purpose but comment it once i figure out what it was
		return reference.SetData(this, data);
	}

	public T_Data NewType(Type type) {
		string name = type.Name;
		if (StaticTypes.ContainsKey(name))
			return Errors.CannotOverwriteBuiltin(name);
		Types[name] = type;
		return T_Data.Success;
	}

	public override string ToString() {
		return $"Memory object";
	}

	public static T_Data GetEvaluator(T_Data thisRef, out Evaluator evaluator) {
		evaluator = null;
		Memory memory = thisRef.Memory;
		Interpreter interpreter = memory.Interpreter;
		if (interpreter == null) return Errors.MissingOrInvalidConnection("Interpreter", "Memory"); // TODO: FIGURE THIS OUT???
		evaluator = interpreter.Evaluator;
		return T_Data.Success;
	}
}