public static class Errors
{
	public static Error template()
		=> new($"");

	public static Error Custom(string message)
		=> new(message);

	public static Error InvaidArgumentCount(string funcName, int expected, int got)
		=> new($"Function \"{funcName}\" expects {expected} args, got {got}");

	public static Error UnknownVariable(string name)
		=> new($"Unknown variable \"{name}\"");

	public static Error InvalidCast(string from, string to)
		=> new($"Cannot cast a {from} to a {to}");

	public static Error CannotParseValueAs(string value, string @as)
		=> new($"Cannot parse {value} as {@as}");
}