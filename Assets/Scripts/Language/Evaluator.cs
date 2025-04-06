using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static Token;

public class Evaluator {
	public Interpreter Interpreter;

	public Data Compare(Data a, Data b, Operator op, Memory memory) {
		// not gona add comparison operator check bc whatever's calling this should already have done it

		Data lt = null;
		bool lessthan = false;
		if (!(op.Value == Operator.Ops.Equality || 
			op.Value == Operator.Ops.NotEquals)) {
			lt = LessThan(a, b, memory, Interpreter); // is bool checks done in the methods already
			if (lt is Error) return lt;
		
			lessthan = (lt as Primitive.Bool).Value;
		}

		Data eq = null;
		bool equals = false;
		if (!(op.Value == Operator.Ops.LessThan ||
			op.Value == Operator.Ops.GreaterThanOrEqualTo)) {
			eq = Equals(a, b, memory, Interpreter);
			if (eq is Error) return eq;
		
			equals = (eq as Primitive.Bool).Value;
		}

		return op.Value switch {
			Operator.Ops.Equality				=> eq,
			Operator.Ops.NotEquals				=> new Primitive.Bool(!equals), // (!=) = (! ==)
			Operator.Ops.LessThan				=> lt,
			Operator.Ops.GreaterThan			=> new Primitive.Bool(!(lessthan || equals)), // (>) = (!<=) 
			Operator.Ops.LessThanOrEqualTo		=> new Primitive.Bool(lessthan || equals), // (<=) = (< || ==)
			Operator.Ops.GreaterThanOrEqualTo	=> new Primitive.Bool(!lessthan), // (>=) = (! <)
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
		FString,
		Data,
		Name,
		DotOperator,
		Region,
		Unary,
		Arithmetic,
		Comparison,
		Logical,
		Assignment
	}

	class ActionContext {
		public Line				line;
		public Memory			memory;
		public int				curRecursionDepth;
		public List<Token>		remaining;
		public Token			highestToken;
		public int				highestIndex;
		public Token			left;
		public Token			right;
		public Reference		leftRef;
		public bool				leftIsRefAndExists;
		public Reference		rightRef;
		public bool				rightIsRefAndExists;

		public ActionContext(
			Line line, 
			Memory memory, 
			int curRecursionDepth, 
			List<Token> remaining, 
			Token highestToken, 
			int highestIndex, 
			Token left, 
			Token right, 
			Reference leftRef, 
			bool leftIsRefAndExists, 
			Reference rightRef, 
			bool rightIsRefAndExists) {

			this.line = line;
			this.memory = memory;
			this.curRecursionDepth = curRecursionDepth;
			this.remaining = remaining;
			this.highestToken = highestToken;
			this.highestIndex = highestIndex;
			this.left = left;
			this.right = right;
			this.leftRef = leftRef;
			this.leftIsRefAndExists = leftIsRefAndExists;
			this.rightRef = rightRef;
			this.rightIsRefAndExists = rightIsRefAndExists;
		}
	}

	public Data Evaluate(Line line, Memory memory, bool makeCopy = true, int depth = 0) {

		if (makeCopy)
			line = line.DeepCopy();

		Data.currentUseMemory = memory;
		UpdateAllLineDataWithMemory(ref line, memory);
		Data tryEvaluate = EvaluateInternal(line, memory, depth);
		Data.currentUseMemory = null;

		return tryEvaluate;
	}

	private void UpdateAllLineDataWithMemory(ref Line line, Memory memory) {
		foreach (Token token in line.Tokens) {
			if (token is Data d)
				d.Memory = memory;
		}
	}

	private Data EvaluateInternal(Line line, Memory memory, int depth = 0) {
		if (line.Tokens.Count == 0) return Errors.CannotEvaluateEmpty();
		if (LanguageConfig.DEBUG) HF.WarnColor($"Evaluating {line.TokenList()}", Color.green);

		Data declChecks = DeclarationChecks(line.Tokens);
		if (declChecks is Error ||
			declChecks.Flags != Flags.None) // check passed
			return declChecks;

		List<Token> remaining = line.Tokens;
		List<Token> last = new();
		bool atLast = false;
		while (!atLast || remaining.Count > 1) { // main loop
			if (remaining.Count <= 1) atLast = true; // ensures last single token can be operated on if needed

			GetHighestPrecedenceAction(
				remaining,
				out Actions highestAction,
				out float highestPrecedence,
				out int highestIndex
			);
			if (highestPrecedence == -1)
				break; // done with iterating

			Token highestToken = remaining[highestIndex];
			Token left					= remaining.ElementAtOrDefault(highestIndex - 1);
			Reference leftRef			= left as Reference;
			bool leftIsRefAndExists		= leftRef != null && leftRef.Exists;

			Token right					= remaining.ElementAtOrDefault(highestIndex + 1);
			Reference rightRef			= right as Reference;
			bool rightIsRefAndExists	= rightRef != null && rightRef.Exists;

			ActionContext localActionContext = new(
				line,
				memory,
				depth,
				remaining,
				highestToken,
				highestIndex,
				left,
				right,
				leftRef,
				leftIsRefAndExists,
				rightRef,
				rightIsRefAndExists
			);

			switch (highestAction) {
				// handle f strings
				case Actions.FString:
					Data parseFString = ParseFString((remaining[highestIndex + 1] as Primitive.String).Value, localActionContext);
					HF.ReplaceRange(remaining, highestIndex, highestIndex + 1, 
						new() { Reference.ExistingGlobalReference(
							parseFString
					) });
					break;

				// D -> R
				case Actions.Data:
					Data data = highestToken as Data;
					remaining[highestIndex] = Reference.ExistingGlobalReference("", data);
					break;

				// N -> R 
				case Actions.Name:
					// check memory for name 
					string name = (highestToken as Name).Value;
					Data get = memory.Get(name);
					// dont return error again LMAO

					// replace name token with reference token
					remaining[highestIndex] = (get is not Error) ?
						Reference.ExistingGlobalReference(name, get) :   // make existing if data exists
						Reference.NewGlobalReference(name);             // or make new
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

				// assignment
				case Actions.Assignment:
					Data tryHandleAssignment = HandleAssignment(localActionContext);
					if (tryHandleAssignment is Error) return tryHandleAssignment;
					break;
			}

			if (last.SequenceEqual(remaining)) break; // duplicate between iters = break
			last = new(remaining);
		}

		if (remaining[0] is Keyword kw) {
			Data handleKeywords = HandleKeywords(ref remaining, kw);
			if (handleKeywords is Error) return handleKeywords;

			return handleKeywords;
		}

		// return data if thats what it collapses to
		if (remaining.Count == 1 && remaining[0] is Reference r) {
			if (!r.Exists)
				return Errors.UnknownName(r);

			return r.ThisReference;
		}

		return Data.Success;
	}

	private Data DeclarationChecks(List<Token> tokens) {
		// check for function, inline function, or class

		(int colonIndex, _) = FindAndCountOperator(tokens, Operator.Ops.Colon);
		if (tokens[0] is Name N && colonIndex != -1) { // some kind of declaration

			// determine if this is some kind of function
			(int oPIndex, int oPCount) = FindAndCountOperator(tokens, Operator.Ops.OpenParentheses);
			(int cPIndex, int cPCount) = FindAndCountOperator(tokens, Operator.Ops.CloseParentheses);

			bool maybeFunction = (oPIndex != -1) || (cPIndex != -1);

			if (maybeFunction) {
				if (oPIndex == -1 || cPIndex == -1 ||
					oPCount > 1 || cPCount > 1)
					return Errors.BadSyntaxFor("function declaration", "mismatched parentheses");

				// find all param names and store as string list
				List<string> paramNames = new();
				for (int i = oPIndex + 1; i < cPIndex; i++) {
					Token thisToken = tokens[i];

					if (thisToken is Operator thisOp) {
						if (thisOp.Value == Operator.Ops.Comma) {
							if (i > oPIndex + 1 && tokens[i - 1] is Operator) // 2 ops in a row
								return Errors.BadSyntaxFor("function declaration", "bad parameter list syntax");
							
							continue; // delimeter, ignore it
						}
						else // any op other than , 
							return Errors.BadSyntaxFor("function declaration", $"invalid operator {thisOp.StringValue} in parameters");
					}
					
					if (thisToken is Keyword)
						return Errors.BadSyntaxFor("function declaration", "cannot use keywords as parameters");

					if (thisToken is not Name thisName) // for good measure
						return Errors.BadSyntaxFor("function declaration");

					paramNames.Add(thisName.Value);
				}

				(int eqIndex, int eqCount) = FindAndCountOperator(tokens, Operator.Ops.Equals);
				
				if (eqIndex != -1) { // inline function 
					return new Primitive.List(new List<Data>() {	   // return list with function info
						new Primitive.String(N.Value),				  // name
						new Primitive.Number(cPIndex)				 // c paren index (rest is definition)
					}).SetFlags(Flags.MakeInline);					// tell interpreter to make inline func
				}

				// normal function
				return new Primitive.List(new List<Data>() {		  // return list with function info
					new Primitive.String(N.Value),					 // name
					new Primitive.List(paramNames					// turn arg names into list of strings
						.Select(n => new Primitive.String(n) as Data)
						.ToList())								  //
				}).SetFlags(Flags.MakeFunction);				 // tell interpreter to make function

			}
			else { // must be class

				// class syntax: <name> <:>
				if (tokens.Count != 2 ||
					colonIndex != 1)
					return Errors.BadSyntaxFor("class declaration");

				return new Primitive.String(N.Value) // return new class name
					.SetFlags(Flags.MakeClass);		// tell interpreter to make class
			}
		}
		return Data.Fail;
	}

	private void GetHighestPrecedenceAction(
		in  List<Token> remaining,
		out Actions highestAction,
		out float highestPrecedence,
		out int highestIndex) {

		highestAction = Actions.None;
		highestPrecedence = -1;
		highestIndex = -1;

		for (int i = 0; i < remaining.Count; i++) {
			Token token = remaining[i];
			Actions action = Actions.None;
			float precedence = -1;

			bool isOp = token is Operator;
			Operator op = isOp ? token as Operator : null;

			// f-strings
			if (token is Name N &&
				N.Value == "f" &&
				i != remaining.Count - 1 &&
				remaining[i + 1] is Data) {
				precedence = 25;
				action = Actions.FString;
			}

			// D -> R
			else if (token is Data) {
				precedence = 20;
				action = Actions.Data;
			}

			// N -> R
			else if (token is Name) {
				precedence = 15;
				action = Actions.Name;
			}

			// handle .
			else if (isOp && op.Value == Operator.Ops.Dot) {
				precedence = 15;
				action = Actions.DotOperator;
			}

			// region operator
			else if (isOp &&
				(op.Value	== Operator.Ops.OpenParentheses ||
				op.Value	== Operator.Ops.OpenBracket ||
				op.Value	== Operator.Ops.OpenBrace)) {
				precedence = 15;
				action = Actions.Region;
			}

			// unary operator
			else if (isOp && Operator.UnaryOperatorsHashSet.Contains(op.Value) &&
				(i == 0 || remaining[i - 1] is Operator or Keyword)) {
				precedence = 14; // slightly less than others
				action = Actions.Unary;
			}

			// normal operators
			else if (isOp && Operator.ArithmeticOperatorsHashSet.Contains(op.Value)) {
				precedence = Operator.NormalOperatorsPrecedence[op.Value];
				action = Actions.Arithmetic;
			}
			else if (isOp && Operator.ComparisonOperatorsHashSet.Contains(op.Value)) {
				precedence = Operator.NormalOperatorsPrecedence[op.Value];
				action = Actions.Comparison;
			}
			else if (isOp && Operator.LogicalOperatorsHashSet.Contains(op.Value)) {
				precedence = Operator.NormalOperatorsPrecedence[op.Value];
				action = Actions.Logical;
			}

			// assignment
			else if (isOp && Operator.AssignmentOperatorsHashSet.Contains(op.Value)) {
				// 4 (1 below highest of normal) + inverse 0-1 of position in the thing
				precedence = 4 + Mathf.InverseLerp(0, remaining.Count + 1, i);
				action = Actions.Assignment;
			}

			if (precedence > highestPrecedence) { // > not >= for leftmost
				highestPrecedence = precedence;
				highestIndex = i;
				highestAction = action;
			}
		}
	}

	private Data ParseFString(string s, ActionContext AC) {
		#region unpack AC
		Line line = AC.line;
		Memory memory = AC.memory;
		#endregion

		if (LanguageConfig.DEBUG) HF.LogColor($"Parsing f string {s}", Color.yellow);

		StringBuilder sb = new();

		int depth = 0;
		for (int i = 0; i < s.Length; i++) {
			char c = s[i];

			if (c == '}') depth--;

			else if (c == '{') {
				if (depth == 0) {
					int startIndex = i;

					while (i < s.Length) { // get braces chunk
						c = s[i];
						if (c == '{') depth++;
						else if (c == '}') depth--;
						if (depth == 0) break;
						i++;
					}

					string expression = s[(startIndex + 1)..i];
					Tokenizer tokenizer = new();
					(List<Token> tokens, Data tryTokenize) = tokenizer.TokenizeLine(expression);
					if (tryTokenize is Error) return tryTokenize;

					Data tryEval = EvaluateInternal(line.CopyWithNewTokens(tokens), memory);
					if (tryEval is Error) return tryEval;

					Data tryCast = tryEval.Cast(Primitive.String.InternalType);
					if (tryCast is Error) return tryCast;
					if (tryCast is not Primitive.String S) return Errors.BadCode();

					sb.Append(S.Value);
				}

				depth++;
			}
			else
				sb.Append(c);
		}

		return new Primitive.String(sb.ToString());
	}

	private Data HandleDotOperator(in ActionContext AC) {
		#region unpack AC
		List<Token> remaining		= AC.remaining;
		int highestIndex			= AC.highestIndex;
		Token right					= AC.right;
		Reference leftRef			= AC.leftRef;
		bool leftIsRefAndExists		= AC.leftIsRefAndExists;
		Reference rightRef			= AC.rightRef;
		bool rightIsRefAndExists	= AC.rightIsRefAndExists;
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

			HF.ReplaceRange(remaining, replaceStart, replaceEnd,					  // replace those tokens with the value
				new() { Reference.ExistingGlobalReference(new Primitive.Number(		 // turn value into ref
					realValue														// actual value
			))});
		}

		// normal member syntax, expect existing reference on left
		if (leftRef != null) {// left is ref
			if (!leftIsRefAndExists) // leftref doesnt exist
				return Errors.UnknownName(leftRef);
				
			if (right is Name rightname) {
				Data tryget = leftRef.GetData();
				if (tryget is Error) return tryget;

				tryget = tryget.GetMember(rightname.Value);

				Reference dataRef;
				if (tryget is Error)
					dataRef = Reference.NewMemberReference(leftRef, rightname.Value);
				else
					dataRef = Reference.ExistingMemberReference(leftRef, tryget, rightname.Value);

				HF.ReplaceRange( // replace l . r with R
					remaining,
					highestIndex - 1,
					highestIndex + 1,
					new() { dataRef });
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
		Line line					= AC.line					;
		Memory memory				= AC.memory					;
		List<Token> remaining		= AC.remaining				;
		Token highestToken			= AC.highestToken			;
		int highestIndex			= AC.highestIndex			;
		Token left					= AC.left					;
		Token right					= AC.right					;
		Reference leftRef			= AC.leftRef				;
		bool leftIsRefAndExists		= AC.leftIsRefAndExists		;
		Reference rightRef			= AC.rightRef				;
		bool rightIsRefAndExists	= AC.rightIsRefAndExists	;
		#endregion

		Operator highestTokenAsOp = highestToken as Operator; // it should be operator plz....

		Operator.Ops pairing = highestTokenAsOp.Value switch {
			Operator.Ops.OpenParentheses	=> Operator.Ops.CloseParentheses,
			Operator.Ops.OpenBracket		=> Operator.Ops.CloseBracket,
			Operator.Ops.OpenBrace			=> Operator.Ops.CloseBrace,
			_ => Operator.Ops.None
		};
		if (pairing == Operator.Ops.None) return Errors.CouldntParse(line.OriginalString);
		
		int i = highestIndex;
		int depth = 0;
		while (i < remaining.Count) { // find index of matching
			if (remaining[i] is Operator op) {
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
			highestTokenAsOp.Value switch {
				Operator.Ops.OpenParentheses => "parentheses",
				Operator.Ops.OpenBracket => "brackets",
				Operator.Ops.OpenBrace => "braces",
				_ => "unknown"
			});

		List<Token> regionTokens = remaining.GetRange(highestIndex + 1, pairIndex - highestIndex - 1);

		switch (highestTokenAsOp.Value) {
			case Operator.Ops.OpenParentheses:
				bool isArguments = leftRef != null; // doesnt matter if it doesnt exist, will be errored
				if (isArguments) { // run function
					Data run = RunFunction(AC, regionTokens);
					if (run is Error) return run;

					HF.ReplaceRange(remaining, highestIndex - 1, pairIndex, 
						new() { Reference.ExistingGlobalReference(
							run
					) });
				}
				else {
					Data evalSubexp = Evaluate(line.CopyWithNewTokens(regionTokens), memory);
					if (evalSubexp is Error) return evalSubexp;

					HF.ReplaceRange(remaining, highestIndex, pairIndex, 
						new() { Reference.ExistingGlobalReference(
							evalSubexp
					) });
				}
				break;

			case Operator.Ops.OpenBracket:
				bool indexing = leftIsRefAndExists;
				if (indexing) {
					Primitive.List leftAsList		= leftRef.ThisReference as Primitive.List;
					Primitive.String leftAsString	= leftRef.ThisReference as Primitive.String;
					Primitive.Dict leftAsDict		= leftRef.ThisReference as Primitive.Dict;

					if (leftAsList == null && leftAsString == null && leftAsDict == null)
						return Errors.CannotIndex(leftRef.ThisReference.Type.Name);

					List<Data> baseList = leftAsList?.Value;
					if (leftAsString != null)
						baseList = Enumerable.Repeat<Data>(null, leftAsString.Value.Length).ToList(); // turn string into representative list

					Data evalList = EvaluateList(
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
								new() { Reference.ExistingGlobalReference(new Primitive.List(
									indexed
							)) });
						}
						else { // left is string
							StringBuilder sb = new();
							foreach (int val in indices)
								sb.Append(leftAsString.Value[val >= 0 ? val : leftAsList.Value.Count - val]);

							HF.ReplaceRange(remaining, highestIndex - 1, pairIndex,
								new() { Reference.ExistingGlobalReference(new Primitive.String(
									sb.ToString()
							)) });
						}
					}
					else { // dict key get
						List<Data> values = new();
						foreach (Data key in (evalList as Primitive.List).Value) {
							Data trygetvalue = Interpreter.RunFunction(
								memory,
								new Primitive.Function("get", Primitive.Dict.get),
								leftAsDict,
								new() { key });
							if (trygetvalue is Error) return trygetvalue;

							values.Add(trygetvalue);
						}

						Data res =
							values.Count > 1 ? new Primitive.List(values) :
							values[0];

						HF.ReplaceRange(remaining, highestIndex - 1, pairIndex,
							new() { Reference.ExistingGlobalReference(
								res
						) });
					}
				}
				else { // normal list
					Data evalList = EvaluateList(line.CopyWithNewTokens(regionTokens), memory);
					if (evalList is Error) return evalList;

					HF.ReplaceRange(remaining, highestIndex, pairIndex, 
						new() { Reference.ExistingGlobalReference(
							evalList
					) });
				}
				break;

			case Operator.Ops.OpenBrace:
				Data evalDict = EvaluateDict(line.CopyWithNewTokens(regionTokens), memory);
				if (evalDict is Error) return evalDict;

				HF.ReplaceRange(remaining, highestIndex, pairIndex, 
					new() { Reference.ExistingGlobalReference(
						evalDict
				) });
				break;
		}

		return Data.Success;
	}

	private Data RunFunction(in ActionContext AC, List<Token> regionTokens) {
		#region unpack AC
		Line line = AC.line;
		Memory memory = AC.memory;
		Reference leftRef = AC.leftRef;
		#endregion
		
		// make sure left is a callable type
		if (!leftRef.Exists)
			return Errors.UnknownName(leftRef);

		if (leftRef.ThisReference is not Primitive.Function func)
			return Errors.MemberIsNotMethod(leftRef.Name, leftRef.ThisReference.Type.Name);

		Data evalArgs = EvaluateList(
			line.CopyWithNewTokens(regionTokens),
			memory);
		if (evalArgs is Error) return evalArgs;

		List<Data> args = (evalArgs as Primitive.List).Value;

		//Memory context = leftRef.IsInstanceVariable ? leftRef.ParentReference.Memory : memory;
		Memory context = memory;

		Data run = Interpreter.RunFunction(context, func, leftRef.ParentReference, args); // might change this retian later 
		run.ClearFlags();
		return run;
	}

	private Data HandleUnary(in ActionContext AC) {
		#region unpack AC
		List<Token> remaining		= AC.remaining;
		Token highestToken			= AC.highestToken;
		int highestIndex			= AC.highestIndex;
		Reference rightRef			= AC.rightRef;
		bool rightIsRefAndExists	= AC.rightIsRefAndExists;
		#endregion

		Operator thisOp = (highestToken as Operator);

		if (!rightIsRefAndExists)
			return Errors.InvalidUseOfOperator(thisOp.StringValue);

		if (thisOp.Value == Operator.Ops.Plus ||
			thisOp.Value == Operator.Ops.Minus) { // + or -

			Data castToNumber = rightRef.ThisReference.Cast(Primitive.Number.InternalType);
			if (castToNumber is Error) return castToNumber;

			double value = (castToNumber as Primitive.Number).Value;
			// use a copy of the number i guess
			HF.ReplaceRange(remaining, highestIndex, highestIndex + 1,
				new() { Reference.ExistingGlobalReference(new Primitive.Number(
					value * (thisOp.Value == Operator.Ops.Minus ? -1 : 1
				))) });
		}
		else { // !
			Data castToBool = rightRef.ThisReference.Cast(Primitive.Bool.InternalType);
			if (castToBool is Error) return castToBool;

			bool value = (castToBool as Primitive.Bool).Value;

			// use a copy of the bool
			HF.ReplaceRange(remaining, highestIndex, highestIndex + 1,
				new() { Reference.ExistingGlobalReference(new Primitive.Bool(
					!value
				)) });
		}

		return Data.Success;
	}

	private Data OperatorCheck(
		in ActionContext AC,
		in Operator op,
		out Data leftData, out Data rightData) {
		#region unpack AC
		Reference leftRef = AC.leftRef;
		bool leftIsRefAndExists = AC.leftIsRefAndExists;
		Reference rightRef = AC.rightRef;
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
			return Errors.UnknownName(leftRef);
		if (!rightIsRefAndExists)
			return Errors.UnknownName(rightRef);

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
		Reference leftRef = AC.leftRef;
		Reference rightRef = AC.rightRef;
		#endregion

		// assume precedence is sorted out already

		Operator op = highestToken as Operator;

		Data check = OperatorCheck(AC, op, out Data leftData, out Data rightData);
		if (check is Error) return check;

		Data perform = PerformOperation(op, leftData, rightData, leftRef, rightRef, memory);
		if (perform is Error) return perform;

		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, 
			new() { Reference.ExistingGlobalReference(
				perform
		) });

		return Data.Success;
	}

	private Data PerformOperation(
		in Operator op,
		in Data leftData, in Data rightData,
		in Reference leftRef, in Reference rightRef,
		in Memory memory) {
		
		// cast left to right
		Data left = leftData.Cast(rightRef.ThisReference.Type);
		if (left is Error) return left;
		Data right = rightData;

		Dictionary<Operator.Ops, string> opNames = new() {
			{ Operator.Ops.Plus,		"ad" },
			{ Operator.Ops.Minus,		"su" },
			{ Operator.Ops.Multiply,	"mu" },
			{ Operator.Ops.Divide,		"di" },
			{ Operator.Ops.Mod,			"mo" },
			{ Operator.Ops.Power,		"po" } 
		};

		string operationName = opNames[op.Value];
		Data tryGetLeftMember = leftData.GetMember(operationName);
		if (tryGetLeftMember is Error)
			return Errors.UnsupportedOperation(
				op.StringValue, 
				leftRef.ThisReference.Type.Name, 
				rightRef.ThisReference.Type.Name);
		if (tryGetLeftMember is not Primitive.Function F)
			return Errors.MemberIsNotMethod(operationName, leftData.Type.Name);

		Data runFunction = Interpreter.RunFunction(
			memory, 
			F, 
			left, 
			new() { right }
		);
		return runFunction;
	}
	
	private Data HandleComparison(in ActionContext AC) {
		#region unpack AC
		Memory memory = AC.memory;
		List<Token> remaining = AC.remaining;
		Token highestToken = AC.highestToken;
		int highestIndex = AC.highestIndex;
		#endregion

		// assume precedence is sorted out already

		Operator op = highestToken as Operator;

		Data check = OperatorCheck(AC, op, out Data leftData, out Data rightData);
		if (check is Error) return check;

		Data compare = Compare(leftData, rightData, op, memory);
		if (compare is Error) return compare;

		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, 
			new() { Reference.ExistingGlobalReference(
				compare
		) });

		return Data.Success;
	}

	private Data HandleLogical(in ActionContext AC) {
		#region unpack AC
		List<Token> remaining = AC.remaining;
		Token highestToken = AC.highestToken;
		int highestIndex = AC.highestIndex;
		#endregion

		// assume precedence is sorted out already

		Operator op = highestToken as Operator;

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
			Operator.Ops.And	=> a && b,
			Operator.Ops.Or		=> a || b,
			Operator.Ops.Nand	=> !(a && b),
			Operator.Ops.Nor	=> !(a || b),
			Operator.Ops.Xor	=> a != b,
			_ => false // vs wouldnt stop screaming at me to add default case
		};

		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, 
			new() { Reference.ExistingGlobalReference(new Primitive.Bool(
				result
		)) });

		return Data.Success;
	}
	
	private Data HandleAssignment(in ActionContext AC) {
		#region unpack AC
		Memory memory				= AC.memory;
		List<Token> remaining		= AC.remaining;
		Token highestToken			= AC.highestToken;
		int highestIndex			= AC.highestIndex;
		Reference leftRef			= AC.leftRef;
		bool leftIsRefAndExists		= AC.leftIsRefAndExists;
		Reference rightRef			= AC.rightRef;
		bool rightIsRefAndExists	= AC.rightIsRefAndExists;
		#endregion

		// assume precedence is sorted out already

		Operator op = highestToken as Operator;

		// check right for ref to existing data
		if (rightRef == null)
			return Errors.Expected("expression", "right of " + op.StringValue);
		if (!rightIsRefAndExists)
			return Errors.UnknownName(rightRef);
		Data rightData = rightRef.ThisReference;

		// check left for ref (doesnt have to exist)
		if (leftRef == null)
			return Errors.Expected("expression", "left of " + op.StringValue);

		Data newValue;

		if (op.Value == Operator.Ops.Equals)
			newValue = rightData;
		else {
			if (!leftIsRefAndExists) // += and others have to have existing left type
				return Errors.UnknownName(leftRef);

			string opToPerform = op.Value switch {
				Operator.Ops.PlusEquals			=> "+",
				Operator.Ops.MinusEquals		=> "-",
				Operator.Ops.MultiplyEquals		=> "*",
				Operator.Ops.DivideEquals		=> "/",
				Operator.Ops.PowerEquals		=> "^",
				Operator.Ops.ModEquals			=> "%",
				_ => ""
			};
			Operator inlineOp = new(opToPerform); // sets it up so i dont have to

			Data performOp = PerformOperation(
				inlineOp, leftRef.ThisReference, rightData, leftRef, rightRef, memory);
			if (performOp is Error) return performOp;

			newValue = performOp;
		}

		Data trySet = memory.Set(leftRef, newValue);
		if (trySet is Error) return trySet;

		// replace with left bc its the actual new reference that got assigned
		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, new() { leftRef });
		return Data.Success;
	}

	private Data HandleKeywords		(ref List<Token> tokens, Keyword kw) {
		return kw.Value switch {
			Keyword.Kws.If			=> HandleIf			(ref tokens),
			Keyword.Kws.Else		=> HandleElse		(ref tokens),
			Keyword.Kws.For			=> HandleFor		(ref tokens),
			Keyword.Kws.While		=> HandleWhile		(ref tokens),
			Keyword.Kws.Break		=> HandleBreak		(ref tokens),
			Keyword.Kws.Continue	=> HandleContinue	(ref tokens),
			Keyword.Kws.Pass		=> HandlePass		(ref tokens),
			Keyword.Kws.Return		=> HandleReturn		(ref tokens),
			Keyword.Kws.Try			=> HandleTry		(ref tokens),
			Keyword.Kws.Except		=> HandleExcept		(ref tokens),
			Keyword.Kws.Finally		=> HandleFinally	(ref tokens),
			Keyword.Kws.Raise		=> HandleRaise		(ref tokens),
			_ => Data.Success
		};
	}
	private Data HandleIf			(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 3 ||
			!(tokens[1] is Reference R && // check for R : 
			tokens[2] is Operator o && o.Value == Operator.Ops.Colon))
			return Errors.BadSyntaxFor("if statement");

		if (!R.Exists)
			return Errors.UnknownName(R);

		Data dataAsBool = R.ThisReference.Cast(Primitive.Bool.InternalType);
		if (dataAsBool is Error) return dataAsBool;

		if ((dataAsBool as Primitive.Bool).Value)
			return Data.Success.CopyWithFlags(
									Flags.If |
									Flags.Success
									);
		else
			return Data.Fail.CopyWithFlags(
								Flags.If |
								Flags.Fail
								);
	}
	private Data HandleElse			(ref List<Token> tokens) {
		// check if this is else if
		if (tokens.Count > 1 &&
			tokens[1] is Keyword kw && kw.Value == Keyword.Kws.If)
			return HandleElseIf(ref tokens);

		// syntax check
		if (tokens.Count != 2 ||
			!(tokens[1] is Operator o && o.Value == Operator.Ops.Colon))
			return Errors.BadSyntaxFor("else statement");

		return Data.Success.CopyWithFlags(Flags.Else);
	}
	private Data HandleElseIf		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 4 ||
			!(tokens[2] is Reference R && // check for R :
			tokens[3] is Operator o && o.Value == Operator.Ops.Colon))
			return Errors.BadSyntaxFor("else if statement");

		if (!R.Exists)
			return Errors.UnknownName(R);

		Data dataAsBool = (tokens[2] as Reference).ThisReference.Cast(Primitive.Bool.InternalType);
		if (dataAsBool is Error) return dataAsBool;

		if ((dataAsBool as Primitive.Bool).Value)
			return Data.Success.CopyWithFlags(
									Flags.Else |
									Flags.If |
									Flags.Success
									);
		else
			return Data.Fail.CopyWithFlags(
								Flags.Else |
								Flags.If |
								Flags.Fail
								);
	}
	private Data HandleFor			(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 3 ||
			!(tokens[1] is Reference R &&
			(tokens[2] is Operator o && o.Value == Operator.Ops.Colon)))
			return Errors.BadSyntaxFor("for loop");

		if (!R.Exists)
			return Errors.UnknownName(R);

		Data dataAsList = R.ThisReference.Cast(Primitive.List.InternalType);
		if (dataAsList is Error) return dataAsList;

		return dataAsList.CopyWithFlags(Flags.For);
	}
	private Data HandleWhile		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 3 ||
			!(tokens[1] is Reference R &&
			(tokens[2] is Operator o && o.Value == Operator.Ops.Colon)))
			return Errors.BadSyntaxFor("while loop");
		
		if (!R.Exists)
			return Errors.UnknownName(R);

		Data dataAsBool = R.ThisReference.Cast(Primitive.Bool.InternalType);
		if (dataAsBool is Error) return dataAsBool;

		return dataAsBool.CopyWithFlags(Flags.While);
	}
	private Data HandleBreak		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 1)
			return Errors.BadSyntaxFor("break statement");

		return Data.Fail.CopyWithFlags(Flags.Break);
	}
	private Data HandleContinue		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 1)
			return Errors.BadSyntaxFor("continue statement");

		return Data.Success.CopyWithFlags(Flags.Continue);
	}
	private Data HandlePass			(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 1)
			return Errors.BadSyntaxFor("pass statement");

		return Data.Success;
	}
	private Data HandleReturn		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			tokens[1] is not Reference R)
			return Errors.BadSyntaxFor("return statement");

		if (!R.Exists)
			return Errors.UnknownName(R);

		return R.ThisReference.CopyWithFlags(Flags.Return);
	}
	private Data HandleTry			(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			!(tokens[1] is Operator o && o.Value == Operator.Ops.Colon))
			return Errors.BadSyntaxFor("try statement");

		return Data.Success.CopyWithFlags(Flags.Try);
	}
	private Data HandleExcept		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			!(tokens[1] is Operator o && o.Value == Operator.Ops.Colon))
			return Errors.BadSyntaxFor("except statement");

		return Data.Success.CopyWithFlags(Flags.Except);
	}
	private Data HandleFinally		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			!(tokens[1] is Operator o && o.Value == Operator.Ops.Colon))
			return Errors.BadSyntaxFor("finally statement");

		return Data.Success.CopyWithFlags(Flags.Finally);
	}
	private Data HandleRaise		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			tokens[1] is not Reference R)
			return Errors.BadSyntaxFor("finally statement");

		if (!R.Exists)
			return Errors.UnknownName(R);

		Data castToString = R.ThisReference.Cast(Primitive.String.InternalType);
		if (castToString is Error) return castToString;

		return castToString.CopyWithFlags(Flags.Finally);
	}

	private Data EvaluateList(Line line, Memory memory, List<Data> baseList = null) {
		// expects unsurrounded list, just tokens and commas
		List<Token> tokens = line.Tokens;
		if (tokens.Count == 0) return new Primitive.List();

		// identify list type
		bool rangeList = false;

		// find and count ellipses
		(int ellipsisIndex, int eCount) = FindAndCountOperator(line.Tokens, Operator.Ops.Ellipsis);
		
		if (eCount > 1) // either 0 or 1 ..s
			return Errors.InvalidUseOfOperator("..");
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
				else if (leftList.Value.Count > 2)
					return Errors.BadSyntaxFor("range lists", "too many start parameters");
			}

			if (rightOfEllipsis.Length != 0) {
				Data rightOfEllipsisEval = Evaluate(
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
			int depth = 0; // have to account for nested lists
			while (i < tokens.Count) {
				Token rt = tokens[i];
				bool added = false;
				if (rt is Operator op){
					switch (op.Value) {
						case Operator.Ops.Comma: // normal comma breakage into new chunk
							if (depth == 0) {
								added = true;
								tokenChunks.Add(curChunk);
								curChunk = new(); // instead of clearing so the reference isnt shared
							}
							break;

						case Operator.Ops.OpenParentheses:
						case Operator.Ops.OpenBracket:
						case Operator.Ops.OpenBrace:
							depth++;
							break;

						case Operator.Ops.CloseParentheses:
						case Operator.Ops.CloseBracket:
						case Operator.Ops.CloseBrace:
							depth--;
							break;

					}
				}

				if (!added)
					curChunk.Add(rt);
				i++;
			}
			if (curChunk.Count > 0) 
				tokenChunks.Add(curChunk);

			// eval arg token chunks into data
			List<Data> items = new(); // can be optimized into array if desperate
			foreach (List<Token> chunk in tokenChunks) {
				Data tryEval = Evaluate(line.CopyWithNewTokens(chunk), memory);
				if (tryEval is Error) return tryEval;

				items.Add(tryEval);
			}

			return new Primitive.List(items);
		}
	}

	private Data EvaluateDict(Line line, Memory memory) {
		List<Token> tokens = line.Tokens;

		// stole from evallist lol

		// split into chunks by comma
		List<Token> curChunk = new();
		List<List<Token>> tokenChunks = new();
		int i = 0;
		while (i < tokens.Count) {
			Token rt = tokens[i];
			if (rt is Operator op && op.Value == Operator.Ops.Comma) {
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
				if (group[ci] is Operator co && co.Value == Operator.Ops.Colon) {
					colonCount++;
					colonIndex = ci;
				}
			}
			if (colonCount == 0) return Errors.BadSyntaxFor("dictionary", "missing colon");
			if (colonCount > 1)  return Errors.BadSyntaxFor("dictionary", "too many colons");

			Token[] key = group.ToArray()[..colonIndex];
			Token[] value = group.ToArray()[(colonIndex + 1)..];

			if (key.Length == 0)   return Errors.BadSyntaxFor("dictionary", "missing key");
			if (value.Length == 0) return Errors.BadSyntaxFor("dictionary", "missing value");

			kvpGroups.Add(new[] { key, value });
		}

		Dictionary<Data, Data> newDict = new();
		foreach (Token[][] kvpTokenGroup in kvpGroups) {
			Data evalKey = Evaluate(line.CopyWithNewTokens(kvpTokenGroup[0].ToList()), memory);
			if (evalKey is Error) return evalKey;

			Data evalValue = Evaluate(line.CopyWithNewTokens(kvpTokenGroup[1].ToList()), memory);
			if (evalValue is Error) return evalValue;

			newDict[evalKey] = evalValue;
		}

		return new Primitive.Dict(newDict);
	}

	private (int, int) FindAndCountOperator(List<Token> tokens, Operator.Ops lookFor) {
		int count = 0;
		int index = -1;

		int depth = 0;
		for (int i = 0; i < tokens.Count; i++) {
			if (tokens[i] is Operator op) {
				switch (op.Value) {
					case Operator.Ops.CloseParentheses:
					case Operator.Ops.CloseBracket:
					case Operator.Ops.CloseBrace:
						depth--;
						break;
				}

				if (op.Value == lookFor) {
					if (depth == 0) {
						index = i;
						count++;
					}
				}
				
				switch (op.Value) {
					case Operator.Ops.OpenParentheses:
					case Operator.Ops.OpenBracket:
					case Operator.Ops.OpenBrace:
						depth++;
						break;
				}
			}
		}

		return (index, count);
	}
}