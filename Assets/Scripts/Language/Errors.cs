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
		Error("Cannot evaluate a string as an expression", interpreter);
	
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
		Error($"Division by zero", interpreter);

	public static Output MalformedString(string s, Interpreter interpreter) =>
		Error($"Cannot parse malformed string: \"{s}\"", interpreter);

	public static Output MalformedList(string s, Interpreter interpreter) =>
		Error($"Cannot parse malformed list: {s}", interpreter);

	public static Output UnknownVariable(string name, Interpreter interpreter) =>
		Error($"Unknown variable \"{name}\"", interpreter);

	public static Output UnknownFunction(string name, Interpreter interpreter) =>
		Error($"Unknown function \"{name}\"", interpreter);

	public static Output UnableToParseAsBool(string s, Interpreter interpreter) =>
		Error($"Unable to parse {s} as bool", interpreter);

	public static Output IndexListWithType(string type, Interpreter interpreter) =>
		Error($"Cannot index a list with a {type} (only whole numbers are allowed)", interpreter);

	public static Output IndexOutOfRange(int index, Interpreter interpreter) =>
		Error($"List index {index} was out of range", interpreter);

	public static Output EvaluatedNothing(Interpreter interpreter) =>
		Error($"Cannot evaluate nothing", interpreter);

	public static Output FunctionAlreadyExists(string name, int numArgs, Interpreter interpreter) =>
		Error($"Function \"{name}\" with {numArgs} args already exists", interpreter);

	public static Output ClassAlreadyExists(string name, Interpreter interpreter) =>
		Error($"A class called \"{name}\" already exists", interpreter);


	public static Output NoFunctionExists(string name, int numargs, Interpreter interpreter) =>
		Error($"No function \"{name}\" exists that takes {numargs} arguments", interpreter);
	
	public static Output UnexpectedNumberofArgs(string name, int expected, int got, Interpreter interpreter) =>
		Error($"Unexpected number of args for method \"{name}\": got {got}, expected {expected}", interpreter);

	public static Output CannotSetKeyword(string name, Interpreter interpreter) =>
		Error($"Cannot set keyword \"{name}\"", interpreter);

	public static Output UnexpectedIndent(Interpreter interpreter) =>
		Error($"Unexpected indent", interpreter);

	public static Output ExpectedColon(Interpreter interpreter) =>
		Error($"Expected colon", interpreter);

	public static Output ExpectedBoolInIf(string gottype, Interpreter interpreter) =>
		Error($"Expected a boolean expression in if statement, got {gottype}", interpreter);

	public static Output UnexpectedStatementAfterParentheses(string statement, Interpreter interpreter) =>
		Error($"Unexpected statement \"{statement}\" after parentheses", interpreter);

	public static Output ExpectedCustom(string expected, Interpreter interpreter) =>
		Error($"Expected {expected}", interpreter);

	public static Output ExpectedParentheses(Interpreter interpreter) =>
		Error($"Expected parentheses", interpreter);


	public static Output UnexpectedElse(Interpreter interpreter) =>
		Error($"Unexpected else statement", interpreter);
	
	public static Output BadVariableName(string badname, Interpreter interpreter) =>
		Error($"Bad variable name: {badname}", interpreter);

	public static Output BadFunctionName(string badname, Interpreter interpreter) =>
		Error($"Bad function name: {badname}", interpreter);
	
	public static Output BadClassName(string badname, Interpreter interpreter) =>
		Error($"Bad class name: {badname}", interpreter);

	public static Output UnexpectedCatch(Interpreter interpreter) =>
		Error("Unexpected catch statement", interpreter);

	public static Output EmptyFunction(Interpreter interpreter) =>
		Error("Function definition is empty", interpreter);

	public static Output DuplicateArguments(string duplicatename, Interpreter interpreter) =>
		Error($"Duplicate arguments: {duplicatename}", interpreter);

	public static Output MaxRecursion(int maxdepth, Interpreter interpreter) =>
		Error($"Max recursion depth reached ({maxdepth})", interpreter);

	public static Output ExpectedClassDef(Interpreter interpreter) =>
		Error($"Expected class definition", interpreter);

	public static Output AlreadyIsClass(string name, Interpreter interpreter) =>
		Error($"Cannot set variable \"{name}\" as it is a class", interpreter);

	public static Output AlreadyIsFunction(string name, Interpreter interpreter) =>
		Error($"Cannot set variable \"{name}\" as it is a function", interpreter);

	public static Output InterpreterDoesntHaveEval(Interpreter interpreter) =>
		Error("Interpreter doesn't have an evaluator. How did you manage to do this?", interpreter);

	public static Output TypeHasNoAttributes(string typeName, Interpreter interpreter) =>
		Error($"A {typeName} has no attributes", interpreter);
}