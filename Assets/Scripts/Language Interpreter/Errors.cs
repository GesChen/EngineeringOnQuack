public static class Errors 
{
	public static Output Error(string error, Interpreter interpreter) =>
		new (new Error(error, interpreter));
	
	public static Output TestError(Interpreter interpreter) =>
		new (new Error("test", interpreter));

	public static Output MismatchedParentheses(Interpreter interpreter) =>
		Error("Mismatched parentheses", interpreter);
	
	public static Output MismatchedBrackets(Interpreter interpreter) =>
		Error("Mismatched brackets", interpreter);

	public static Output AttemptedEvalStringAsExpr(Interpreter interpreter) =>
		Error("Attempted to evaluate a string as an expression", interpreter);
	
	public static Output OperatorInBadPosition(string op, Interpreter interpreter) =>
		Error($"Operator {op} in bad position", interpreter);
	public static Output OperatorInBadPosition(Interpreter interpreter) =>
		Error($"An operator is in a bad position", interpreter);

	public static Output OperatorDoesntExist(string op, Interpreter interpreter) =>
		Error($"Operator {op} doesn't exist", interpreter);

	public static Output OperatorMissingSide(string op, Interpreter interpreter) =>
		Error($"One side of operator {op} is missing a number", interpreter);

	public static Output UnableToParseStrAsNum(string str, Interpreter interpreter) =>
		Error($"Unable to parse \"{str}\" as number", interpreter);

	public static Output OperationFailed(string op, Interpreter interpreter) =>
		Error($"Operation {op} failed unexpectedly", interpreter);

	public static Output InvaidString(string str, Interpreter interpreter) =>
		Error($"String \"{str}\" is not a valid string", interpreter);

	public static Output UnsupportedOperation(string op, string type1, string type2, Interpreter interpreter) =>
		Error($"Unsupported operation: {op} between {type1} and {type2}", interpreter);
	
	public static Output DivisionByZero(Interpreter interpreter) =>
		Error($"Attempted to divide by zero", interpreter);

	public static Output MalformedString(string s, Interpreter interpreter) =>
		Error($"String {s} was malformed, could not parse", interpreter);

	public static Output MalformedList(string s, Interpreter interpreter) =>
		Error($"Could not parse malformed list: {s}", interpreter);

	public static Output UnknownVariable(string name, Interpreter interpreter) =>
		Error($"Unknown variable \"{name}\"", interpreter);

	public static Output UnableToParseBool(Interpreter interpreter) =>
		Error($"Unable to parse bool", interpreter);

	public static Output IndexListWithType(string type, Interpreter interpreter) =>
		Error($"Attempted to index a list with a {type} (only whole numbers are allowed)", interpreter);

	public static Output IndexOutOfRange(int index, Interpreter interpreter) =>
		Error($"List index {index} was out of range", interpreter);

	public static Output EvaluatedNothing(Interpreter interpreter) =>
		Error($"Attempted to evaluate nothing", interpreter);
}
