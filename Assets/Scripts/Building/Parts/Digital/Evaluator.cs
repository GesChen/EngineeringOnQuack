using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;

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

		return op.StringValue switch {
			"==" => eq,
			"!=" => new Primitive.Bool(!equals), // (!=) = (! ==)
			"<" => lt,
			">" => new Primitive.Bool(!(lessthan || equals)), // (>) = (!<=) 
			"<=" => new Primitive.Bool(lessthan || equals), // (<=) = (< || ==)
			">=" => new Primitive.Bool(!lessthan), // (>=) = (! <)
			_ => Errors.CannotCompare(a.Type.Name, b.Type.Name),
		};
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
		DotOperator,
		Region,
		Unary,
	}

	public Data Evaluate(int flags, Line line, Memory memory) {

		List<Token> remaining = line.Tokens;
		List<Token> last = new();

		while (remaining.Count > 1) { // main loop

			// find leftmost and highest precedence

			Actions highestAction = Actions.None;
			int highestPrecedence = -1;
			int highestIndex = -1;

			for (int i = 0; i < remaining.Count; i++) {
				Token token = remaining[i];

				Actions action = Actions.None;
				int precedence = -1;

				bool isOp = token is Token.Operator;
				Token.Operator op = isOp ? token as Token.Operator : null;

				// D -> R
				if (token is Data) { 
					precedence = 15;
					action = Actions.Data;
				}

				// N -> R
				if (token is Token.Name) { 
					precedence = 10;
					action = Actions.Name;
				}

				// handle .
				else if (isOp && op.StringValue == ".") { 
					precedence = 10;
					action = Actions.DotOperator;
				}

				// region operator
				else if (isOp &&
					(op.StringValue == "(" ||
					op.StringValue == "[" ||
					op.StringValue == "{")) { 
					precedence = 10;
					action = Actions.Region;
				}

				// unary operator
				else if (isOp &&
					(op.StringValue == "+" || op.StringValue == "-" || op.StringValue == "!") &&
					(i == 0 || remaining[i - 1] is Token.Operator || remaining[i - 1] is Token.Keyword)) {
					precedence = 9; // slightly less than others
					action = Actions.Unary;
				}

				if (precedence > highestPrecedence) { // > not >= for leftmost
					highestPrecedence = precedence;
					highestIndex = i;
					highestAction = action;
				}
			}
			if (highestPrecedence == -1)
				break; // done with iterating
			Token highestToken = remaining[highestIndex];

			Token left = remaining.ElementAtOrDefault(highestIndex - 1);
			Token right = remaining.ElementAtOrDefault(highestIndex + 1);

			Token.Reference leftRef = left as Token.Reference;
			bool leftIsRef = leftRef != null;
			Token.Reference rightRef = left as Token.Reference;
			bool rightIsRef = leftRef != null;

			Data tryget;
			switch (highestAction) {
				
				// D -> R
				case Actions.Data:
					Data data = remaining[highestIndex] as Data;
					remaining[highestIndex] = Token.Reference.ExistingGlobalReference("", data);
					break;
				
				// N -> R 
				case Actions.Name:
					// check memory for name 
					string name = (highestToken as Token.Name).Value;
					Data get = memory.Get(name);

					// replace name token with reference token
					remaining[highestIndex] = (get is not Error) ?
						Token.Reference.ExistingGlobalReference(name, get) : // make existing if data exists
						Token.Reference.NewGlobalReference(name);           // or make new
					break;
				
				// decimal / member handling
				case Actions.DotOperator:
					void handleAsNumber() {
						Primitive.Number leftAsNumber = leftIsRef ?
							leftRef.ThisReference as Primitive.Number : null;
						Primitive.Number rightAsNumber = rightIsRef ?
							rightRef.ThisReference as Primitive.Number : null;

						int leftNum = leftAsNumber != null ? (int)leftAsNumber.Value : 0;
						int rightNum = rightAsNumber != null ? (int)rightAsNumber.Value : 0;

						int replaceStart = highestIndex - (leftAsNumber != null ? 1 : 0);
						int replaceEnd = highestIndex + (rightAsNumber != null ? 1 : 0);

						double fractionalPart = rightNum * Math.Pow(10, Math.Floor(Math.Log10(rightNum)) - 1);
						double realValue = leftNum + fractionalPart;

						HF.ReplaceRange(remaining, replaceStart, replaceEnd, // replace those tokens with the value
							new() { Token.Reference.ExistingGlobalReference("", // turn value into ref
							new Primitive.Number(realValue)) }); // actual value
					}

					// normal member syntax, expect existing reference on left
					if (leftIsRef) {
						if (left is Primitive.Number)
							handleAsNumber();
						if (right is Token.Name rightname) {
							tryget = leftRef.GetData();
							if (tryget is Error) return tryget;

							tryget = tryget.GetMember(rightname.Value);
							HF.ReplaceRange( // replace l . r with R
								remaining,
								highestIndex - 1,
								highestIndex + 1,
								new() { Token.Reference.ExistingMemberReference(
									leftRef,
									tryget,
									rightname.Value)});
						}
						else
							return Errors.InvalidUseOfOperator(".");
					}
					else { // left is null or anything else
						if (right is Primitive.Number) // handle as number if right is number
							handleAsNumber();
						else
							return Errors.InvalidUseOfOperator(".");
					}

					break;

				// region operators
				case Actions.Region:
					Token.Operator highestTokenAsOp = highestToken as Token.Operator; // it should be operator plz....

					string pairing = highestTokenAsOp.StringValue switch {
						"(" => ")",
						"[" => "]",
						"{" => "}",
						_ => ""
					};
					if (pairing == "") return Errors.CouldntParse(line.OriginalString);
					int i = highestIndex;
					while (i < remaining.Count && // find index of matching
						!(remaining[i] is Token.Operator op && op.StringValue == pairing)) i++;
					int pairIndex = i;

					if (pairIndex == remaining.Count) return Errors.MismatchedSomething(
						highestTokenAsOp.StringValue switch {
							"(" => "parentheses",
							"[" => "brackets",
							"{" => "braces",
							_ => "unknown"
						});

					List<Token> regionTokens = remaining.GetRange(highestIndex + 1, pairIndex - highestIndex - 1);
					
					switch (highestTokenAsOp.StringValue) {
						case "(":
							bool isArguments = leftIsRef;
							if (isArguments) {
								// make sure left is a callable type
								if (!leftRef.Exists)
									return Errors.UnknownVariable(leftRef.Name);
								if (leftRef.ThisReference is not Primitive.Function func)
									return Errors.MemberIsNotMethod(leftRef.Name, leftRef.ThisReference.Type.Name);
								
								// split arg list into chunks delimited by comma
								List<Token> curChunk = new();
								List<List<Token>> argChunks = new();
								i = 0;
								while (i < regionTokens.Count) {
									Token rt = regionTokens[i];
									if (rt is Token.Operator op && op.StringValue == ",") {
										argChunks.Add(curChunk);
										curChunk.Clear();
									}
									else
										curChunk.Add(rt);
									i++;
								}
								argChunks.Add(curChunk);

								// eval arg token chunks into data
								List<Data> args = new(); // can be optimized into array if desperate
								foreach (List<Token> chunk in argChunks) {
									Data tryEval = Evaluate(0,
										new(line.RealLineNumber,
										line.OriginalString,
										chunk), memory);

									if (tryEval is Error) return tryEval;

									args.Add(tryEval);
								}

								Data run = Interpreter.RunFunction(memory, func, leftRef.ParentReference, args);
								if (run is Error) return run;

								HF.ReplaceRange(remaining, highestIndex - 1, pairIndex, new() { run });
							}
							else {
								Data evalSubexp = Evaluate(0,
									new(
										line.RealLineNumber,
										line.OriginalString,
										regionTokens),
									memory);

								if (evalSubexp is Error) return evalSubexp;

								HF.ReplaceRange(remaining, highestIndex, pairIndex, new() { evalSubexp });
							}
							break;

						case "[":
							// eval the list
							Data evalList = EvaluateList()
							
							bool indexing = leftIsRef;
							if (indexing) {

							}
							else {

							}
							break;

						case "{":

							break;
					}

					break;

				// unary operators
				case Actions.Unary:

					break;


			}

			if (last.SequenceEqual(remaining))
				break;

			last = new(remaining);

			if (highestIndex == -1)
				break;
		}

		if (remaining.Count == 1 && remaining[0] is Token.Reference r)
			return r.ThisReference;

		return Data.Success;
	}

	private Data EvaluateList(int flags, List<Token> tokens, Memory memory) {
		// identify list type
		bool rangeList = false;

		int eCount = 0;
		int ellipsisIndex = -1;
		for (int i = 0; i < tokens.Count; i++) {
			if (tokens[i] is Token.Operator op && op.StringValue == "..") {
				ellipsisIndex = i;
				eCount++;
			}
		}
		if (eCount > 1) // either 0 or 1 ..s
			return Errors.InvalidUseOfOperator("..");
		else if (eCount > 0) // this is range
			rangeList = true;


		if (rangeList) {
			Token[] leftOfEllipsis = tokens.ToArray()[..ellipsisIndex]; // TODO figure out if this syntax is right
			Token[] rightOfEllipsies = tokens.ToArray()[ellipsisIndex..];


		}
		else {
		}
	}
}
