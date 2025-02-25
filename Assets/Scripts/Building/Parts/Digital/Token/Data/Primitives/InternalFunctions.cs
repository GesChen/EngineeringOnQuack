using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InternalFunctions
{
	public static Data print(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("print", 1, args.Count);

		Data tryCast = args[0].Cast(Primitive.String.InternalType);
		if (tryCast is Error) return tryCast;

		Debug.Log((tryCast as Primitive.String).Value);
		return Data.Success;
	}
}
