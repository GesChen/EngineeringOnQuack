public static class Errors {
	public static Error Custom(string message)
		=> new(message);
	public static Error InvalidArgumentCount(string funcName, int expected, int got)
		=> new($"Function \"{funcName}\" expects {expected} args, got {got}");
	public static Error InvalidArgumentType(string funcName, int index, string expected, string got)
		=> new($"Function \"{funcName}\" expects {expected} in argument {index}, got {got} instead");
	public static Error UnknownName(Token.Reference reference)
		=> new($"Unknown name \"{reference.Name}\"");
	public static Error UnknownName(string name)
		=> new($"Unknown name \"{name}\"");
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
	public static Error CannotIndexWithType(string type)
		=> new($"Cannot index with {type}, must be whole numbers");
	public static Error IndexOutOfRange(int attempt)
		=> new($"Index out of range: {attempt}");
	public static Error CannotSetMemberOfPrimitive(string membername)
		=> new($"Cannot set a member of a primitive value: {membername}");
	public static Error CannotSetLiteral()
		=> new($"Cannot set a literal to another value");
	public static Error InvalidUseOfOperator(string op)
		=> new($"Invalid use of operator {op}");
	public static Error InvalidCharacter(char c)
		=> new($"Invalid character {HF.Repr(c.ToString())}");
	public static Error MismatchedSomething(string mismatched)
		=> new($"Mismatched {mismatched}");
	public static Error VarNameCannotStartWithNum()
		=> new($"Variable name cannot start with a number");
	public static Error UnknownOperator(string op)
		=> new($"Unknown operator {op}");
	public static Error CouldntParse(string line)
		=> new($"Couldn't parse \"{line}\"");
	public static Error MemberIsNotMethod(string membername, string of)
		=> new($"Member {membername} of {of} is not a method");
	public static Error CannotUseTypeWithFeature(string type, string feature)
		=> new($"Cannot use {type} type with {feature}");
	public static Error CannotEvaluateEmpty()
		=> new($"Cannot evaluate empty item"); // change wording? idk
	public static Error BadSyntaxFor(string thing)
		=> new($"Bad syntax for {thing}");
	public static Error BadSyntaxFor(string thing, string reason)
		=> new($"Bad syntax for {thing}: {reason}");
	public static Error UnknownKey()
		=> new($"Unknown key");
	public static Error UnknownKey(string key)
		=> new($"Unknown key: {key}");
	public static Error UnsupportedOperation(string op, string typeA, string typeB)
		=> new($"Unsupported operation: {typeA} {op} {typeB}");
	public static Error DivisonByZero()
		=> new($"Division by zero");
	public static Error Expected(string what, string where)
		=> new($"Expected {what} {where}");
	public static Error Expected(string what)
		=> new($"Expected {what}");
	public static Error Unexpected(string what, string where)
		=> new($"Unexpected {what} {where}");
	public static Error Unexpected(string what)
		=> new($"Unexpected {what}");
	public static Error template()
		=> new($"");
}