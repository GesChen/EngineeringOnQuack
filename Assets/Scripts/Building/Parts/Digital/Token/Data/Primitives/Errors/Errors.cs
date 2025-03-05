public static class Errors {
	public static Error Custom(string message)
		=> new(message);
	public static Error InvalidArgumentCount(string funcName, int expected, int got)
		=> new($"Function \"{funcName}\" expects {expected} args, got {got}");
	public static Error InvalidArgumentType(string funcName, int index, string expected, string got)
		=> new($"Function \"{funcName}\" expects {expected} in argument {index}, got {got} instead");
	public static Error UnknownVariable(string name)
		=> new($"Unknown variable \"{name}\"");
	public static Error InvalidCast(string from, string to)
		=> new($"Cannot cast a {from} to a {to}");
	public static Error CannotParseValueAs(string value, string @as)
		=> new($"Cannot parse {value} as {@as}");
	public static Error CannotCompare(string typeA, string typeB)
		=> new($"Cannot compare a {typeA} to a {typeB}");
	public static Error ImproperImplementation(string function, string reason = null)
		=> new($"Improper implementation of {function}" + (reason != null ? ": " + reason : ""));
	public static Error MissingOrInvalidConnection(string portName, string partFrom)
		=> new($"Missing or Invalid connection to the {portName} port on the {partFrom}");
	public static Error CannotIndex(string type)
		=> new($"Cannot index {type} object, Lists and Strings only");
	public static Error IndexOutOfRange(int attempt)
		=> new($"Index out of range: {attempt}");
	public static Error CannotSetMemberOfPrimitive(string membername)
		=> new($"Cannot set a member of a primitive value: {membername}");
	public static Error InvalidUseOfOperator(string op)
		=> new($"Invalid use of operator {op}");
	public static Error InvalidCharacter(char c)
		=> new($"Invalid character {HF.GetStringRepresentation(c.ToString())}");
	public static Error MismatchedSomething(string mismatched)
		=> new($"Mismatched {mismatched}");
	public static Error VarNameCannotStartWithNum()
		=> new($"Variable name cannot start with a number");
	public static Error UnknownOperator(string op)
		=> new($"Unknown operator {op}");
	public static Error CouldntParse(string line)
		=> new($"Couldn't parse \"{line}\"");
	public static Error template()
		=> new($"");
}