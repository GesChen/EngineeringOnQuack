using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InternalFunctions
{
	// for all internal functions, throwaway the arg at thisref since there is no "this"

	// normal internal methods
	public static Data print(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("print", 1, args.Count);

		Data tryCast = args[0].Cast(Primitive.String.InternalType);
		if (tryCast is Error) return tryCast;

		Debug.Log((tryCast as Primitive.String).Value); // lol dont delete this debug log LMAO
		return Data.Success;
	}

	// castings
	public static Data num(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("num", 1, args.Count);
		return args[0].Cast(Primitive.Number.InternalType);
	}
	public static Data @bool(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("bool", 1, args.Count);
		return args[0].Cast(Primitive.Bool.InternalType);
	}
	public static Data str(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("str", 1, args.Count);
		return args[0].Cast(Primitive.String.InternalType);
	}
	public static Data list(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("list", 1, args.Count);
		return args[0].Cast(Primitive.List.InternalType);
	}
	public static Data dict(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("dict", 1, args.Count);
		return args[0].Cast(Primitive.Dict.InternalType);
	}

	// arithmetic
	public static Data abs(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("abs", 1, args.Count);
		if (args[0] is not Primitive.Number n) 
			return Errors.InvalidArgumentType("abs", 1, "Number", args[0].Type.Name);
		
		return new Primitive.Number(Math.Abs(n.Value));
	}
	public static Data sqrt(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("sqrt", 1, args.Count);
		if (args[0] is not Primitive.Number n) 
			return Errors.InvalidArgumentType("sqrt", 1, "Number", args[0].Type.Name);
		
		return new Primitive.Number(Math.Sqrt(n.Value));
	}
	public static Data round(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("round", 1, args.Count);
		if (args[0] is not Primitive.Number n) 
			return Errors.InvalidArgumentType("round", 1, "Number", args[0].Type.Name);
		
		return new Primitive.Number(Math.Round(n.Value));
	}
	public static Data sum(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("sum", 1, args.Count);
		if (args[0] is not Primitive.List L) 
			return Errors.InvalidArgumentType("sum", 1, "List", args[0].Type.Name);

		double sum = 0;
		foreach (Data d in L.Value) {
			if (d is not Primitive.Number n) 
				return Errors.InvalidArgumentType("sum", 1, "Numerical List", "Non-Numerical List");
			sum += n.Value;
		}
		return new Primitive.Number(sum);
	}
	public static Data max(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("max", 1, args.Count);
		if (args[0] is not Primitive.List L) 
			return Errors.InvalidArgumentType("max", 1, "List", args[0].Type.Name);

		double max = double.NegativeInfinity;
		foreach (Data d in L.Value) {
			if (d is not Primitive.Number n)
				return Errors.InvalidArgumentType("sum", 1, "Numerical List", "Non-Numerical List");

			max = Math.Max(max, n.Value);
		}
		return new Primitive.Number(max);
	}
	public static Data min(Data _, List<Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("min", 1, args.Count);
		if (args[0] is not Primitive.List L) 
			return Errors.InvalidArgumentType("min", 1, "List", args[0].Type.Name);

		double min = double.PositiveInfinity;
		foreach (Data d in L.Value) {
			if (d is not Primitive.Number n)
				return Errors.InvalidArgumentType("sum", 1, "Numerical List", "Non-Numerical List");

			min = Math.Min(min, n.Value);
		}
		return new Primitive.Number(min);
	}
}
