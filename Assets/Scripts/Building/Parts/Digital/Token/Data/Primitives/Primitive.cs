using System.Collections.Generic;

public abstract partial class Primitive : Data {
	protected Primitive(Type type) : base(type) { }

	public static List<string> TypeNames = new() {
		"Number",
		"String",
		"Bool",
		"List",
		"Dict",
		"Function"
	};
	private static Data GetEvaluator(Data thisRef, out Evaluator evaluator) {
		evaluator = null;
		Memory memory = thisRef.Memory;
		Interpreter interpreter = memory.GetInterpreter();
		if (interpreter == null) return Errors.MissingOrInvalidConnection("Interpreter", "Memory"); // TODO: FIGURE THIS OUT???
		evaluator = interpreter.GetEvaluator();
		if (evaluator == null) return Errors.MissingOrInvalidConnection("Evaluator", "Memory"); // TODO: FIGURE THIS OUT???
		return Data.Success;
	}

	public partial class Number : Primitive { }
	public partial class String : Primitive { }
	public partial class Bool : Primitive { }
	public partial class List : Primitive { }
	public partial class Dict : Primitive { }
	public partial class Function : Primitive { }
}