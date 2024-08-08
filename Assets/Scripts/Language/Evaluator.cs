using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;

public class Evaluator : MonoBehaviour
{
	public bool DEBUGMODE = false;

	readonly Dictionary<string, int> operatorRanks = new()
	{
		{ "+" , 1 },
		{ "-" , 1 },
		{ "*" , 2 },
		{ "/" , 2 },
		{ "^" , 3 },
		{ "%" , 3 },
		{ "==", 0 },
		{ "!=", 0 },
		{ "<" , 0 },
		{ ">" , 0 },
		{ "<=", 0 },
		{ ">=", 0 },
		{ "&&", 4 },
		{ "||", 4 },
		{ "!&", 4 },
		{ "&!", 4 },
		{ "!|", 4 },
		{ "|!", 4 },
		{ "!!", 4 },
		{ "!" , 5 },
		{ "." , 6 }
	};

	readonly string[] operators =
	{
		"+",
		"-",
		"*",
		"/",
		"^",
		"%",
		"==",
		"!=",
		"<",
		">",
		"<=",
		">=",
		"&&",
		"||",
		"!",
		"!&",
		//"&!",
		"!|",
		//"|!",
		"!!",
		"."
	};
	readonly char[] operatorFirstChars =
	{
		'+',
		'-',
		'*',
		'/',
		'^',
		'%',
		'=',
		'!',
		'<',
		'>',
		'&',
		'|',
		'.'
	};
	readonly string[] booleanOperators =
	{
		"==",
		"!=",
		"&&", //and
		"||", //or
		"!", //not
		"!&", //nand
		//"&!",
		"!|", //nor
		//"|!", //
		"!!"  //xor
	};

	public enum TokenType
	{
		val, // value
		op // operator, keyword is not allowed
		   //arg // arguments to a function (not sure if this will work)
	}
	public class Token
	{
		public string Text;
		public dynamic RealValue;
		public TokenType Type;

		public void Set(dynamic realValue, TokenType type)
		{
			RealValue = realValue;
			Text = HF.ConvertToString(realValue);
			Type = type;
		}

		public override string ToString()
		{
			return $"Token [{Text}] of type {Type}";
		}
	}

	public class Member
	{
		public delegate dynamic AttributeFunctionType(dynamic value); // assuming everything is done right, there should be no type issues
		public string For { get; private set; } // might not be used, just metadata
		public string Name { get; private set; }
		public bool IsFunction { get; private set; }
		public AttributeFunctionType AttributeFunction { get; private set; }
		public Function Function { get; private set; }
		public int ArgumentCount { get; private set; }

		public Member(string @for, string name, Function function = null, AttributeFunctionType attributeFunction = null, int numArgs = 1)
		{
			For = @for;
			Name = name;
			ArgumentCount = numArgs;
			if (attributeFunction == null) 
			{
				IsFunction = true;
				Function = function;
			}
			else
			{
				IsFunction = false;
				AttributeFunction = attributeFunction;
			}
		}
		public Output Invoke(List<dynamic> args, Interpreter interpreter)
		{
			if (args.Count != ArgumentCount)
				return Errors.NoFunctionExists(Name, ArgumentCount, interpreter);
			dynamic output = Function.DynamicInvoke(args);
			return new(output);
		}
	}

	#region builtin type classes

	public class Number
	{
		public static Output GetMember(double value, string membername, Interpreter interpreter)
		{
			if (!Members.ContainsKey(membername))
				return Errors.TypeHasNoAttribute(membername, "number", interpreter);

			Member member = Members[membername];
			if (member.IsFunction)
				return new(member.Function);
			else // you have to actually run the attribute function on the value to get the attribute result
			{
				dynamic output = member.AttributeFunction.Invoke(value);
				return new(output);
			}
		}

		static Dictionary<string, Member> Members = new()
		{
			{"pow", new("number", "pow", Pow, null, 1) },
			{"toString", new("number", "toString", null, StringFunction)}
		};

		static dynamic Pow(dynamic value, List<dynamic> args) // idk what static is doing in this case...
		{
			double a = value;
			double b = args[0];
			return Math.Pow(a, b);
		}
		static dynamic StringFunction(dynamic value) 
		{
			double doublevalue = (double)value;
			return doublevalue.ToString();
		}
	}

	#endregion

	bool ExpressionContainsSurfaceLevel(char symbol, string expr)
	{
		bool inString = false;
		int depth = 0;
		foreach (char c in expr)
		{
			if (c == '"') inString = !inString;
			if (!inString && (c == '(' || c == '[')) depth++;
			if (!inString && (c == ')' || c == ']')) depth--;

			if (c == symbol && !inString && depth == 0) return true;
		}
		return false;
	}
	int ExpressionCountSymbol(char symbol, string expr)
	{
		int count = 0;
		bool inString = false;
		foreach (char c in expr)
		{
			if (c == '"') inString = !inString;
			if (c == symbol && !inString) count++;
		}
		return count;
	}
	string RemoveNonStringSpace(string expr)
	{
		string tempstring = "";
		bool inString = false;
		foreach (char c in expr)
		{
			if (c == '"') inString = !inString;
			if (c != ' ' || inString) // anything but space unless in quotes
				tempstring += c;
		}
		return tempstring;
	}
	bool CheckListForm(string s) // returns if a list is properly formed, expects single lists, not chained or expressions
	{
		string nospaces = s.Replace(" ", "");
		if (nospaces[0] != '[' || nospaces[^1] != ']') return false;

		int depth = 0;
		bool instring = false;
		foreach (char c in s)
		{
			if (c == '"') instring = !instring;
			if (c == '[' && !instring) depth++;
			else if (c == ']' && !instring) depth--;
		}
		bool paritycheck = depth == 0; // well formed lists should have equal [ and ], therefore closed
		if (!paritycheck) return false;

		// commas should always have something between them
		nospaces = nospaces[1..^1]; // made sure started and ended with [], now remove them
		instring = false;
		bool inlist = false;
		for (int i = 0; i < nospaces.Length; i++)
		{
			char c = nospaces[i];
			if (c == '"') instring = !instring;
			if ((c == '[' || c == ']') && !instring) inlist = !inlist;
			if (nospaces[i] == ',' && !(instring || inlist)) // dont process commas inside of nested list or string
			{
				if (i == 0 || i == nospaces.Length - 1)// shouldnt be at start or end
					return false;
				
				if (nospaces[i + 1] == ',' || nospaces[i - 1] == ',') // shouldnt be next to each other
					return false;
			}
		}
		return true; // if paritycheck was false, would have already returned 
	}

	Output ParseNumberPart(string s, Interpreter interpreter)
	{
		double value = 0;
		try
		{
			value = double.Parse(s);
		}
		catch
		{ // could be a variable
			bool negative = s[0] == '-';
			if (negative) s = s[1..];

			Output result = interpreter.memory.Fetch(s);
			string type = HF.DetermineTypeFromString(s);
			if (!result.Success)
			{
				if (type != "number")
					return result;
				else
					return Errors.UnableToParseStrAsNum(s, interpreter);
			}
			else
			{
				dynamic variable = result.Value;
				if (variable is double v)
				{
					value = v;
				}
				else if (variable is string)
				{
					try
					{
						value = double.Parse(variable);
					}
					catch
					{ // not a parseable double string, or a variable
						return Errors.UnableToParseStrAsNum(value.ToString(), interpreter);
					}
				}
				if (negative)
				{
					value *= -1;
				}
			}
		}
		return new Output(value);
	}

	Output DynamicStringParse(string s, Interpreter interpreter)
	{
		if (s == "") return new(s);

		string type = HF.DetermineTypeFromString(s);

		if (type == "malformed string") return Errors.MalformedString(s, interpreter);
		else if (type == "malformed list") return Errors.MalformedList(s, interpreter);

		dynamic value = null;

		if (type == "number")
		{
			Output result = ParseNumberPart(s, interpreter);
			if (!result.Success) return result;
			value = result.Value;
		}
		else if (type == "string") value = s.Trim('"');
		else if (type == "bool")
		{
			if (s == "true") value = true;
			else if (s == "false") value = false;
			else return Errors.UnableToParseAsBool(s, interpreter);
		}
		else if (type == "list")
		{
			Output attemptR = EvaluateList(s, interpreter);
			if (!attemptR.Success) return attemptR;

			value = attemptR.Value;
		}
		else if (type == "variable")
		{
			Output result = interpreter.memory.Fetch(s);
			if (!result.Success) return result;
			value = result.Value;
		}

		return new Output(value);
	}
	Output ParityChecks(string expr, Interpreter interpreter)
	{
		bool inString = false;
		int parenthesesDepth = 0;
		int bracketDepth = 0;
		foreach (char c in expr)
		{
			if (c == '"') inString = !inString;
			if (!inString)
			{
				if (c == '(') parenthesesDepth++;
				else if (c == ')') parenthesesDepth--;
				else if (c == '[') bracketDepth++;
				else if (c == ']') bracketDepth--;

				if (parenthesesDepth <= -1) return Errors.MismatchedParentheses(interpreter);
				else if (bracketDepth <= -1) return Errors.MismatchedBrackets(interpreter);
			}
		}
		if (inString) return Errors.MismatchedQuotes(interpreter);
		return new(true);
	}
	public static Output DynamicCast(string typeFrom, string typeTo, dynamic value, Interpreter interpreter)
	{
		switch($"{typeFrom}-{typeTo}")
		{
			case "string-string":return new(value);
			case "string-number":return new(0f);
			case "string-bool":return new(false);
			case "string-list":return new(new List<dynamic> { value });
			case "number-string":return new(HF.ConvertToString(value));
			case "number-number":return new(value);
			case "number-bool":return new(false);
			case "number-list":return new(new List<dynamic> { value });
			case "bool-string":return new(HF.ConvertToString(value));
			case "bool-number":return new((bool)value ? 1f : 0f);
			case "bool-bool": return new(value);
			case "bool-list": return new(new List<dynamic> { value });
			case "list-string": return new(HF.ConvertToString(value));
			case "list-number": return new(0f);
			case "list-bool": return new(false);
			case "list-list": return new(value);
			default: return Errors.UnknownType(interpreter);
		}
	}

	Output EvaluateRangeList(string expr, bool isAlone, int baseListLength, Interpreter interpreter) // isalone determines behavior of range from positive to negative
	{ // expects well formed list (not checking again)
		string[] parts = expr.Split("..."); // there should only be one ... therefore two sides
		if (parts.Length > 2) return Errors.MalformedList(expr, interpreter);

		expr = expr[1..^1];
		parts = expr.Split("...");

		string startString = parts[0];
		string endString = parts[1];
		string intervalString = "1"; // default interval is 1
		int commaCount = ExpressionCountSymbol(',', startString);
		if (commaCount > 1)
			return Errors.MalformedList(expr, interpreter);
		else if (commaCount == 1)
		{
			intervalString = startString.Split(',')[1];
			startString = startString.Split(',')[0];
		}

		Output tryEval;
		double start;
		if (!string.IsNullOrEmpty(startString))
		{
			tryEval = Evaluate(startString, interpreter);
			if (!tryEval.Success) return tryEval;
			if (tryEval.Value is not double) return Errors.UnableToParseStrAsNum(startString, interpreter);
			start = tryEval.Value;
		}
		else
			start = 0;

		double end;
		if (!string.IsNullOrEmpty(endString))
		{
			tryEval = Evaluate(endString, interpreter);
			if (!tryEval.Success) return tryEval;
			if (tryEval.Value is not double) return Errors.UnableToParseStrAsNum(endString, interpreter);
			end = tryEval.Value;
		}
		else if (!isAlone)
			end = baseListLength - 1;
		else
			return Errors.TestError(interpreter);

		tryEval = Evaluate(intervalString, interpreter);
		if (!tryEval.Success) return tryEval;
		if (tryEval.Value is not double) return Errors.UnableToParseStrAsNum(intervalString, interpreter);
		double interval = tryEval.Value;

		List<dynamic> values = new();
		if (isAlone)
		{
			if (start < 0) start = baseListLength + start;
			if (end < 0) end = baseListLength + end;
		}

		if (start < end)
			for (double v = start; v <= end; v += interval)
				values.Add(v);
		else
			for (double v = start; v >= end; v -= interval)
				values.Add(v);

		return new Output(values);
	}
	public Output EvaluateSingularList(string expr, Interpreter interpreter)
	{
		return EvaluateSingularList(expr, true, 0, interpreter);
	}
	public Output EvaluateSingularList(string expr, bool isAlone, int baseListLength, Interpreter interpreter)
	{
		// check for well formed list
		if (!CheckListForm(expr)) return Errors.MalformedList(expr, interpreter);

		// check if this is a range list [x,y...z] [x...y]
		if (expr.Contains("...")) return EvaluateRangeList(expr, isAlone, baseListLength, interpreter);

		expr = expr[1..^1]; // trim first and last [ ] 

		List<dynamic> items = new();

		int depth = 0;
		string accum = "";
		bool inString = false;
		Output evaluate;
		for (int i = 0; i < expr.Length; i++)
		{
			char c = expr[i];

			if (c == '[') depth++;
			else if (c == ']') depth--;

			if (c == '"') inString = !inString;
			if (c != ',' || inString || depth != 0) // commas are allowed in strings 
			{
				accum += c;
			}
			else
			{ // , and not in string
				evaluate = Evaluate(accum.Trim(), interpreter);
				if (!evaluate.Success) return evaluate;

				items.Add(evaluate.Value);
				accum = "";
			}
		}
		evaluate = Evaluate(accum.Trim(), interpreter);
		if (!evaluate.Success) return evaluate;

		items.Add(evaluate.Value);

		return new Output(items);
	}
	public Output EvaluateList(string expr, Interpreter interpreter)
	{
		#region empty list check
		if (expr.Replace(" ", "") == "[]") return new(new List<dynamic>());
		#endregion

		#region extract the parts from the expression
		List<string> parts = new();
		bool inString = false;
		int depth = 0;
		string accum = "";
		for (int i = 0; i < expr.Length; i++)
		{
			char c = expr[i];

			if (c == ' ' && !inString) continue; // ignore spaces

			accum += c;

			if (c == '"') inString = !inString;
			else if (c == '[' && !inString) depth++;
			else if (c == ']' && !inString) depth--;

			// list ending or end of expresion, not in nested list or string
			if ((c == ']' || i == expr.Length - 1) && depth == 0 && !inString)
			{
				parts.Add(accum);
				accum = "";
			}
		}
		if (depth != 0) return Errors.MalformedList(expr, interpreter);
		//parts.Reverse();
		#endregion

		if (parts.Count > 1)
		{ // some kind of indexing is going on
		  // eval first part first

			Output tryEval = EvaluateSingularList(parts[0], interpreter);
			if (!tryEval.Success) return tryEval;
			List<dynamic> baseList = tryEval.Value;

			parts.RemoveAt(0);

			// iteratively evaluate the next lists, using their returned items as indexes
			foreach (string part in parts)
			{
				tryEval = EvaluateSingularList(part, false, baseList.Count, interpreter);
				if (!tryEval.Success) return tryEval;
				List<dynamic> dynamicIndexes = tryEval.Value;

				// only whole numbers allowed in the indexes
				foreach (dynamic index in dynamicIndexes)
				{
					if (index is not double)
						return Errors.IndexListWithType(HF.DetermineTypeFromVariable(index), interpreter);
					if (Math.Round(index) != index)
						return Errors.IndexListWithType(HF.DetermineTypeFromVariable(index), interpreter);
				}
				List<int> indexes = new();
				foreach (dynamic index in dynamicIndexes) indexes.Add((int)index);

				// indexes have to be in range
				foreach (int index in indexes)
					if (index < -baseList.Count || index >= baseList.Count) // negatives can get up to amount, cant go over length - 1
						return Errors.IndexOutOfRange(index, interpreter);

				// index the list
				List<dynamic> temp = new();
				foreach (int index in indexes)
					temp.Add(baseList[index < 0 ? ^Math.Abs(index) : index]); // handle - indexes
				baseList = temp;
			}

			return new Output(baseList);
		}
		else
		{ // no indexing, normal list
		  // return the one part 
			return EvaluateSingularList(expr, interpreter);
		}
	}
	public Output Tokenize(string expr, Interpreter interpreter)
	{
		List<Token> tokens = new();
		string accum = "";
		int i = 0;
		while (i < expr.Length) 
		{ 
			char c = expr[i];
			// some - are negative signs ugh just stole the old code jk now its acting up and im gonna fix it in the evaluator
			/*			bool minusIsNegative = false;
			if (c == '-') // handle - sign, annoying af
			{
				minusIsNegative = i == 0; // is first char
				if (!minusIsNegative)
					minusIsNegative = // case where previous operator 
						!(char.IsDigit(expr[i - 1]) ||	// previous char is not a digit (operator)
						char.IsLetter(expr[i - 1]) ||
						expr[i - 1] == '_' ||
						expr[i - 1] == '.');			// and previous char is not .
			}*/


			if (char.IsLetterOrDigit(c) || c == '_')// || minusIsNegative)
				//|| (c == '.' && ((i > 0 && char.IsDigit(expr[i-1])) || (i < expr.Length - 2 && char.IsDigit(expr[i-1]))))) // there has to be a better way than this
			{
				accum += c;
			}
			else if (c == '(' || c == '[') // parentheses and brackets are just their own entire token
			{ // entire thing is a token 
				char incChar = c;
				char decChar = c == '(' ? ')' : ']';

				if (accum != "") tokens.Add(new() { Text = accum, Type = TokenType.val }); // not sure what was before this, but add it to not lose it
				accum = "";
				int depth = 0;
				bool deeperInString = false;
				while (i < expr.Length)
				{
					c = expr[i];
					if (c == '"') deeperInString = !deeperInString;
					if (!deeperInString)
					{
						if (c == incChar) depth++;
						else if (c == decChar) depth--;
					}
					accum += c;
					if (depth == 0) break;
					i++;
				}

				if (i >= expr.Length) // should not have broken by i being too high, at some point should have broken early
				{
					if (incChar == '(') return Errors.MismatchedParentheses(interpreter);
					else return Errors.MismatchedBrackets(interpreter);
				}

				tokens.Add(new() { Text = accum, Type = TokenType.val });
				accum = "";
			}
			else if (c == '"') // string special eval lest bullshit
			{
				if (accum != "") tokens.Add(new() { Text = accum, Type = TokenType.val }); // not sure what was before this, but add it to not lose it
				int startI = i;
				accum = "";
				while (i < expr.Length)
				{
					c = expr[i];
					accum += c;
					if (c == '"' && i != startI) break;
					i++;
				}

				if (i >= expr.Length) // should not have broken by i being too high, at some point should have broken early
					return Errors.MismatchedQuotes(interpreter);

				tokens.Add(new() { Text = accum, Type = TokenType.val });
				accum = "";
			}
			else if (operatorFirstChars.Contains(c))
			{
				if (accum != "") tokens.Add(new() { Text = accum, Type = TokenType.val }); // not sure what was before this, but add it to not lose it
				accum = "";

				string operation = c.ToString();

				if (i != expr.Length - 1)
				{
					char next = expr[i + 1];
					foreach (string op in operators)
					{
						if (new string(c, next) == op)
						{
							operation = op;
							break;
						}
					}
				}

				if (!operators.Contains(operation))
					return Errors.OperatorDoesntExist(operation, interpreter);

				tokens.Add(new() { Text = operation, Type = TokenType.op });
			}
			else
			{
				return Errors.UnknownSymbol(c, interpreter);
			}
			i++;
		}
		if (accum != "") tokens.Add(new() { Text = accum, Type = TokenType.val }); // not sure what was before this, but add it to not lose it

		return new(tokens);
	}

	public Output HandleMembers(string type, dynamic left, string memberName, Interpreter interpreter)
	{
		if (type == "ClassInstance")
		{
			ClassInstance classInstance = (ClassInstance)left;

			return classInstance.OwnInterpreter.memory.Fetch(memberName);
		}

		switch (type)
		{
			case "number": return Number.GetMember(left, memberName, interpreter);
			case "string":
			case "boolean":
			case "list":
			default:
				return Errors.TypeHasNoMethod(memberName, type, interpreter);
		}
	}

	public Output Evaluate(string expr, Interpreter interpreter)
	{
		if (DEBUGMODE) interpreter.LogColor($"Evaluating {expr}", Color.blue);

		// can't evaluate nothing
		if (string.IsNullOrWhiteSpace(expr)) return new(""); // return Errors.EvaluatedNothing(interpreter);

		// get rid of whitespace
		expr = RemoveNonStringSpace(expr);

		// pre check(s)
		Output parityCheck = ParityChecks(expr, interpreter);
		if (!parityCheck.Success) return parityCheck;

		// evaluate straight lists
		if (ExpressionContainsSurfaceLevel(',', expr))
			return EvaluateList('[' + expr + ']', interpreter);

		// tokenize the expression 
		Output tokenize = Tokenize(expr, interpreter);
		if (!tokenize.Success) return tokenize;
		List<Token> tokens = tokenize.Value;

		if (tokens.Count == 1)
		{
			if (expr.StartsWith('(') && expr.EndsWith(')'))
				return Evaluate(expr[1..^1], interpreter); // single values in parentheses get evaled

			return DynamicStringParse(expr, interpreter);
		}

		// get the actual value of each value token (not operation, they don't need to be evaluated)
		foreach (Token token in tokens) if (token.Type == TokenType.val)
			{
				if (token.Text.StartsWith('(')) token.Text = token.Text[1..];
				if (token.Text.EndsWith(')')) token.Text = token.Text[..^1];

				// variables are treated differently
				if (HF.DetermineTypeFromString(token.Text) == "variable")
				{
					Variable variable = new(token.Text, null);
					token.Set(variable, TokenType.val);
				}
				else
				{
					Output tryEval = Evaluate(token.Text, interpreter); // TODO: this is not gonna end well
					if (!tryEval.Success) return tryEval;
					token.Set(tryEval.Value, TokenType.val);
				}
			}

		
		// evaluate with pemdas
		while (tokens.Count > 1)
		{
			// evaluate all functions and list indexing before everything else 
			bool doublesExist = true; // side by side values
			while (doublesExist)
			{
				// find doubles location
				int doubleStartIndex = -1;
				doublesExist = false;
				for (int i = 0; i < tokens.Count; i++)
				{
					if ( // condition for function or list indexing (not method)
						i < tokens.Count - 1 &&
						tokens[i].Type == TokenType.val && tokens[i + 1].Type == TokenType.val && // value followed by argument?
						((i > 0 && tokens[i - 1].Text != ".") || i == 0) // prev token isn't . ?  
						)
					{
						doublesExist = true;
						doubleStartIndex = i;
						break;
					}
				}

				if (doublesExist)
				{
					int nextIndex = doubleStartIndex + 1;
					// differentiate between list indexing and arguments
					if (tokens[nextIndex].Text.StartsWith('[') && tokens[nextIndex].Text.EndsWith(']')) // indexing
					{
						// chained indexing should hopefully be handled implicitly as the first instance is always processed first
						// only need to handle two together

						if (tokens[nextIndex].RealValue is not List<dynamic>) // how would this happen?
							return Errors.UnknownError(interpreter);

						List<dynamic> listToIndex = tokens[doubleStartIndex].RealValue is List<dynamic> ?
							tokens[doubleStartIndex].RealValue :
							new List<dynamic>() { tokens[doubleStartIndex].RealValue };
						List<dynamic> indexList = tokens[nextIndex].RealValue;

						List<dynamic> newList = new();
						foreach (dynamic index in indexList)
						{
							if (index is not double || Math.Round(index) != index)
								return Errors.IndexListWithType(HF.DetermineTypeFromVariable(index), interpreter);

							int intIndex = (int)index;
							if (intIndex >= listToIndex.Count)
								return Errors.IndexOutOfRange(intIndex, interpreter);

							newList.Add(listToIndex[intIndex]);
						}

						tokens.RemoveAt(nextIndex); // remove index list
						tokens[doubleStartIndex].Set(newList, TokenType.val); // update list
					}
					else
					{
						Token functionToken = tokens[doubleStartIndex];
						Token argumentToken = tokens[nextIndex];

						string functionName = functionToken.Text;

						List<dynamic> args = argumentToken.RealValue is List<dynamic> ?
							argumentToken.RealValue :
							new List<dynamic>() { argumentToken.RealValue };
						int numargs = args.Count;

						string functionTokenType = HF.DetermineTypeFromVariable(functionToken.RealValue);
						if (functionTokenType == "function")
						{
							Dictionary<string, Function> functions = interpreter.memory.GetFunctions();
							if (!functions.ContainsKey(functionName))
								return Errors.NoFunctionExists(name, numargs, interpreter);
							Function function = functions[functionName];

							Output output = function.Run(args, interpreter);
							if (!output.Success) return output;

							tokens.RemoveAt(nextIndex); // remove arg token
							tokens[doubleStartIndex].Set(output.Value, TokenType.val); // set the function vallue accordingly
						}
						else
						{
							return Errors.NoFunctionExists(functionToken.Text, numargs, interpreter);
						}
					}
				}
			}


			#region find leftmost highest ranking operator
			int opIndex = -1;
			int highestRank = int.MinValue;
			for (int i = 0; i < tokens.Count; i++)
			{
				Token token = tokens[i];
				if (token.Type == TokenType.op)
				{
					int rank = operatorRanks[token.Text];
					if (rank > highestRank)
					{
						opIndex = i;
						highestRank = rank;
					}
				}
			}
			#endregion

			if (opIndex == -1) return Errors.NoOperator(interpreter);

			string[] specialOperators = new string[2] { "!", "." };
			string operation = tokens[opIndex].Text;
			if ((opIndex == 0 || opIndex == tokens.Count - 1 || // not outside range
				tokens[opIndex - 1].Type == TokenType.op || tokens[opIndex + 1].Type == TokenType.op) && // left or right aren't operators
				!specialOperators.Contains(operation)) // special operators get exception
				return Errors.OperatorInInvalidPosition(operation, interpreter);

			Token leftToken = opIndex > 0 ? tokens[opIndex - 1] : new Token() { RealValue = 0 };
			Token rightToken = opIndex < (tokens.Count - 1) ? tokens[opIndex + 1] : new Token() { RealValue = 0 };

			bool leftWasntValid = false; // for future reference for . to know how to handle left and right side
			bool rightWasntValid = false;
			if (opIndex == 0 || tokens[opIndex - 1].Type == TokenType.op)
			{ // special case for ., would have been caught normally
				leftToken = new Token() { RealValue = 0d };
				leftWasntValid = true;
			}
			if (opIndex == tokens.Count - 1 || tokens[opIndex + 1].Type == TokenType.op)
			{
				rightToken = new Token() { RealValue = 0d };
				rightWasntValid = true;
			}

			dynamic left = leftToken.RealValue;
			dynamic right = rightToken.RealValue;
			string leftType = HF.DetermineTypeFromVariable(left);
			string rightType = HF.DetermineTypeFromVariable(right);
			dynamic result = 0;

			bool normaloperation = true;

			if (operation == "!")
			{
				if (rightToken == null) return Errors.OperatorInInvalidPosition("!", interpreter);

				bool boolvalue = false;
				if (rightType == "string") boolvalue = right == "true";
				else if (rightType == "number") boolvalue = right > 0;
				else if (rightType == "boolean") boolvalue = right;
				else if (rightType == "list") boolvalue = false;

				tokens.RemoveAt(opIndex);
				tokens[opIndex].Set(!boolvalue, TokenType.val);

				normaloperation = false;
			}
			else if (operation == ".")
			{
				if (leftType == "number" && rightType == "number") // decimal (will have unintended consequences)
				{
					double rightDigits = right != 0 ? Math.Floor(Math.Log10(right) + 1) : 1;
					result = left + right / Math.Pow(10, rightDigits);

					if (leftWasntValid)
					{
						tokens[opIndex + 1].Set(result, TokenType.val); // replace original value with result
						tokens.RemoveAt(opIndex); // remove .
					}
					else if (rightWasntValid)
					{
						tokens[opIndex - 1].Set(result, TokenType.val); // replace original value with result
						tokens.RemoveAt(opIndex); // remove .
					}
					else 
					{
						tokens.RemoveAt(opIndex + 1); // remove right side
						tokens.RemoveAt(opIndex); // remove .
						tokens[opIndex - 1].Set(result, TokenType.val); // replace original value with result
					}
				}
				else if (rightType == "variable") // optimally want to be able to just check for method/variable
				{
					Variable rightVar = (Variable)right;
					Output attemptHandleMember = HandleMembers(leftType, left, rightVar.Name, interpreter);
					if (!attemptHandleMember.Success) return attemptHandleMember;

					tokens.RemoveAt(opIndex + 1); // remove right side
					tokens.RemoveAt(opIndex); // remove .
					tokens[opIndex - 1].Set(attemptHandleMember.Value, TokenType.val); // replace original value with result
				}
				else
				{
					return Errors.InvalidUseOfPeriod(interpreter);
				}

				normaloperation = false;
			}
			
			if (normaloperation) {
				try
				{
					if (leftType == "number")
					{
						if (rightType != "number")
						{ // attempt to convert to number
							if (rightType == "string")
							{
								try
								{
									right = double.Parse(right.Trim('"'));
								}
								catch
								{
									return Errors.UnableToParseStrAsNum(right, interpreter);
								}
							}
							else if (rightType == "bool") right = right ? 1 : 0;
							else if (rightType == "list") right = 0;
						}

						switch (operation)
						{
							case "+":	result = left + right; break;
							case "-":	result = left - right; break;
							case "*":	result = left * right; break;
							case "/":  // check for division by zero
								if (left == 0 || right == 0) return Errors.DivisionByZero(interpreter);
								else	result = left / right; break;
							case "^":	result = Math.Pow(left, right); break;
							case "%":	result = left % right; break; //
							case "==":	result = left == right; break;
							case "!=":	result = left != right; break;
							case "<":	result = left < right; break;
							case ">":	result = left > right; break;
							case "<=":	result = left <= right; break;
							case ">=":	result = left >= right; break;
							default:	return Errors.UnsupportedOperation(operation, "number", rightType, interpreter);
						}
					}
					else if (leftType == "string")
					{
						if (operation != "+") return Errors.UnsupportedOperation(operation, "string", rightType, interpreter);
						if (rightType != "string") right = HF.ConvertToString(right);

						result = left + right;
					}
					else if (leftType == "list")
					{
						if (operation != "+") return Errors.UnsupportedOperation(operation, "list", rightType, interpreter);
						if (rightType == "list")
							left.AddRange(right);
						else
							left.Add(right);
						result = left;
					}
					else if (leftType == "bool")
					{
						if (!booleanOperators.Contains(operation)) return Errors.UnsupportedOperation(operation, "bool", rightType, interpreter);
						if (rightType != "bool")
						{
							if (rightType == "string") right = right == "true";
							else if (rightType == "number") right = right > 0;
							else if (rightType == "list") right = false;
						}
						switch (operation)
						{
							case "==": result = left == right; break;
							case "!=": result = left != right; break;
							case "&&": result = left && right; break;
							case "||": result = left || right; break;
							case "!&": result = !(left && right); break;
							case "!|": result = !(left || right); break;
							case "!!": result = left ^ right; break;
						}
					}
				}
				catch
				{
					return Errors.OperationFailed($"{left}{operation}{right}", interpreter);
				}
				tokens.RemoveAt(opIndex + 1); // remove right side
				tokens[opIndex] = new() { RealValue = result }; // replace operator with result
				tokens.RemoveAt(opIndex - 1); // remove left
			}

		}

		return new(tokens[0].RealValue);
	}

	/*public Output _Evaluate(string expr, Interpreter interpreter)
	{
		if (DEBUGMODE) interpreter.LogColor($"Evaluating {expr}", Color.blue);

		#region blankcheck
		if (string.IsNullOrWhiteSpace(expr))
			return Errors.EvaluatedNothing(interpreter);
		#endregion

		#region remove all spaces except inside ""
		string tempstring = "";
		bool inString = false;
		foreach (char c in expr)
		{
			if (c == '"') inString = !inString;
			if (c != ' ' || inString) // anything but space unless in quotes
				tempstring += c;
		}
		expr = tempstring;
		#endregion

		#region function check 
		// look for all functions, replace with evaluated output, or error if fail
		bool foundFunction = expr.IndexOf('(') != -1; // kinda jank? idk
		string functionName;
		int depth;
		while (foundFunction)
		{
			// find the first valid set of parentheses
			inString = false;
			depth = 0;
			foundFunction = false;
			int startIndex = -1;
			int endIndex = -1;
			for (int i = 0; i < expr.Length; i++)
			{
				char c = expr[i];
				if (c == '"') inString = !inString;
				else if (!inString && c == '(')
				{
					if (depth == 0) startIndex = i;
					depth++;
				}
				else if (!inString && c == ')')
				{
					depth--;
					if (depth == 0) { endIndex = i; break; }
				}
			}

			// find closest index of operator
			int checkingIndex = startIndex - 1;
			bool curIndexIsNotPartOfName = false; // if char at this index or this and char before is an operator
			while (checkingIndex >= 0)
			{
				if (startIndex != -1 && endIndex != -1)
				{
					if ((checkingIndex >= 1 && operators.Contains(expr[checkingIndex].ToString()))
						|| (checkingIndex >= 2 && operators.Contains(expr.Substring(checkingIndex - 1, 2)))
						|| !(char.IsLetterOrDigit(expr[checkingIndex]) || expr[checkingIndex] == '_'))
						curIndexIsNotPartOfName = true;
				}

				if (curIndexIsNotPartOfName)
				{
					if (checkingIndex == startIndex - 1) // operator immediately before, this is not a function
						foundFunction = false;
					else
						foundFunction = true;

					checkingIndex++;
					break;
				}
				else if (checkingIndex == 0) // if it breaks because hit 0 then include 0
				{
					if (checkingIndex == startIndex) foundFunction = false;
					else foundFunction = true;
					break;
				}

				checkingIndex--;
			}
			if (foundFunction)
			{
				functionName = expr[checkingIndex..startIndex];

				Dictionary<string, Function> functions = interpreter.GetFunctions();

				if (!functions.ContainsKey(functionName))
					return Errors.UnknownFunction(functionName, interpreter);

				Output tryEval = interpreter.ExtractArgs(expr[checkingIndex..(endIndex+1)], functionName, functions[functionName].ArgumentNames.Count);
				if (!tryEval.Success) return tryEval;
				List<dynamic> args = tryEval.Value;

				tryEval = interpreter.RunFunction(functions[functionName], args);
				if (!tryEval.Success) return tryEval;
				if (tryEval.Value is ClassInstance) return new(tryEval.Value); // classes are immediately returned

				expr = HF.ReplaceSection(expr, checkingIndex, endIndex, HF.ConvertToString(tryEval.Value));

			}
		}
		#endregion

		#region handle parentheses
		while (ExpressionContainsParentheses(expr))
		{
			// find the first instance of ( not inside of string
			int parenthesesStartIndex = 0;
			inString = false;
			for (int i = 0; i < expr.Length; i++)
			{
				if (expr[i] == '"') inString = !inString;
				if (expr[i] == '(' && !inString)
				{
					parenthesesStartIndex = i;
					break;
				}
			}
			int parenthesesEndIndex = -1;

			// search for matching parentheses
			depth = 0;
			inString = false;
			for (int i = parenthesesStartIndex; i < expr.Length; i++)
			{
				char c = expr[i];
				if (c == '"') inString = !inString;
				if (c == '(' && !inString) depth++;
				if (c == ')' && !inString)
				{
					depth--;
					if (depth == 0)
					{
						parenthesesEndIndex = i;
						break;
					}
				}
			}

			// either missing ( or ) isnt closed properly
			if (parenthesesStartIndex == -1 || parenthesesEndIndex == -1)
				return Errors.MismatchedParentheses(interpreter);

			string newexpr = expr.Substring(parenthesesStartIndex + 1, parenthesesEndIndex - parenthesesStartIndex - 1);

			Output tryEval = Evaluate(newexpr, interpreter);
			if (!tryEval.Success) return tryEval;
			string evaledvalue = HF.ConvertToString(tryEval.Value);

			// replace the chunk of parentheses with the evalled value 
			expr = HF.ReplaceSection(expr, parenthesesStartIndex, parenthesesEndIndex, evaledvalue);
		}
		#endregion

		#region tokenize the expression
		List<string> tokenStrings = new();
		string accum = "";
		inString = false;
		bool exitGrace = false; // this is because when you exit string or list, it wants to interpret final char as operator
		int listDepth = 0;
		for (int i = 0; i < expr.Length; i++)
		{
			char c = expr[i];

			if (c == '"') { inString = !inString; exitGrace = true; }
			else if (c == '[' && !inString) listDepth++;
			else if (c == ']' && !inString) { listDepth--; exitGrace = true; }

			if (char.IsDigit(c) || c == '.' || char.IsLetter(c) // digits, . and letters are never operators
				|| inString || exitGrace // also ignore anything when in a string
				|| listDepth != 0) // or if in a list
			{
				accum += c;
				exitGrace = false;
			}
			else
			{
				bool minusIsNegative = false;
				if (c == '-') // handle - sign, annoying af
				{
					minusIsNegative = i == 0; // is first char
					if (!minusIsNegative)
						minusIsNegative = // case where previous operator 
							!(char.IsDigit(expr[i - 1]) ||   // previous char is not a digit (operator)
							char.IsLetter(expr[i - 1]) ||
							expr[i - 1] == '_' ||
							expr[i - 1] == '.');             // and previous char is not .
				}

				if (minusIsNegative)
				{
					accum += c;
				}
				else
				{
					if (accum.Length > 0)
						tokenStrings.Add(accum);
					accum = "";

					string op = c.ToString();
					// handle 2 len operators
					if (i != expr.Length - 1)
					{
						char next = expr[i + 1];
						if (operators.Contains(c.ToString() + next.ToString())) // would adding the next char still make it an operator
						{
							i++; // already checked to see if last, shouldnt break hopefully
							op += expr[i]; // add next char to the operator
						}
					}
					else // additional check can be added, operators shouldnt be at end
						return Errors.OperatorInInvalidPosition(op, interpreter);

					if (operators.Contains(op))
					{ // valid operator
						tokenStrings.Add(op);
					}
					else
						return Errors.OperatorDoesntExist(op, interpreter);
				}
			}
		}
		tokenStrings.Add(accum);
		#endregion

		#region handle ! modifier
		List<string> temp = new();
		for (int i = 0; i < tokenStrings.Count; i++)
		{
			if (tokenStrings[i] == "!")
			{
				if (i != tokenStrings.Count - 1)
				{
					Output evaluateboolean = Evaluate(tokenStrings[i + 1], interpreter);
					if (!evaluateboolean.Success) return evaluateboolean;

					string newToken;
					if (evaluateboolean.Value is string) newToken = !evaluateboolean.Value;
					else newToken = evaluateboolean.Value ? "false" : "true";
					temp.Add(newToken);
					i++;
				}
				else // shouldnt be at end
					return Errors.OperatorInInvalidPosition("!", interpreter);
			}
			else
			{
				temp.Add(tokenStrings[i]);
			}
		}
		tokenStrings = temp;
		#endregion

		#region check if operators are in correct positions (odd indices), not in front
		for (int t = 0; t < tokenStrings.Count; t++)
		{
			if (t % 2 == 0)
			{
				if (operators.Contains(tokenStrings[t])) return Errors.OperatorInInvalidPosition(tokenStrings[t], interpreter);
			}
			else
			{
				if (!operators.Contains(tokenStrings[t])) return Errors.OperatorInInvalidPosition(interpreter);
			}
		}
		#endregion

		#region parse non operation parts
		List<dynamic> tokens = new();
		foreach (string tokenString in tokenStrings)
		{
			if (!operators.Contains(tokenString))
			{
				Output parsed = DynamicStringParse(tokenString, interpreter);
				if (!parsed.Success) return parsed;

				tokens.Add(parsed.Value);
			}
			else
				tokens.Add(tokenString);
		}
		#endregion

		#region convert special types to string
		List<dynamic> converted = new();
		foreach (dynamic token in tokens)
		{
			if (token is ClassInstance || token is ClassDefinition) converted.Add(token.ToString());
			else converted.Add(token);
		}
		tokens = converted;
		#endregion

		#region iteratively evaluate with pemdas
		while (tokens.Count > 1)
		{
			#region find the index of left most and highest ranking operator
			int lmhrIndex = -1;
			int lmhrRank = -1;
			for (int t = 1; t < tokens.Count; t += 2)
			{ // premature check already confirmed all odd indices are ops
				int rank = operatorRanks[tokens[t]];
				if (rank > lmhrRank)
				{
					lmhrRank = rank;
					lmhrIndex = t;
				}
			}

			if (lmhrIndex == -1) return Errors.Error("how.", interpreter); // code should have found at least one op
			if (lmhrIndex == tokens.Count - 1 || lmhrIndex == 0) return Errors.OperatorInInvalidPosition(tokens[^1], interpreter); // operators shouldnt be at end
			#endregion

			// operator definitely has items on both sides
			// left and right types
			dynamic left = tokens[lmhrIndex - 1];
			string leftType = HF.DetermineTypeFromVariable(left);

			dynamic right = tokens[lmhrIndex + 1];
			string rightType = HF.DetermineTypeFromVariable(right);

			string operation = tokens[lmhrIndex];
			dynamic result = 0;

			try
			{
				if (leftType == "number")
				{
					if (rightType != "number")
					{ // attempt to convert to number
						if (rightType == "string")
						{
							try
							{
								right = float.Parse(right.Trim('"'));
							}
							catch
							{
								return Errors.UnableToParseStrAsNum(right, interpreter);
							}
						}
						else if (rightType == "bool")
						{
							right = right ? 1 : 0;
						}
						else if (rightType == "list") return Errors.UnsupportedOperation(operation, "number", "list", interpreter);
					}

					switch (operation)
					{
						case "+": result = left + right; break;
						case "-": result = left - right; break;
						case "*": result = left * right; break;
						case "/":  // check for division by zero
							if (left == 0 || right == 0) return Errors.DivisionByZero(interpreter);
							else result = left / right; break;
						case "^": result = Mathf.Pow(left, right); break;
						case "%": result = left % right; break; //
						case "==": result = left == right; break;
						case "!=": result = left != right; break;
						case "<": result = left < right; break;
						case ">": result = left > right; break;
						case "<=": result = left <= right; break;
						case ">=": result = left >= right; break;
						default: return Errors.UnsupportedOperation(operation, "number", rightType, interpreter);
					}
				}
				else if (leftType == "string")
				{
					if (operation != "+") return Errors.UnsupportedOperation(operation, "string", rightType, interpreter);
					if (rightType != "string") right = HF.ConvertToString(right);

					result = left + right;
				}
				else if (leftType == "list")
				{
					if (operation != "+") return Errors.UnsupportedOperation(operation, "list", rightType, interpreter);
					if (rightType == "list")
						left.AddRange(right);
					else
						left.Add(right);
					result = left;
				}
				else if (leftType == "bool")
				{
					if (!booleanOperators.Contains(operation)) return Errors.UnsupportedOperation(operation, "bool", rightType, interpreter);
					if (rightType != "bool")
					{
						if (rightType == "string")
						{
							if (right == "true") right = true;
							else if (right == "false") right = false;
							else return Errors.UnsupportedOperation(operation, "bool", "string", interpreter);
						}
						else if (rightType == "number")
						{
							if (right == 1) right = true;
							else if (right == 0) right = false;
							else return Errors.UnsupportedOperation(operation, "bool", "number", interpreter);
						}
						else if (rightType == "list") return Errors.UnsupportedOperation(operation, "bool", "list", interpreter);
					}
					switch (operation)
					{
						case "==": result = left == right; break;
						case "!=": result = left != right; break;
						case "&&": result = left && right; break;
						case "||": result = left || right; break;
						case "!&": result = !(left && right); break;
						case "!|": result = !(left || right); break;
						case "!!": result = left ^ right; break;
					}
				}
			}
			catch
			{
				return Errors.OperationFailed($"{left}{operation}{right}", interpreter);
			}

			tokens.RemoveAt(lmhrIndex + 1); // remove right side
			tokens[lmhrIndex] = result; // replace operator with result
			tokens.RemoveAt(lmhrIndex - 1); // remove left
		}
		#endregion

		return new Output(tokens[0]);
	}*/
}