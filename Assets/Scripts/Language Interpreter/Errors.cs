public static class Errors 
{
	public static Output Error(string error, Interpreter interpreter) =>
		new (new Error(error, interpreter));
	
	public static Output TestError(Interpreter interpreter) =>
		new (new Error("test", interpreter));

	public static Output MismatchedParentheses(Interpreter interpreter) =>
		new (new Error("Mismatched parentheses", interpreter));

	public static Output AttemptedEvalStringAsExpr(Interpreter interpreter) =>
		new(new Error("Attempted to evaluate a string as an expression", interpreter));
	
	public static Output OperatorInBadPosition(string op, Interpreter interpreter) =>
		new(new Error($"Operator {op} in bad position", interpreter));
	public static Output OperatorInBadPosition(Interpreter interpreter) =>
		new(new Error($"An operator is in a bad position", interpreter));

	public static Output OperatorDoesntExist(string op, Interpreter interpreter) =>
		new(new Error($"Operator {op} doesn't exist", interpreter));

	public static Output OperatorMissingSide(string op, Interpreter interpreter) =>
		new(new Error($"One side of operator {op} is missing a number", interpreter));

	public static Output UnableToParse(string str, Interpreter interpreter) =>
		new(new Error($"Unable to parse \"{str}\" as number", interpreter));

	public static Output OperationFailed(string op, Interpreter interpreter) =>
			new(new Error($"Operation {op} failed unexpectedly", interpreter));
}
