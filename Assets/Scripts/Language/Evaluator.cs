using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static Token;

public class Evaluator {
	public Interpreter Interpreter;

	public T_Data Compare(T_Data a, T_Data b, T_Operator op, Memory memory) {
		// not gona add comparison operator check bc whatever's calling this should already have done it

		T_Data lt = null;
		bool lessthan = false;
		if (!(op.Value == T_Operator.Ops.Equality || 
			op.Value == T_Operator.Ops.NotEquals)) {
			lt = LessThan(a, b, memory, Interpreter); // is bool checks done in the methods already
			if (lt is Error) return lt;
		
			lessthan = (lt as Primitive.Bool).Value;
		}

		T_Data eq = null;
		bool equals = false;
		if (!(op.Value == T_Operator.Ops.LessThan ||
			op.Value == T_Operator.Ops.GreaterThanOrEqualTo)) {
			eq = Equals(a, b, memory, Interpreter);
			if (eq is Error) return eq;
		
			equals = (eq as Primitive.Bool).Value;
		}

		return op.Value switch {
			T_Operator.Ops.Equality				=> eq,
			T_Operator.Ops.NotEquals				=> new Primitive.Bool(!equals), // (!=) = (! ==)
			T_Operator.Ops.LessThan				=> lt,
			T_Operator.Ops.GreaterThan			=> new Primitive.Bool(!(lessthan || equals)), // (>) = (!<=) 
			T_Operator.Ops.LessThanOrEqualTo		=> new Primitive.Bool(lessthan || equals), // (<=) = (< || ==)
			T_Operator.Ops.GreaterThanOrEqualTo	=> new Primitive.Bool(!lessthan), // (>=) = (! <)
			_ => Errors.CannotCompare(a.Type.Name, b.Type.Name),
		};
	}
	private T_Data Equals(T_Data a, T_Data b, Memory memory, Interpreter interpreter) { // op should either be == or !=
																				  // try to get the eq operator from either a or b, if neither has one then return false
		T_Data f = a.GetMember("eq");
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

		T_Data run = interpreter.RunFunction(memory, function, a, new() { b });
		if (run is Error) return run;
		if (run is not Primitive.Bool)
			return Errors.ImproperImplementation($"the eq operator", "Should return a bool");

		return run; // more handling to be done by specifics
	}
	private T_Data LessThan(T_Data a, T_Data b, Memory memory, Interpreter interpreter) {
		// try to get the eq operator from the data
		T_Data f = a.GetMember("lt");
		if (f is Error) return f;
		if (f is not Primitive.Function)
			return Errors.CannotCompare(a.Type.Name, b.Type.Name);

		Primitive.Function function = f as Primitive.Function;

		T_Data run = interpreter.RunFunction(memory, function, a, new() { b });
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
		public T_Reference		leftRef;
		public bool				leftIsRefAndExists;
		public T_Reference		rightRef;
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
			T_Reference leftRef, 
			bool leftIsRefAndExists, 
			T_Reference rightRef, 
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

	public T_Data Evaluate(Line line, Memory memory, bool makeCopy = true, int depth = 0) {

		if (makeCopy)
			line = line.DeepCopy();

		T_Data.currentUseMemory = memory;
		UpdateAllLineDataWithMemory(ref line, memory);
		T_Data tryEvaluate = EvaluateInternal(line, memory, depth);
		T_Data.currentUseMemory = null;

		return tryEvaluate;
	}

	private void UpdateAllLineDataWithMemory(ref Line line, Memory memory) {
		foreach (Token token in line.Tokens) {
			if (token is T_Data d)
				d.Memory = memory;
		}
	}

	private T_Data EvaluateInternal(Line line, Memory memory, int depth = 0) {
		if (line.Tokens.Count == 0) return Errors.CannotEvaluateEmpty();
		if (Config.Language.DEBUG) HF.WarnColor($"Evaluating {line.TokenList()}", Color.green);

		T_Data declChecks = DeclarationChecks(line.Tokens);
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
			T_Reference leftRef			= left as T_Reference;
			bool leftIsRefAndExists		= leftRef != null && leftRef.Exists;

			Token right					= remaining.ElementAtOrDefault(highestIndex + 1);
			T_Reference rightRef			= right as T_Reference;
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
					T_Data parseFString = ParseFString((remaining[highestIndex + 1] as Primitive.String).Value, localActionContext);
					HF.ReplaceRange(remaining, highestIndex, highestIndex + 1, 
						new() { T_Reference.ExistingGlobalReference(
							parseFString
					) });
					break;

				// D -> R
				case Actions.Data:
					T_Data data = highestToken as T_Data;
					remaining[highestIndex] = T_Reference.ExistingGlobalReference("", data);
					break;

				// N -> R 
				case Actions.Name:
					// check memory for name 
					string name = (highestToken as T_Name).Value;
					T_Data get = memory.Get(name);
					// dont return error again LMAO

					// replace name token with reference token
					remaining[highestIndex] = (get is not Error) ?
						T_Reference.ExistingGlobalReference(name, get) :   // make existing if data exists
						T_Reference.NewGlobalReference(name);             // or make new
					break;

				// decimal / member handling
				case Actions.DotOperator:
					T_Data tryHandleDotOperator = HandleDotOperator(localActionContext);
					if (tryHandleDotOperator is Error) return tryHandleDotOperator;
					break;

				// region operators
				case Actions.Region:
					T_Data tryHandleRegion = HandleRegion(localActionContext);
					if (tryHandleRegion is Error) return tryHandleRegion;
					break;

				// unary operators
				case Actions.Unary:
					T_Data tryHandleUnary = HandleUnary(localActionContext);
					if (tryHandleUnary is Error) return tryHandleUnary;
					break;

				// normal operators
				case Actions.Arithmetic:
					T_Data tryHandleArithmetic = HandleArithmetic(localActionContext);
					if (tryHandleArithmetic is Error) return tryHandleArithmetic;
					break;

				case Actions.Comparison:
					T_Data tryHandleComparison = HandleComparison(localActionContext);
					if (tryHandleComparison is Error) return tryHandleComparison;
					break;

				case Actions.Logical:
					T_Data tryHandleLogical = HandleLogical(localActionContext);
					if (tryHandleLogical is Error) return tryHandleLogical;
					break;

				// assignment
				case Actions.Assignment:
					T_Data tryHandleAssignment = HandleAssignment(localActionContext);
					if (tryHandleAssignment is Error) return tryHandleAssignment;
					break;
			}

			if (last.SequenceEqual(remaining)) break; // duplicate between iters = break
			last = new(remaining);
		}

		if (remaining[0] is T_Keyword kw) {
			T_Data handleKeywords = HandleKeywords(ref remaining, kw);
			if (handleKeywords is Error) return handleKeywords;

			return handleKeywords;
		}

		// return data if thats what it collapses to
		if (remaining.Count == 1 && remaining[0] is T_Reference r) {
			if (!r.Exists)
				return Errors.UnknownName(r);

			return r.ThisReference;
		}

		return T_Data.Success;
	}

	private T_Data DeclarationChecks(List<Token> tokens) {
		// check for function, inline function, or class

		(int colonIndex, _) = FindAndCountOperator(tokens, T_Operator.Ops.Colon);
		if (tokens[0] is T_Name N && colonIndex != -1) { // some kind of declaration

			// determine if this is some kind of function
			(int oPIndex, int oPCount) = FindAndCountOperator(tokens, T_Operator.Ops.OpenParentheses);
			(int cPIndex, int cPCount) = FindAndCountOperator(tokens, T_Operator.Ops.CloseParentheses);

			bool maybeFunction = (oPIndex != -1) || (cPIndex != -1);

			if (maybeFunction) {
				if (oPIndex == -1 || cPIndex == -1 ||
					oPCount > 1 || cPCount > 1)
					return Errors.BadSyntaxFor("function declaration", "mismatched parentheses");

				// find all param names and store as string list
				List<string> paramNames = new();
				for (int i = oPIndex + 1; i < cPIndex; i++) {
					Token thisToken = tokens[i];

					if (thisToken is T_Operator thisOp) {
						if (thisOp.Value == T_Operator.Ops.Comma) {
							if (i > oPIndex + 1 && tokens[i - 1] is T_Operator) // 2 ops in a row
								return Errors.BadSyntaxFor("function declaration", "bad parameter list syntax");
							
							continue; // delimeter, ignore it
						}
						else // any op other than , 
							return Errors.BadSyntaxFor("function declaration", $"invalid operator {thisOp.StringValue} in parameters");
					}
					
					if (thisToken is T_Keyword)
						return Errors.BadSyntaxFor("function declaration", "cannot use keywords as parameters");

					if (thisToken is not T_Name thisName) // for good measure
						return Errors.BadSyntaxFor("function declaration");

					paramNames.Add(thisName.Value);
				}

				(int eqIndex, int eqCount) = FindAndCountOperator(tokens, T_Operator.Ops.Equals);
				
				if (eqIndex != -1) { // inline function 
					return new Primitive.List(new List<T_Data>() {	   // return list with function info
						new Primitive.String(N.Value),				  // name
						new Primitive.Number(cPIndex)				 // c paren index (rest is definition)
					}).SetFlags(Flags.MakeInline);					// tell interpreter to make inline func
				}

				// normal function
				return new Primitive.List(new List<T_Data>() {		  // return list with function info
					new Primitive.String(N.Value),					 // name
					new Primitive.List(paramNames					// turn arg names into list of strings
						.Select(n => new Primitive.String(n) as T_Data)
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
		return T_Data.Fail;
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

			bool isOp = token is T_Operator;
			T_Operator op = isOp ? token as T_Operator : null;

			// f-strings
			if (token is T_Name N &&
				N.Value == "f" &&
				i != remaining.Count - 1 &&
				remaining[i + 1] is T_Data) {
				precedence = 25;
				action = Actions.FString;
			}

			// D -> R
			else if (token is T_Data) {
				precedence = 20;
				action = Actions.Data;
			}

			// N -> R
			else if (token is T_Name) {
				precedence = 15;
				action = Actions.Name;
			}

			// handle .
			else if (isOp && op.Value == T_Operator.Ops.Dot) {
				precedence = 15;
				action = Actions.DotOperator;
			}

			// region operator
			else if (isOp &&
				(op.Value	== T_Operator.Ops.OpenParentheses ||
				op.Value	== T_Operator.Ops.OpenBracket ||
				op.Value	== T_Operator.Ops.OpenBrace)) {
				precedence = 15;
				action = Actions.Region;
			}

			// unary operator
			else if (isOp && T_Operator.UnaryOperatorsHashSet.Contains(op.Value) &&
				(i == 0 || remaining[i - 1] is T_Operator or T_Keyword)) {
				precedence = 14; // slightly less than others
				action = Actions.Unary;
			}

			// normal operators
			else if (isOp && T_Operator.ArithmeticOperatorsHashSet.Contains(op.Value)) {
				precedence = T_Operator.NormalOperatorsPrecedence[op.Value];
				action = Actions.Arithmetic;
			}
			else if (isOp && T_Operator.ComparisonOperatorsHashSet.Contains(op.Value)) {
				precedence = T_Operator.NormalOperatorsPrecedence[op.Value];
				action = Actions.Comparison;
			}
			else if (isOp && T_Operator.LogicalOperatorsHashSet.Contains(op.Value)) {
				precedence = T_Operator.NormalOperatorsPrecedence[op.Value];
				action = Actions.Logical;
			}

			// assignment
			else if (isOp && T_Operator.AssignmentOperatorsHashSet.Contains(op.Value)) {
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

	private T_Data ParseFString(string s, ActionContext AC) {
		#region unpack AC
		Line line = AC.line;
		Memory memory = AC.memory;
		#endregion

		if (Config.Language.DEBUG) HF.LogColor($"Parsing f string {s}", Color.yellow);

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
					(List<Token> tokens, T_Data tryTokenize) = tokenizer.TokenizeLine(expression);
					if (tryTokenize is Error) return tryTokenize;

					T_Data tryEval = EvaluateInternal(line.CopyWithNewTokens(tokens), memory);
					if (tryEval is Error) return tryEval;

					T_Data tryCast = tryEval.Cast(Primitive.String.InternalType);
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

	private T_Data HandleDotOperator(in ActionContext AC) {
		#region unpack AC
		List<Token> remaining		= AC.remaining;
		int highestIndex			= AC.highestIndex;
		Token right					= AC.right;
		T_Reference leftRef			= AC.leftRef;
		bool leftIsRefAndExists		= AC.leftIsRefAndExists;
		T_Reference rightRef			= AC.rightRef;
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
				new() { T_Reference.ExistingGlobalReference(new Primitive.Number(		 // turn value into ref
					realValue														// actual value
			))});
		}

		// normal member syntax, expect existing reference on left
		if (leftRef != null) {// left is ref
			if (!leftIsRefAndExists) // leftref doesnt exist
				return Errors.UnknownName(leftRef);
				
			if (right is T_Name rightname) {
				T_Data tryget = leftRef.GetData();
				if (tryget is Error) return tryget;

				tryget = tryget.GetMember(rightname.Value);

				T_Reference dataRef;
				if (tryget is Error)
					dataRef = T_Reference.NewMemberReference(leftRef, rightname.Value);
				else
					dataRef = T_Reference.ExistingMemberReference(leftRef, tryget, rightname.Value);

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

		return T_Data.Success;
	}

	private T_Data HandleRegion(in ActionContext AC) {
		#region unpack AC
		Line line					= AC.line					;
		Memory memory				= AC.memory					;
		List<Token> remaining		= AC.remaining				;
		Token highestToken			= AC.highestToken			;
		int highestIndex			= AC.highestIndex			;
		Token left					= AC.left					;
		Token right					= AC.right					;
		T_Reference leftRef			= AC.leftRef				;
		bool leftIsRefAndExists		= AC.leftIsRefAndExists		;
		T_Reference rightRef			= AC.rightRef				;
		bool rightIsRefAndExists	= AC.rightIsRefAndExists	;
		#endregion

		T_Operator highestTokenAsOp = highestToken as T_Operator; // it should be operator plz....

		T_Operator.Ops pairing = highestTokenAsOp.Value switch {
			T_Operator.Ops.OpenParentheses	=> T_Operator.Ops.CloseParentheses,
			T_Operator.Ops.OpenBracket		=> T_Operator.Ops.CloseBracket,
			T_Operator.Ops.OpenBrace			=> T_Operator.Ops.CloseBrace,
			_ => T_Operator.Ops.None
		};
		if (pairing == T_Operator.Ops.None) return Errors.CouldntParse(line.OriginalString);
		
		int i = highestIndex;
		int depth = 0;
		while (i < remaining.Count) { // find index of matching
			if (remaining[i] is T_Operator op) {
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
				T_Operator.Ops.OpenParentheses => "parentheses",
				T_Operator.Ops.OpenBracket => "brackets",
				T_Operator.Ops.OpenBrace => "braces",
				_ => "unknown"
			});

		List<Token> regionTokens = remaining.GetRange(highestIndex + 1, pairIndex - highestIndex - 1);

		switch (highestTokenAsOp.Value) {
			case T_Operator.Ops.OpenParentheses:
				bool isArguments = leftRef != null; // doesnt matter if it doesnt exist, will be errored
				if (isArguments) { // run function
					T_Data run = RunFunction(AC, regionTokens);
					if (run is Error) return run;

					HF.ReplaceRange(remaining, highestIndex - 1, pairIndex, 
						new() { T_Reference.ExistingGlobalReference(
							run
					) });
				}
				else {
					T_Data evalSubexp = Evaluate(line.CopyWithNewTokens(regionTokens), memory);
					if (evalSubexp is Error) return evalSubexp;

					HF.ReplaceRange(remaining, highestIndex, pairIndex, 
						new() { T_Reference.ExistingGlobalReference(
							evalSubexp
					) });
				}
				break;

			case T_Operator.Ops.OpenBracket:
				bool indexing = leftIsRefAndExists;
				if (indexing) {
					Primitive.List leftAsList		= leftRef.ThisReference as Primitive.List;
					Primitive.String leftAsString	= leftRef.ThisReference as Primitive.String;
					Primitive.Dict leftAsDict		= leftRef.ThisReference as Primitive.Dict;

					if (leftAsList == null && leftAsString == null && leftAsDict == null)
						return Errors.CannotIndex(leftRef.ThisReference.Type.Name);

					List<T_Data> baseList = leftAsList?.Value;
					if (leftAsString != null)
						baseList = Enumerable.Repeat<T_Data>(null, leftAsString.Value.Length).ToList(); // turn string into representative list

					T_Data evalList = EvaluateList(
						line.CopyWithNewTokens(regionTokens),
						memory,
						baseList
						);
					if (evalList is Error) return evalList;

					if (leftAsList != null || leftAsString != null) { // indexing
																	  // check indices
						List<int> indices = new();
						foreach (T_Data d in (evalList as Primitive.List).Value) {
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
							List<T_Data> indexed = new();
							foreach (int val in indices)
								indexed.Add(leftAsList.Value[val >= 0 ? val : leftAsList.Value.Count - val]);

							HF.ReplaceRange(remaining, highestIndex - 1, pairIndex,
								new() { T_Reference.ExistingGlobalReference(new Primitive.List(
									indexed
							)) });
						}
						else { // left is string
							StringBuilder sb = new();
							foreach (int val in indices)
								sb.Append(leftAsString.Value[val >= 0 ? val : leftAsList.Value.Count - val]);

							HF.ReplaceRange(remaining, highestIndex - 1, pairIndex,
								new() { T_Reference.ExistingGlobalReference(new Primitive.String(
									sb.ToString()
							)) });
						}
					}
					else { // dict key get
						List<T_Data> values = new();
						foreach (T_Data key in (evalList as Primitive.List).Value) {
							T_Data trygetvalue = Interpreter.RunFunction(
								memory,
								new Primitive.Function("get", Primitive.Dict.get),
								leftAsDict,
								new() { key });
							if (trygetvalue is Error) return trygetvalue;

							values.Add(trygetvalue);
						}

						T_Data res =
							values.Count > 1 ? new Primitive.List(values) :
							values[0];

						HF.ReplaceRange(remaining, highestIndex - 1, pairIndex,
							new() { T_Reference.ExistingGlobalReference(
								res
						) });
					}
				}
				else { // normal list
					T_Data evalList = EvaluateList(line.CopyWithNewTokens(regionTokens), memory);
					if (evalList is Error) return evalList;

					HF.ReplaceRange(remaining, highestIndex, pairIndex, 
						new() { T_Reference.ExistingGlobalReference(
							evalList
					) });
				}
				break;

			case T_Operator.Ops.OpenBrace:
				T_Data evalDict = EvaluateDict(line.CopyWithNewTokens(regionTokens), memory);
				if (evalDict is Error) return evalDict;

				HF.ReplaceRange(remaining, highestIndex, pairIndex, 
					new() { T_Reference.ExistingGlobalReference(
						evalDict
				) });
				break;
		}

		return T_Data.Success;
	}

	private T_Data RunFunction(in ActionContext AC, List<Token> regionTokens) {
		#region unpack AC
		Line line = AC.line;
		Memory memory = AC.memory;
		T_Reference leftRef = AC.leftRef;
		#endregion
		
		// make sure left is a callable type
		if (!leftRef.Exists)
			return Errors.UnknownName(leftRef);

		if (leftRef.ThisReference is not Primitive.Function func)
			return Errors.MemberIsNotMethod(leftRef.Name, leftRef.ThisReference.Type.Name);

		T_Data evalArgs = EvaluateList(
			line.CopyWithNewTokens(regionTokens),
			memory);
		if (evalArgs is Error) return evalArgs;

		List<T_Data> args = (evalArgs as Primitive.List).Value;

		//Memory context = leftRef.IsInstanceVariable ? leftRef.ParentReference.Memory : memory;
		Memory context = memory;

		T_Data run = Interpreter.RunFunction(context, func, leftRef.ParentReference, args); // might change this retian later 
		run.ClearFlags();
		return run;
	}

	private T_Data HandleUnary(in ActionContext AC) {
		#region unpack AC
		List<Token> remaining		= AC.remaining;
		Token highestToken			= AC.highestToken;
		int highestIndex			= AC.highestIndex;
		T_Reference rightRef			= AC.rightRef;
		bool rightIsRefAndExists	= AC.rightIsRefAndExists;
		#endregion

		T_Operator thisOp = (highestToken as T_Operator);

		if (!rightIsRefAndExists)
			return Errors.InvalidUseOfOperator(thisOp.StringValue);

		if (thisOp.Value == T_Operator.Ops.Plus ||
			thisOp.Value == T_Operator.Ops.Minus) { // + or -

			T_Data castToNumber = rightRef.ThisReference.Cast(Primitive.Number.InternalType);
			if (castToNumber is Error) return castToNumber;

			double value = (castToNumber as Primitive.Number).Value;
			// use a copy of the number i guess
			HF.ReplaceRange(remaining, highestIndex, highestIndex + 1,
				new() { T_Reference.ExistingGlobalReference(new Primitive.Number(
					value * (thisOp.Value == T_Operator.Ops.Minus ? -1 : 1
				))) });
		}
		else { // !
			T_Data castToBool = rightRef.ThisReference.Cast(Primitive.Bool.InternalType);
			if (castToBool is Error) return castToBool;

			bool value = (castToBool as Primitive.Bool).Value;

			// use a copy of the bool
			HF.ReplaceRange(remaining, highestIndex, highestIndex + 1,
				new() { T_Reference.ExistingGlobalReference(new Primitive.Bool(
					!value
				)) });
		}

		return T_Data.Success;
	}

	private T_Data OperatorCheck(
		in ActionContext AC,
		in T_Operator op,
		out T_Data leftData, out T_Data rightData) {
		#region unpack AC
		T_Reference leftRef = AC.leftRef;
		bool leftIsRefAndExists = AC.leftIsRefAndExists;
		T_Reference rightRef = AC.rightRef;
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
		return T_Data.Success;
	}

	private T_Data HandleArithmetic(in ActionContext AC) {
		#region unpack AC
		Memory memory = AC.memory;
		List<Token> remaining = AC.remaining;
		Token highestToken = AC.highestToken;
		int highestIndex = AC.highestIndex;
		T_Reference leftRef = AC.leftRef;
		T_Reference rightRef = AC.rightRef;
		#endregion

		// assume precedence is sorted out already

		T_Operator op = highestToken as T_Operator;

		T_Data check = OperatorCheck(AC, op, out T_Data leftData, out T_Data rightData);
		if (check is Error) return check;

		T_Data perform = PerformOperation(op, leftData, rightData, leftRef, rightRef, memory);
		if (perform is Error) return perform;

		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, 
			new() { T_Reference.ExistingGlobalReference(
				perform
		) });

		return T_Data.Success;
	}

	private T_Data PerformOperation(
		in T_Operator op,
		in T_Data leftData, in T_Data rightData,
		in T_Reference leftRef, in T_Reference rightRef,
		in Memory memory) {
		
		// cast left to right
		T_Data left = leftData.Cast(rightRef.ThisReference.Type);
		if (left is Error) return left;
		T_Data right = rightData;

		Dictionary<T_Operator.Ops, string> opNames = new() {
			{ T_Operator.Ops.Plus,		"ad" },
			{ T_Operator.Ops.Minus,		"su" },
			{ T_Operator.Ops.Multiply,	"mu" },
			{ T_Operator.Ops.Divide,		"di" },
			{ T_Operator.Ops.Mod,			"mo" },
			{ T_Operator.Ops.Power,		"po" } 
		};

		string operationName = opNames[op.Value];
		T_Data tryGetLeftMember = leftData.GetMember(operationName);
		if (tryGetLeftMember is Error)
			return Errors.UnsupportedOperation(
				op.StringValue, 
				leftRef.ThisReference.Type.Name, 
				rightRef.ThisReference.Type.Name);
		if (tryGetLeftMember is not Primitive.Function F)
			return Errors.MemberIsNotMethod(operationName, leftData.Type.Name);

		T_Data runFunction = Interpreter.RunFunction(
			memory, 
			F, 
			left, 
			new() { right }
		);
		return runFunction;
	}
	
	private T_Data HandleComparison(in ActionContext AC) {
		#region unpack AC
		Memory memory = AC.memory;
		List<Token> remaining = AC.remaining;
		Token highestToken = AC.highestToken;
		int highestIndex = AC.highestIndex;
		#endregion

		// assume precedence is sorted out already

		T_Operator op = highestToken as T_Operator;

		T_Data check = OperatorCheck(AC, op, out T_Data leftData, out T_Data rightData);
		if (check is Error) return check;

		T_Data compare = Compare(leftData, rightData, op, memory);
		if (compare is Error) return compare;

		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, 
			new() { T_Reference.ExistingGlobalReference(
				compare
		) });

		return T_Data.Success;
	}

	private T_Data HandleLogical(in ActionContext AC) {
		#region unpack AC
		List<Token> remaining = AC.remaining;
		Token highestToken = AC.highestToken;
		int highestIndex = AC.highestIndex;
		#endregion

		// assume precedence is sorted out already

		T_Operator op = highestToken as T_Operator;

		T_Data check = OperatorCheck(AC, op, out T_Data leftData, out T_Data rightData);
		if (check is Error) return check;

		// try cast both to bool
		T_Data trycastLeft = leftData.Cast(Primitive.Bool.InternalType);
		T_Data trycastRight = rightData.Cast(Primitive.Bool.InternalType);
		if (trycastLeft is Error) return trycastLeft;
		if (trycastRight is Error) return trycastRight;

		bool a = (trycastLeft as Primitive.Bool).Value;
		bool b = (trycastRight as Primitive.Bool).Value;

		bool result = op.Value switch {
			T_Operator.Ops.And	=> a && b,
			T_Operator.Ops.Or		=> a || b,
			T_Operator.Ops.Nand	=> !(a && b),
			T_Operator.Ops.Nor	=> !(a || b),
			T_Operator.Ops.Xor	=> a != b,
			_ => false // vs wouldnt stop screaming at me to add default case
		};

		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, 
			new() { T_Reference.ExistingGlobalReference(new Primitive.Bool(
				result
		)) });

		return T_Data.Success;
	}
	
	private T_Data HandleAssignment(in ActionContext AC) {
		#region unpack AC
		Memory memory				= AC.memory;
		List<Token> remaining		= AC.remaining;
		Token highestToken			= AC.highestToken;
		int highestIndex			= AC.highestIndex;
		T_Reference leftRef			= AC.leftRef;
		bool leftIsRefAndExists		= AC.leftIsRefAndExists;
		T_Reference rightRef			= AC.rightRef;
		bool rightIsRefAndExists	= AC.rightIsRefAndExists;
		#endregion

		// assume precedence is sorted out already

		T_Operator op = highestToken as T_Operator;

		// check right for ref to existing data
		if (rightRef == null)
			return Errors.Expected("expression", "right of " + op.StringValue);
		if (!rightIsRefAndExists)
			return Errors.UnknownName(rightRef);
		T_Data rightData = rightRef.ThisReference;

		// check left for ref (doesnt have to exist)
		if (leftRef == null)
			return Errors.Expected("expression", "left of " + op.StringValue);

		T_Data newValue;

		if (op.Value == T_Operator.Ops.Equals)
			newValue = rightData;
		else {
			if (!leftIsRefAndExists) // += and others have to have existing left type
				return Errors.UnknownName(leftRef);

			string opToPerform = op.Value switch {
				T_Operator.Ops.PlusEquals			=> "+",
				T_Operator.Ops.MinusEquals		=> "-",
				T_Operator.Ops.MultiplyEquals		=> "*",
				T_Operator.Ops.DivideEquals		=> "/",
				T_Operator.Ops.PowerEquals		=> "^",
				T_Operator.Ops.ModEquals			=> "%",
				_ => ""
			};
			T_Operator inlineOp = new(opToPerform); // sets it up so i dont have to

			T_Data performOp = PerformOperation(
				inlineOp, leftRef.ThisReference, rightData, leftRef, rightRef, memory);
			if (performOp is Error) return performOp;

			newValue = performOp;
		}

		T_Data trySet = memory.Set(leftRef, newValue);
		if (trySet is Error) return trySet;

		// replace with left bc its the actual new reference that got assigned
		HF.ReplaceRange(remaining, highestIndex - 1, highestIndex + 1, new() { leftRef });
		return T_Data.Success;
	}

	private T_Data HandleKeywords		(ref List<Token> tokens, T_Keyword kw) {
		return kw.Value switch {
			T_Keyword.Kws.If			=> HandleIf			(ref tokens),
			T_Keyword.Kws.Else		=> HandleElse		(ref tokens),
			T_Keyword.Kws.For			=> HandleFor		(ref tokens),
			T_Keyword.Kws.While		=> HandleWhile		(ref tokens),
			T_Keyword.Kws.Break		=> HandleBreak		(ref tokens),
			T_Keyword.Kws.Continue	=> HandleContinue	(ref tokens),
			T_Keyword.Kws.Pass		=> HandlePass		(ref tokens),
			T_Keyword.Kws.Return		=> HandleReturn		(ref tokens),
			T_Keyword.Kws.Try			=> HandleTry		(ref tokens),
			T_Keyword.Kws.Except		=> HandleExcept		(ref tokens),
			T_Keyword.Kws.Finally		=> HandleFinally	(ref tokens),
			T_Keyword.Kws.Raise		=> HandleRaise		(ref tokens),
			_ => T_Data.Success
		};
	}
	private T_Data HandleIf			(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 3 ||
			!(tokens[1] is T_Reference R && // check for R : 
			tokens[2] is T_Operator o && o.Value == T_Operator.Ops.Colon))
			return Errors.BadSyntaxFor("if statement");

		if (!R.Exists)
			return Errors.UnknownName(R);

		T_Data dataAsBool = R.ThisReference.Cast(Primitive.Bool.InternalType);
		if (dataAsBool is Error) return dataAsBool;

		if ((dataAsBool as Primitive.Bool).Value)
			return T_Data.Success.CopyWithFlags(
									Flags.If |
									Flags.Success
									);
		else
			return T_Data.Fail.CopyWithFlags(
								Flags.If |
								Flags.Fail
								);
	}
	private T_Data HandleElse			(ref List<Token> tokens) {
		// check if this is else if
		if (tokens.Count > 1 &&
			tokens[1] is T_Keyword kw && kw.Value == T_Keyword.Kws.If)
			return HandleElseIf(ref tokens);

		// syntax check
		if (tokens.Count != 2 ||
			!(tokens[1] is T_Operator o && o.Value == T_Operator.Ops.Colon))
			return Errors.BadSyntaxFor("else statement");

		return T_Data.Success.CopyWithFlags(Flags.Else);
	}
	private T_Data HandleElseIf		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 4 ||
			!(tokens[2] is T_Reference R && // check for R :
			tokens[3] is T_Operator o && o.Value == T_Operator.Ops.Colon))
			return Errors.BadSyntaxFor("else if statement");

		if (!R.Exists)
			return Errors.UnknownName(R);

		T_Data dataAsBool = (tokens[2] as T_Reference).ThisReference.Cast(Primitive.Bool.InternalType);
		if (dataAsBool is Error) return dataAsBool;

		if ((dataAsBool as Primitive.Bool).Value)
			return T_Data.Success.CopyWithFlags(
									Flags.Else |
									Flags.If |
									Flags.Success
									);
		else
			return T_Data.Fail.CopyWithFlags(
								Flags.Else |
								Flags.If |
								Flags.Fail
								);
	}
	private T_Data HandleFor			(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 3 ||
			!(tokens[1] is T_Reference R &&
			(tokens[2] is T_Operator o && o.Value == T_Operator.Ops.Colon)))
			return Errors.BadSyntaxFor("for loop");

		if (!R.Exists)
			return Errors.UnknownName(R);

		T_Data dataAsList = R.ThisReference.Cast(Primitive.List.InternalType);
		if (dataAsList is Error) return dataAsList;

		return dataAsList.CopyWithFlags(Flags.For);
	}
	private T_Data HandleWhile		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 3 ||
			!(tokens[1] is T_Reference R &&
			(tokens[2] is T_Operator o && o.Value == T_Operator.Ops.Colon)))
			return Errors.BadSyntaxFor("while loop");
		
		if (!R.Exists)
			return Errors.UnknownName(R);

		T_Data dataAsBool = R.ThisReference.Cast(Primitive.Bool.InternalType);
		if (dataAsBool is Error) return dataAsBool;

		return dataAsBool.CopyWithFlags(Flags.While);
	}
	private T_Data HandleBreak		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 1)
			return Errors.BadSyntaxFor("break statement");

		return T_Data.Fail.CopyWithFlags(Flags.Break);
	}
	private T_Data HandleContinue		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 1)
			return Errors.BadSyntaxFor("continue statement");

		return T_Data.Success.CopyWithFlags(Flags.Continue);
	}
	private T_Data HandlePass			(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 1)
			return Errors.BadSyntaxFor("pass statement");

		return T_Data.Success;
	}
	private T_Data HandleReturn		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			tokens[1] is not T_Reference R)
			return Errors.BadSyntaxFor("return statement");

		if (!R.Exists)
			return Errors.UnknownName(R);

		return R.ThisReference.CopyWithFlags(Flags.Return);
	}
	private T_Data HandleTry			(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			!(tokens[1] is T_Operator o && o.Value == T_Operator.Ops.Colon))
			return Errors.BadSyntaxFor("try statement");

		return T_Data.Success.CopyWithFlags(Flags.Try);
	}
	private T_Data HandleExcept		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			!(tokens[1] is T_Operator o && o.Value == T_Operator.Ops.Colon))
			return Errors.BadSyntaxFor("except statement");

		return T_Data.Success.CopyWithFlags(Flags.Except);
	}
	private T_Data HandleFinally		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			!(tokens[1] is T_Operator o && o.Value == T_Operator.Ops.Colon))
			return Errors.BadSyntaxFor("finally statement");

		return T_Data.Success.CopyWithFlags(Flags.Finally);
	}
	private T_Data HandleRaise		(ref List<Token> tokens) {
		// syntax check
		if (tokens.Count != 2 ||
			tokens[1] is not T_Reference R)
			return Errors.BadSyntaxFor("finally statement");

		if (!R.Exists)
			return Errors.UnknownName(R);

		T_Data castToString = R.ThisReference.Cast(Primitive.String.InternalType);
		if (castToString is Error) return castToString;

		return castToString.CopyWithFlags(Flags.Finally);
	}

	private T_Data EvaluateList(Line line, Memory memory, List<T_Data> baseList = null) {
		// expects unsurrounded list, just tokens and commas
		List<Token> tokens = line.Tokens;
		if (tokens.Count == 0) return new Primitive.List();

		// identify list type
		bool rangeList = false;

		// find and count ellipses
		(int ellipsisIndex, int eCount) = FindAndCountOperator(line.Tokens, T_Operator.Ops.Ellipsis);
		
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
				T_Data leftOfEllipsisEval = EvaluateList(
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
				T_Data rightOfEllipsisEval = Evaluate(
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

			List<T_Data> list = new();

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
				if (rt is T_Operator op){
					switch (op.Value) {
						case T_Operator.Ops.Comma: // normal comma breakage into new chunk
							if (depth == 0) {
								added = true;
								tokenChunks.Add(curChunk);
								curChunk = new(); // instead of clearing so the reference isnt shared
							}
							break;

						case T_Operator.Ops.OpenParentheses:
						case T_Operator.Ops.OpenBracket:
						case T_Operator.Ops.OpenBrace:
							depth++;
							break;

						case T_Operator.Ops.CloseParentheses:
						case T_Operator.Ops.CloseBracket:
						case T_Operator.Ops.CloseBrace:
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
			List<T_Data> items = new(); // can be optimized into array if desperate
			foreach (List<Token> chunk in tokenChunks) {
				T_Data tryEval = Evaluate(line.CopyWithNewTokens(chunk), memory);
				if (tryEval is Error) return tryEval;

				items.Add(tryEval);
			}

			return new Primitive.List(items);
		}
	}

	private T_Data EvaluateDict(Line line, Memory memory) {
		List<Token> tokens = line.Tokens;

		// stole from evallist lol

		// split into chunks by comma
		List<Token> curChunk = new();
		List<List<Token>> tokenChunks = new();
		int i = 0;
		while (i < tokens.Count) {
			Token rt = tokens[i];
			if (rt is T_Operator op && op.Value == T_Operator.Ops.Comma) {
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
				if (group[ci] is T_Operator co && co.Value == T_Operator.Ops.Colon) {
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

		Dictionary<T_Data, T_Data> newDict = new();
		foreach (Token[][] kvpTokenGroup in kvpGroups) {
			T_Data evalKey = Evaluate(line.CopyWithNewTokens(kvpTokenGroup[0].ToList()), memory);
			if (evalKey is Error) return evalKey;

			T_Data evalValue = Evaluate(line.CopyWithNewTokens(kvpTokenGroup[1].ToList()), memory);
			if (evalValue is Error) return evalValue;

			newDict[evalKey] = evalValue;
		}

		return new Primitive.Dict(newDict);
	}

	private (int, int) FindAndCountOperator(List<Token> tokens, T_Operator.Ops lookFor) {
		int count = 0;
		int index = -1;

		int depth = 0;
		for (int i = 0; i < tokens.Count; i++) {
			if (tokens[i] is T_Operator op) {
				switch (op.Value) {
					case T_Operator.Ops.CloseParentheses:
					case T_Operator.Ops.CloseBracket:
					case T_Operator.Ops.CloseBrace:
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
					case T_Operator.Ops.OpenParentheses:
					case T_Operator.Ops.OpenBracket:
					case T_Operator.Ops.OpenBrace:
						depth++;
						break;
				}
			}
		}

		return (index, count);
	}
}