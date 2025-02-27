using System;
using System.Collections.Generic;
using System.Linq;

public class Evaluator : Part {
	public CableConnection InterpreterCC;

	public Interpreter GetInterpreter()
		=> InterpreterCC.Cable.OtherCC(InterpreterCC).Part as Interpreter;

	public Data TryGetInterpreter(out Interpreter interpreter) {
		interpreter = GetInterpreter();

		return interpreter == null ?
			Errors.MissingOrInvalidConnection("Evaluator", "Interpreter") :
			Data.Success;
	}

	public Data Compare(Data a, Data b, Token.Operator op, Memory memory) {
		// not gona add comparison operator check bc whatever's calling this should already have done it
		Interpreter interpreter = GetInterpreter();
		if (interpreter == null) return Errors.MissingOrInvalidConnection("Interpreter", "Evaluator");

		Data lt = LessThan(a, b, memory, interpreter); // is bool checks done in the methods already
		if (lt is Error) return lt;

		Data eq = Equals(a, b, memory, interpreter);
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
		Name,
		DotOperator
	}
	public Data Evaluate(int flags, Line line) {

		List<Token> remaining = line.Tokens;
		List<Token> last = new();

		while (!(remaining.Count == 1 || remaining.SequenceEqual(last))) { // main loop
			// find leftmost and highest precedence
			Actions action = Actions.None;

			int highestPrecedence = -1;
			int highestIndex = -1;

			for (int i = 0; i < remaining.Count; i++) { 
				Token token = remaining[i];

				int precedence = -1;
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
				case Actions.Name:
					// try get memory
					Data tryget = TryGetInterpreter(out Interpreter interpreter);
					if (tryget is Error) return tryget;
					tryget = interpreter.TryGetMemory(out Memory memory);
					if (tryget is Error) return tryget;

					// check memory for name 
					string name = (highestToken as Token.Name).Value;
					Data get = memory.Get(name);

					// replace name token with reference token
					remaining[highestIndex] = (get is not Error) ? 
						Token.Reference.ExistingGlobalReference(name, get) :
						Token.Reference.NewGlobalReference(name);
					break;
				case Actions.DotOperator:
					// check left
					
					break;
			}


			if (highestIndex == -1)
				break;
		}

		return Data.Success;
	}
}
