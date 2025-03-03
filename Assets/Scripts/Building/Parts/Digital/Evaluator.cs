using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Evaluator : MonoBehaviour {
	public Interpreter Interpreter;

	public Data Compare(Data a, Data b, Token.Operator op, Memory memory) {
		// not gona add comparison operator check bc whatever's calling this should already have done it

		Data lt = LessThan(a, b, memory, Interpreter); // is bool checks done in the methods already
		if (lt is Error) return lt;

		Data eq = Equals(a, b, memory, Interpreter);
		if (eq is Error) return eq;

		bool lessthan = (lt as Primitive.Bool).Value;
		bool equals = (eq as Primitive.Bool).Value;

		if (op.StringValue == "==") {
			return eq;
		}
		else if (op.StringValue == "!=") { // (!=) = (! ==)
			return new Primitive.Bool(!equals);
		}
		else if (op.StringValue == "<") {
			return lt;
		}
		else if (op.StringValue == ">") { // (>) = (!<=) 
			return new Primitive.Bool(!(lessthan || equals));
		}
		else if (op.StringValue == "<=") { // (<=) = (< || ==)
			return new Primitive.Bool(lessthan || equals);
		}
		else if (op.StringValue == ">=") { // (>=) = (! <)
			return new Primitive.Bool(!lessthan);
		}
		return Errors.CannotCompare(a.Type.Name, b.Type.Name);
	}
	private Data Equals(Data a, Data b, Memory memory, Interpreter interpreter) { // op should either be == or !=
																				  // try to get the eq operator from either a or b, if neither has one then return false
		Data f = a.GetMember("eq");
		bool functionIsA = true;
		if (f is not Primitive.Function) {
			f = b.GetMember("eq");
			functionIsA = false;

			if (f is not Primitive.Function)
				//return Errors.CannotCompare(a.Type.Name, b.Type.Name);
				return new Primitive.Bool(false); // say false for now, cuz its just equals so if neither side has equals operator then neither is equal
		}

		Primitive.Function function = f as Primitive.Function;
		if (!functionIsA) // swap if we use b's function
			(a, b) = (b, a);

		Data run = interpreter.RunFunction(memory, function, a, new() { b });
		if (run is Error) return run;
		if (run is not Primitive.Bool)
			return Errors.ImproperImplementation($"the eq operator", "Should return a bool");

		return run; // more handling to be done by specifics
	}
	private Data LessThan(Data a, Data b, Memory memory, Interpreter interpreter) {
		// try to get the eq operator from the data
		Data f = a.GetMember("lt");
		if (f is Error) return f;
		if (f is not Primitive.Function)
			return Errors.CannotCompare(a.Type.Name, b.Type.Name);

		Primitive.Function function = f as Primitive.Function;

		Data run = interpreter.RunFunction(memory, function, a, new() { b });
		if (run is Error) return run;
		if (run is not Primitive.Bool)
			return Errors.ImproperImplementation($"the lt operator", "Should return a bool");

		return run; // more handling to be done by specifics
	}

	enum Actions {
		None,
		Data,
		Name,
		DotOperator
	}
	public Data Evaluate(int flags, Line line) {

		List<Token> remaining = line.Tokens;
		List<Token> last = new();

		while (remaining.Count > 1) { // main loop

			// find leftmost and highest precedence
			Actions action = Actions.None;

			int highestPrecedence = -1;
			int highestIndex = -1;

			for (int i = 0; i < remaining.Count; i++) {
				Token token = remaining[i];

				int precedence = -1;
				if (token is Data) {
					precedence = 15;
					action = Actions.Data;
				}
				if (token is Token.Name) {
					precedence = 10;
					action = Actions.Name;
				}
				else if (token is Token.Operator op && op.StringValue == ".") {
					precedence = 10;
					action = Actions.DotOperator;
				}

				if (precedence > highestPrecedence) { // > not >= for leftmost
					highestPrecedence = precedence;
					highestIndex = i;
				}
			}
			Token highestToken = remaining[highestIndex];

			switch (action) {
				case Actions.Data: // converts data into reference
					Data data = remaining[highestIndex] as Data;
					remaining[highestIndex] = Token.Reference.ExistingGlobalReference("", data);
					break;
				case Actions.Name:
					// try get memory
					Data tryget = Interpreter.TryGetMemory(out Memory memory);
					if (tryget is Error) return tryget;

					// check memory for name 
					string name = (highestToken as Token.Name).Value;
					Data get = memory.Get(name);

					// replace name token with reference token
					remaining[highestIndex] = (get is not Error) ?
						Token.Reference.ExistingGlobalReference(name, get) : // make existing if data exists
						Token.Reference.NewGlobalReference(name);			// or make new
					break;
				case Actions.DotOperator:
					// check left and right for number (ref) and name (ref)
					Token left = remaining.ElementAtOrDefault(highestIndex - 1);
					Token right = remaining.ElementAtOrDefault(highestIndex + 1);

					// right is number, handle decimal

					// fat garbage that used to be 2 lines of casting ugh stupid errors
					Token.Reference rightRef = right as Token.Reference;
					Token.Reference leftRef = left as Token.Reference;
					Primitive.Number rightAsNumber = rightRef != null ? rightRef.ThisReference as Primitive.Number : null;
					Primitive.Number leftAsNumber = leftRef != null ? leftRef.ThisReference as Primitive.Number : null;
					bool rightIsNumber = rightAsNumber != null;
					bool leftIsNumber = leftAsNumber != null;

					if (rightIsNumber || leftIsNumber) {
						int lookForNegativeAt = highestIndex - 1;

						bool negative = false;
						int leftNum = 0;
						int rightNum = 0;

						// look for number on left and right
						if (leftAsNumber != null) {
							lookForNegativeAt--;
							leftNum = (int)leftAsNumber.Value;
						}
						if (rightAsNumber != null)
							rightNum = (int)rightAsNumber.Value;

						if (lookForNegativeAt > 0 &&
							remaining[lookForNegativeAt] is Token.Operator op &&
							op.StringValue == "-")
							negative = true;

						double fractionalPart = rightNum * Math.Pow(10, Math.Floor(Math.Log10(rightNum)) - 1);
						double realValue = (negative ? -1 : 1) * (leftNum + fractionalPart);

						// replace region start & end inclusive
						int replaceStart = highestIndex - (leftAsNumber != null ? 1 : 0) - (negative ? 1 : 0);
						int replaceEnd = highestIndex + (rightAsNumber != null ? 1 : 0); // +1 if right is number

						HF.ReplaceRange(remaining, replaceStart, replaceEnd, // replace those tokens with the value
							new() { Token.Reference.ExistingGlobalReference("", // turn value into ref
							new Primitive.Number(realValue)) }); // actual value
					}
					else if (right is Token.Name) { // normal member syntax, expect existing reference on left

					}
					else
						return Errors.InvalidUseOfOperator(".");

						break;
			}

			if (last.SequenceEqual(remaining))
				break;

			last = new(remaining);

			if (highestIndex == -1)
				break;
		}

		return Data.Success;
	}
}
