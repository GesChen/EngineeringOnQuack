using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Evaluator : MonoBehaviour {
	public Interpreter Interpreter;

	public Data Compare(Data a, Data b, Token.Operator op, Memory memory) {
		// not gona add comparison operator check bc whatever's calling this should already have done it

		Data lt = null;
		bool lessthan = false;
		if (!(op.Value == Token.Operator.Ops.Equality || 
			op.Value == Token.Operator.Ops.NotEquals)) {
			lt = LessThan(a, b, memory, Interpreter); // is bool checks done in the methods already
			if (lt is Error) return lt;
		
			lessthan = (lt as Primitive.Bool).Value;
		}

		Data eq = null;
		bool equals = false;
		if (!(op.Value == Token.Operator.Ops.LessThan ||
			op.Value == Token.Operator.Ops.GreaterThanOrEqualTo)) {
			eq = Equals(a, b, memory, Interpreter);
			if (eq is Error) return eq;
		
			equals = (eq as Primitive.Bool).Value;
		}

		return op.Value switch {
			Token.Operator.Ops.Equality				=> eq,
			Token.Operator.Ops.NotEquals			=> new Primitive.Bool(!equals), // (!=) = (! ==)
			Token.Operator.Ops.LessThan				=> lt,
			Token.Operator.Ops.GreaterThan			=> new Primitive.Bool(!(lessthan || equals)), // (>) = (!<=) 
			Token.Operator.Ops.LessThanOrEqualTo	=> new Primitive.Bool(lessthan || equals), // (<=) = (< || ==)
			Token.Operator.Ops.GreaterThanOrEqualTo	=> new Primitive.Bool(!lessthan), // (>=) = (! <)
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
		Arithmetic,
		Comparison,
		Logical
	}

	struct ActionContext {
		public int				flags;
		public Line				line;
		public Memory			memory;
		public List<Token>		remaining;
		public Token			highestToken;
		public int				highestIndex;
		public Token			left;
		public Token			right;
		public Token.Reference	leftRef;
		public bool				leftIsRefAndExists;
		public Token.Reference	rightRef;
		public bool				rightIsRefAndExists;
	}

	public Data Evaluate(int flags, Line line, Memory memory) {
		if (line.Tokens.Count == 0)
			return Errors.CannotEvaluateEmpty();

		List<Token> remaining = line.Tokens;
		List<Token> last = new();
		bool atLast = false;
		while (!atLast || remaining.Count > 1) { // main loop
			if (remaining.Count <= 1) atLast = true; // ensures last single token can be operated on if needed

			GetHighestPrecedenceAction(
				remaining,
				out Actions highestAction,
				out int highestPrecedence,
				out int highestIndex
			);
			if (highestPrecedence == -1)
				break; // done with iterating

			Token highestToken = remaining[highestIndex];
			Token left					= remaining.ElementAtOrDefault(highestIndex - 1);
			Token.Reference leftRef		= left as Token.Reference;
			bool leftIsRefAndExists		= leftRef != null && leftRef.Exists;

			Token right					= remaining.ElementAtOrDefault(highestIndex + 1);
			Token.Reference rightRef	= right as Token.Reference;
			bool rightIsRefAndExists	= rightRef != null && rightRef.Exists;

			ActionContext localActionContext = new() {
				flags					= flags,
				line					= line,
				memory					= memory,
				remaining				= remaining,
				highestToken			= highestToken,
				highestIndex			= highestIndex,
				left					= left,
				right					= right,
				leftRef					= leftRef,
				leftIsRefAndExists		= leftIsRefAndExists,
				rightRef				= rightRef,
				rightIsRefAndExists		= rightIsRefAndExists
			};

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
						Token.Reference.ExistingGlobalReference(name, get) :	 // make existing if data exists
						Token.Reference.NewGlobalReference(name);				// or make new
					break;
				
				// decimal / member handling
				case Actions.DotOperator:
					Data tryHandleDotOperator = HandleDotOperator(localActionContext);
					if (tryHandleDotOperator is Error) return tryHandleDotOperator;
					break;

				// region operators
				case Actions.Region:
					Data tryHandleRegion = HandleRegion(localActionContext);
					if (tryHandleRegion is Error) return tryHandleRegion;
					break;

				// unary operators
				case Actions.Unary:
					Data tryHandleUnary = HandleUnary(localActionContext);
					if (tryHandleUnary is Error) return tryHandleUnary;
					break;

				// normal operators
				case Actions.Arithmetic:
					Data tryHandleArithmetic = HandleArithmetic(localActionContext);
					if (tryHandleArithmetic is Error) return tryHandleArithmetic;
					break;

				case Actions.Comparison:
					Data tryHandleComparison = HandleComparison(localActionContext);
					if (tryHandleComparison is Error) return tryHandleComparison;
					break;
				
				case Actions.Logical:
					Data tryHandleLogical = HandleLogical(localActionContext);
					if (tryHandleLogical is Error) return tryHandleLogical;
					break;

			}

			if (last.SequenceEqual(remaining)) break; // duplicate between iters = break
			last = new(remaining);
		}

		if (remaining.Count == 1 && remaining[0] is Token.Reference r)
			return r.ThisReference;

		return Data.Success;
	}

	private void GetHighestPrecedenceAction(
		List<Token> remaining,
		out Actions highestAction,
		out int highestPrecedence,
		out int highestIndex) {

		highestAction = Actions.None;
		highestPrecedence = -1;
		highestIndex = -1;

		for (int i = 0; i < remaining.Count; i++) {
			Token token = remaining[i];
			Actions action = Actions.None;
			int precedence = -1;

			bool isOp = token is Token.Operator;
			Token.Operator op = isOp ? token as Token.Operator : null;

			// D -> R
			if (token is Data) {
				precedence = 20;
				action = Actions.Data;
			}

			// N -> R
			if (token is Token.Name) {
				precedence = 15;
				action = Actions.Name;
			}

			// handle .
			else if (isOp && op.Value == Token.Operator.Ops.Dot) {
				precedence = 15;
				action = Actions.DotOperator;
			}

			// region operator
			else if (isOp &&
				(op.Value	== Token.Operator.Ops.OpenParentheses ||
				op.Value	== Token.Operator.Ops.OpenBracket ||
				op.Value	== Token.Operator.Ops.OpenBrace)) {
				precedence = 15;
				action = Actions.Region;
			}

			// unary operator
			else if (isOp && Token.Operator.UnaryOperatorsHashSet.Contains(op.Value) &&
				(i == 0 || remaining[i - 1] is Token.Operator or Token.Keyword)) {
				precedence = 14; // slightly less than others
				action = Actions.Unary;
			}

			// normal operators
			else if (isOp && Token.Operator.ArithmeticOperatorsHashSet.Contains(op.Value)) {
				precedence = Token.Operator.NormalOperatorsPrecedence[op.Value];
				action = Actions.Arithmetic;
			}
			else if (isOp && Token.Operator.ComparisonOperatorsHashSet.Contains(op.Value)) {
				precedence = Token.Operator.NormalOperatorsPrecedence[op.Value];
				action = Actions.Comparison;
			}
			else if (isOp && Token.Operator.LogicalOperatorsHashSet.Contains(op.Value)) {
				precedence = Token.Operator.NormalOperatorsPrecedence[op.Value];
				action = Actions.Logical;
			}

			if (precedence > highestPrecedence) { // > not >= for leftmost
				highestPrecedence = precedence;
				highestIndex = i;
				highestAction = action;
			}
		}
	}

	private Data HandleDotOperator(in ActionContext AC) {
		#region unpack AC
		List<Token> remaining = AC.remaining;
		int highestIndex = AC.highestIndex;
		Token left = AC.left;
		Token right = AC.right;
		Token.Reference leftRef = AC.leftRef;
		bool leftIsRefAndExists = AC.leftIsRefAndExists;
		Token.Reference rightRef = AC.rightRef;
		bool rightIsRefAndExists = AC.rightIsRefAndExists;
		#endregion

		void handleAsNumber() {
			Primitive.Number leftAsNumber = leftIsRefAndExists ?
				leftRef.ThisReference as Primitive.Number : null;
			Primitive.Number rightAsNumber = rightIsRefAndExists ?
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
		if (leftIsRefAndExists) {
			if (right is Token.Name rightname) {
				Data tryget = leftRef.GetData();
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
			else if (leftRef.ThisReference is Primitive.Number)
				handleAsNumber();
			else
				return Errors.InvalidUseOfOperator(".");
		}
		else { // left is null or anything else
			if (right is Primitive.Number) // handle as number if right is number
				handleAsNumber();
			else
				return Errors.InvalidUseOfOperator(".");
		}

		return Data.Success;
	}

	private Data HandleRegion(in ActionContext AC) {
		#region unpack AC
		int flags					= AC.flags					;
		Line line					= AC.line					;
		Memory memory				= AC.memory					;
		List<Token> remaining		= AC.remaining				;
		Token highestToken			= AC.highestToken			;
		int highestIndex			= AC.highestIndex			;
		Token left					= AC.left					;
		Token right					= AC.right					;
		Token.Reference leftRef		= AC.leftRef				;
		bool leftIsRefAndExists		= AC.leftIsRefAndExists		;
		Token.Reference rightRef	= AC.rightRef				;
		bool rightIsRefAndExists	= AC.rightIsRefAndExists	;
		#endregion

		Token.Operator highestTokenAsOp = highestToken as Token.Operator; // it should be operator plz....

		Token.Operator.Ops pairing = highestTokenAsOp.Value switch {
			Token.Operator.Ops.OpenParentheses => Token.Operator.Ops.CloseParentheses,
			Token.Operator.Ops.OpenBracket => Token.Operator.Ops.CloseBracket,
			Token.Operator.Ops.OpenBrace => Token.Operator.Ops.CloseBrace,
			_ => Token.Operator.Ops.None
		};
		if (pairing == Token.Operator.Ops.None) return Errors.CouldntParse(line.OriginalString);
		
		int i = highestIndex;
		int depth = 0;
		while (i < remaining.Count) { // find index of matching
			if (remaining[i] is Token.Operator op) {
				if (op.Value == pairing) {
					depth--;
					if (depth == 0) break;
				}
				else if (op.Value == highestTokenAsOp.Value)
					depth++;
			}
			i++;
		}
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
				bool isArguments = leftIsRefAndExists;
				if (isArguments) {
					// make sure left is a callable type
					if (!(left == null || leftRef.Exists))
						return Errors.UnknownVariable(leftRef.Name);
					if (leftRef.ThisReference is not Primitive.Function func)
						return Errors.MemberIsNotMethod(leftRef.Name, leftRef.ThisReference.Type.Name);

					Data evalArgs = EvaluateList(
						flags,
						line.CopyWithNewTokens(regionTokens),
						memory);
					if (evalArgs is Error) return evalArgs;

					List<Data> args = (evalArgs as Primitive.List).Value;

					Data run = Interpreter.RunFunction(memory, func, leftRef.ParentReference, args);
					if (run is Error) return run;

					HF.ReplaceRange(remaining, highestIndex - 1, pairIndex, new() { run });
				}
				else {
					Data evalSubexp = Evaluate(0, line.CopyWithNewTokens(regionTokens), memory);
					if (evalSubexp is Error) return evalSubexp;

					HF.ReplaceRange(remaining, highestIndex, pairIndex, new() { evalSubexp });
				}
				break;

			case "[":
				bool indexing = leftIsRefAndExists;
				if (indexing) {
					Primitive.List leftAsList = leftRef.ThisReference as Primitive.List;
					Primitive.String leftAsString = leftRef.ThisReference as Primitive.String;
					Primitive.Dict leftAsDict = leftRef.ThisReference as Primitive.Dict;

					if (leftAsList == null && leftAsString == null && leftAsDict == null)
						return Errors.CannotIndex(leftRef.ThisReference.Type.Name);

					List<Data> baseList = leftAsList?.Value;
					if (leftAsString != null)
						baseList = Enumerable.Repeat<Data>(null, leftAsString.Value.Length).ToList(); // turn string into representative list

					Data evalList = EvaluateList(
						flags,
						line.CopyWithNewTokens(regionTokens),
						memory,
						baseList
						);
					if (evalList is Error) return evalList;

					if (leftAsList != null || leftAsString != null) { // indexing
																	  // check indices
						List<int> indices = new();
						foreach (Data d in (evalList as Primitive.List).Value) {
							if (d is not Primitive.Number num)
								return Errors.CannotIndexWithType(d.Type.Name);

							int val = (int)num.Value;

							if (num.Value != val)
								return Errors.CannotIndexWithType("non whole number");

							if (val >= baseList.Count ||
								val < -baseList.Count)
								return Errors.IndexOutOfRange(val);

							indices.Add((int)num.Value);
						}

						if (leftAsList != null) { // left is list
							List<Data> indexed = new();
							foreach (int val in indices)
								indexed.Add(leftAsList.Value[val >= 0 ? val : leftAsList.Value.Count - val]);

							HF.ReplaceRange(remaining, highestIndex - 1, pairIndex,
								new() { new Primitive.List(indexed) });
						}
						else { // left is string
							StringBuilder sb = new();
							foreach (int val in indices)
								sb.Append(leftAsString.Value[val >= 0 ? val : leftAsList.Value.Count - val]);

							HF.ReplaceRange(remaining, highestIndex - 1, pairIndex,
								new() { new Primitive.String(sb.ToString()) });
						}
					}
					else { // dict key get
						List<Data> values = new();
						foreach (Data key in (evalList as Primitive.List).Value) {
							Data trygetvalue = Interpreter.RunFunction(
								memory,
								new Primitive.Function(Primitive.Dict.get),
								leftAsDict,
								new() { key });
							if (trygetvalue is Error) return trygetvalue;

							values.Add(trygetvalue);
						}


						HF.ReplaceRange(remaining, highestIndex - 1, pairIndex,
							new() { new Primitive.List(values) });
					}
				}
				else { // normal list
					Data evalList = EvaluateList(flags, line.CopyWithNewTokens(regionTokens), memory);
					if (evalList is Error) return evalList;

					HF.ReplaceRange(remaining, highestIndex, pairIndex, new() { evalList });
				}
				break;

			case "{":
				Data evalDict = EvaluateDict(flags, line.CopyWithNewTokens(regionTokens), memory);
				if (evalDict is Error) return evalDict;

				HF.ReplaceRange(remaining, highestIndex, pairIndex, new() { evalDict });
				break;
		}

		return Data.Success;
	}

	private Data HandleUnary(in ActionContext AC) {
		#region unpack AC
		List<Token> remaining = AC.remaining;
		Token highestToken = AC.highestToken;
		int highestIndex = AC.highestIndex;
		Token.Reference rightRef = AC.rightRef;
		bool rightIsRefAndExists = AC.rightIsRefAndExists;
		#endregion

		Token.Operator thisOp = (highestToken as Token.Operator);

		if (!rightIsRefAndExists)
			return Errors.InvalidUseOfOperator(thisOp.StringValue);

		if (thisOp.Value == Token.Operator.Ops.Plus ||
			thisOp.Value == Token.Operator.Ops.Minus) { // + or -

			Data castToNumber = rightRef.ThisReference.Cast(Primitive.Number.InternalType);
			if (castToNumber is Error) return castToNumber;

			double value = (castToNumber as Primitive.Number).Value;
			// use a copy of the number i guess
			HF.ReplaceRange(remaining, highestIndex, highestIndex + 1,
				new() { new Primitive.Number(value * (thisOp.Value == Token.Operator.Ops.Minus ? -1 : 1)) });
		}
		else { // !
			Data castToBool = rightRef.ThisReference.Cast(Primitive.Bool.InternalType);
			if (castToBool is Error) return castToBool;

			bool value = (castToBool as Primitive.Bool).Value;

			// use a copy of the bool
			HF.ReplaceRange(remaining, highestIndex, highestIndex + 1,
				new() { new Primitive.Bool(!value) });
		}

		return Data.Success;
	}

	private Data OperatorCheck(
		in ActionContext AC,
		in Token.Operator op,
		out Data leftData, out Data rightData) {
		#region unpack AC
		Token.Reference leftRef = AC.leftRef;
		bool leftIsRefAndExists = AC.leftIsRefAndExists;
		Token.Reference rightRef = AC.rightRef;
		bool rightIsRefAndExists = AC.rightIsRefAndExists;
		#endregion
		
		leftData = null;
		rightData = null;

		// check left and right
		if (leftRef == null)
			return Errors.Expected("expression", "left of " + op.StringValue);
		if (rightRef == null)
			return Errors.Expected("expression", "right of " + op.StringValue);

		if (!leftIsRefAndExists)
			return Errors.UnknownVariable(leftRef.Name);
		if (!rightIsRefAndExists)
			return Errors.UnknownVariable(rightRef.Name);

		leftData = leftRef.ThisReference;
		rightData = rightRef.ThisReference;
		return Data.Success;
	}

	private Data HandleArithmetic(in ActionContext AC) {
		#region unpack AC
		Memory memory = AC.memory;
		List<Token> remaining = AC.remaining;
		Token highestToken = AC.highestToken;
		int highestIndex = AC.highestIndex;
		Token.Reference leftRef = AC.leftRef;
		Token.Reference rightRef = AC.rightRef;
		#endregion

		// assume precedence is sorted out already

		Token.Operator op = highestToken as Token.Operator;

		Data check = OperatorCheck(AC, op, out Data leftData, out Data rightData);
		if (check is Error) return check;

		// cast left to right
		leftData = leftData.Cast(rightRef.ThisReference.Type);
		if (leftData is Error) return leftData;

		Dictionary<Token.Operator.Ops, string> opNames = new() {
			{ Token.Operator.Ops.Plus,		"ad" },
			{ Token.Operator.Ops.Minus,		"su" },
			{ Token.Operator.Ops.Multiply,	"mu" },
			{ Token.Operator.Ops.Divide,	"di" },
			{ Token.Operator.Ops.Mod,		"mo" },
			{ Token.Operator.Ops.Power,		"po" } 
		};

		string operationName = opNames[op.Value];
		Data tryGetLeftMember = leftData.GetMember(operationName);
		if (tryGetLeftMember is Error)
			return Errors.UnsupportedOperation(
				op.StringValue, 
				leftRef.ThisReference.Type.Name, 
				rightRef.ThisReference.Type.Name);

		Data runFunction = Interpreter.RunFunction(
			memory, 
			tryGetLeftMember as Primitive.Function, 
			leftData, 
			new() { rightData }
		);
		if (runFunction is Error) return runFunction;

		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, new() { runFunction });

		return Data.Success;
	}
	
	private Data HandleComparison(ActionContext AC) {
		#region unpack AC
		Memory memory = AC.memory;
		List<Token> remaining = AC.remaining;
		Token highestToken = AC.highestToken;
		int highestIndex = AC.highestIndex;
		Token.Reference leftRef = AC.leftRef;
		bool leftIsRefAndExists = AC.leftIsRefAndExists;
		Token.Reference rightRef = AC.rightRef;
		bool rightIsRefAndExists = AC.rightIsRefAndExists;
		#endregion

		// assume precedence is sorted out already

		Token.Operator op = highestToken as Token.Operator;

		Data check = OperatorCheck(AC, op, out Data leftData, out Data rightData);
		if (check is Error) return check;

		Data compare = Compare(leftData, rightData, op, memory);
		if (compare is Error) return compare;

		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, new() { compare });

		return Data.Success;
	}

	private Data HandleLogical(ActionContext AC) {
		#region unpack AC
		List<Token> remaining = AC.remaining;
		Token highestToken = AC.highestToken;
		int highestIndex = AC.highestIndex;
		#endregion

		// assume precedence is sorted out already

		Token.Operator op = highestToken as Token.Operator;

		Data check = OperatorCheck(AC, op, out Data leftData, out Data rightData);
		if (check is Error) return check;

		// try cast both to bool
		Data trycastLeft = leftData.Cast(Primitive.Bool.InternalType);
		Data trycastRight = rightData.Cast(Primitive.Bool.InternalType);
		if (trycastLeft is Error) return trycastLeft;
		if (trycastRight is Error) return trycastRight;

		bool a = (trycastLeft as Primitive.Bool).Value;
		bool b = (trycastRight as Primitive.Bool).Value;

		bool result = op.Value switch {
			Token.Operator.Ops.And	=> a && b,
			Token.Operator.Ops.Or	=> a || b,
			Token.Operator.Ops.Nand	=> !(a && b),
			Token.Operator.Ops.Nor	=> !(a || b),
			Token.Operator.Ops.Xor	=> a != b,
		};

		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, 
			new() { new Primitive.Bool(result) });

		return Data.Success;
	}

	private Data EvaluateList(int flags, Line line, Memory memory, List<Data> baseList = null) {
		List<Token> tokens = line.Tokens;
		
		// identify list type
		bool rangeList = false;

		int eCount = 0;
		int ellipsisIndex = -1;
		for (int i = 0; i < tokens.Count; i++) {
			if (tokens[i] is Token.Operator op && op.Value == Token.Operator.Ops.Ellipsis) {
				ellipsisIndex = i;
				eCount++;
			}
		}
		if (eCount > 1) // either 0 or 1 ...s
			return Errors.InvalidUseOfOperator("...");
		else if (eCount > 0) // this is range
			rangeList = true;


		if (rangeList) {
			Token[] leftOfEllipsis = tokens.ToArray()[..ellipsisIndex]; // exclusive
			Token[] rightOfEllipsis = tokens.ToArray()[(ellipsisIndex + 1)..]; // inclusive 

			double start = 0;
			bool stepDefined = false;
			double step = 0;
			bool endDefined = false;
			double end = 0;

			if (baseList != null) { // ternaries hard to understand so using if instead
				endDefined = true;
				end = baseList.Count;
			}

			if (leftOfEllipsis.Length != 0) {
				Data leftOfEllipsisEval = EvaluateList(
								flags, 
								line.CopyWithNewTokens(leftOfEllipsis.ToList()), 
								memory);
				if (leftOfEllipsisEval is Error) return leftOfEllipsisEval;
				
				Primitive.List leftList = leftOfEllipsisEval as Primitive.List;
				
				if (leftList.Value[0] is not Primitive.Number leftNum)
					return Errors.CannotUseTypeWithFeature(leftList.Value[0].Type.Name, "range lists");

				start = leftNum.Value;

				if (leftList.Value.Count > 1) {
					if (leftList.Value[1] is not Primitive.Number stepNum)
						return Errors.CannotUseTypeWithFeature(leftList.Value[1].Type.Name, "range lists");

					stepDefined = true;
					step = stepNum.Value;
				}
				if (leftList.Value.Count > 2)
					return Errors.BadSyntaxFor("range lists", "too many start parameters");
			}

			if (rightOfEllipsis.Length != 0) {
				Data rightOfEllipsisEval = Evaluate(
					flags,
					line.CopyWithNewTokens(rightOfEllipsis.ToList()),
					memory);

				if (rightOfEllipsisEval is Error) return rightOfEllipsisEval;

				if (rightOfEllipsisEval is not Primitive.Number rightNum)
					return Errors.CannotUseTypeWithFeature(rightOfEllipsisEval.Type.Name, "range lists");

				endDefined = true;
				end = rightNum.Value;
			}

			if (!stepDefined)
				step = 1;

			step = (start < end ? 1 : -1) * Math.Abs(step); // force step to work

			if (!endDefined)
				return Errors.BadSyntaxFor("range lists", "missing end parameter");

			List<Data> list = new();

			for (double i = start; start < end ? (i < end) : (i > end); i += step) {
				list.Add(new Primitive.Number(i));
			}

			return new Primitive.List(list);
		}
		else { // normal list
			// split into chunks by comma
			List<Token> curChunk = new();
			List<List<Token>> tokenChunks = new();
			int i = 0;
			while (i < tokens.Count) {
				Token rt = tokens[i];
				if (rt is Token.Operator op && op.Value == Token.Operator.Ops.Comma) {
					tokenChunks.Add(curChunk);
					curChunk = new(); // instead of clearing so the reference isnt shared
				}
				else
					curChunk.Add(rt);
				i++;
			}
			if (curChunk.Count > 0) 
				tokenChunks.Add(curChunk);

			// eval arg token chunks into data
			List<Data> items = new(); // can be optimized into array if desperate
			foreach (List<Token> chunk in tokenChunks) {
				Data tryEval = Evaluate(0, line.CopyWithNewTokens(chunk), memory);
				if (tryEval is Error) return tryEval;

				items.Add(tryEval);
			}

			return new Primitive.List(items);
		}
	}

	private Data EvaluateDict(int flags, Line line, Memory memory) {
		List<Token> tokens = line.Tokens;

		// stole from evallist lol

		// split into chunks by comma
		List<Token> curChunk = new();
		List<List<Token>> tokenChunks = new();
		int i = 0;
		while (i < tokens.Count) {
			Token rt = tokens[i];
			if (rt is Token.Operator op && op.Value == Token.Operator.Ops.Comma) {
				tokenChunks.Add(curChunk);
				curChunk = new(); // instead of clearing so the reference isnt shared
			}
			else
				curChunk.Add(rt);
			i++;
		}
		if (curChunk.Count > 0)
			tokenChunks.Add(curChunk);

		/* structure: 
		 * outermost list - all kvp groups
		 * 2nd layer key value pair (groups) (only 2 in this layer)
		 * groups of tokens making up key and value
		 */

		List<Token[][]> kvpGroups = new();
		foreach (List<Token> group in tokenChunks) {
			// split by the : operator
			int colonCount = 0;
			int colonIndex = -1;
			for (int ci = 0; ci < group.Count; ci++) {
				if (group[ci] is Token.Operator co && co.Value == Token.Operator.Ops.Colon) {
					colonCount++;
					colonIndex = ci;
				}
			}
			if (colonCount == 0) return Errors.BadSyntaxFor("dictionary", "missing colon");
			if (colonCount > 1) return Errors.BadSyntaxFor("dictionary", "too many colons");

			Token[] key = group.ToArray()[..colonIndex];
			Token[] value = group.ToArray()[(colonIndex + 1)..];

			if (key.Length == 0) return Errors.BadSyntaxFor("dictionary", "missing key");
			if (value.Length == 0) return Errors.BadSyntaxFor("dictionary", "missing value");

			kvpGroups.Add(new[] { key, value });
		}

		Dictionary<Data, Data> newDict = new();
		foreach (Token[][] kvpTokenGroup in kvpGroups) {
			Data evalKey = Evaluate(flags, 
				line.CopyWithNewTokens(kvpTokenGroup[0].ToList()), 
				memory);
			if (evalKey is Error) return evalKey;

			Data evalValue = Evaluate(flags,
				line.CopyWithNewTokens(kvpTokenGroup[1].ToList()),
				memory);
			if (evalValue is Error) return evalValue;

			newDict[evalKey] = evalValue;
		}

		return new Primitive.Dict(newDict);
	}
}