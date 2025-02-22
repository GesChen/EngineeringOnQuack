public static class Errors
{
	public static Primitive.Error template()
		=> new($"");

	public static Primitive.Error InvaidArgumentCount(string funcName, int expected, int got)
		=> new($"Function \"{funcName}\" expects {expected} args, got {got}");

	public static Primitive.Error UnknownVariable(string name)
		=> new($"Unknown variable \"{name}\"");
}