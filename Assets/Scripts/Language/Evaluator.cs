using System.Collections.Generic;
using System.Linq;
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
		{ "!" , 5 }
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
		"!!"
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

	bool ExpressionContains(char symbol, string expr)
	{
		bool inString = false;
		foreach (char c in expr)
		{
			if (c == '"') inString = !inString;
			if (c == symbol && !inString) return true;
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
	bool ExpressionContainsParentheses(string expr)
	{
		return ExpressionContains('(', expr) || ExpressionContains(')', expr);
	}

	Output ParseFloatPart(string s, Interpreter interpreter)
	{
		float value = 0;
		try
		{
			value = float.Parse(s);
		}
		catch
		{ // could be a variable
			bool negative = s[0] == '-';
			if (negative) s = s[1..];

			Output result = interpreter.FetchVariable(s);
			string type = HF.DetermineTypeFromString(s);
			if (!result.success)
			{
				if (type != "number")
					return result;
				else
					return Errors.UnableToParseStrAsNum(s, interpreter);
			}
			else
			{
				dynamic variable = result.value;
				if (variable is float || variable is int)
				{
					value = (float)variable;
				}
				else if (variable is string)
				{
					try
					{
						value = float.Parse(variable);
					}
					catch
					{ // not a parseable float string, or a variable
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
				if (i == 0 || i == nospaces.Length - 1) // shouldnt be at start or end
					return false;

				if (nospaces[i + 1] == ',' || nospaces[i - 1] == ',') // shouldnt be next to each other
					return false;
			}
		}
		return true; // if paritycheck was false, would have already returned 
	}

	Output DynamicStringParse(string s, Interpreter interpreter)
	{
		string type = HF.DetermineTypeFromString(s);

		if (type == "malformed string") return Errors.MalformedString(s, interpreter);
		else if (type == "malformed list") return Errors.MalformedList(s, interpreter);

		dynamic value = null;

		if (type == "number")
		{
			Output result = ParseFloatPart(s, interpreter);
			if (!result.success) return result;
			value = result.value;
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
			if (!attemptR.success) return attemptR;

			value = attemptR.value;
		}
		else if (type == "variable")
		{
			Output result = interpreter.FetchVariable(s);
			if (!result.success) return result;
			value = result.value;
		}

		return new Output(value);
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
		float start;
		if (!string.IsNullOrEmpty(startString))
		{
			tryEval = Evaluate(startString, interpreter);
			if (!tryEval.success) return tryEval;
			if (tryEval.value is not float && tryEval.value is not int) return Errors.UnableToParseStrAsNum(startString, interpreter);
			start = tryEval.value;
		}
		else
			start = 0;

		float end;
		if (!string.IsNullOrEmpty(endString))
		{
			tryEval = Evaluate(endString, interpreter);
			if (!tryEval.success) return tryEval;
			if (tryEval.value is not float && tryEval.value is not int) return Errors.UnableToParseStrAsNum(endString, interpreter);
			end = tryEval.value;
		}
		else if (!isAlone)
			end = baseListLength - 1;
		else
			return Errors.TestError(interpreter);

		tryEval = Evaluate(intervalString, interpreter);
		if (!tryEval.success) return tryEval;
		if (tryEval.value is not float && tryEval.value is not int) return Errors.UnableToParseStrAsNum(intervalString, interpreter);
		float interval = tryEval.value;

		List<dynamic> values = new();
		if (isAlone)
		{
			if (start < 0) start = baseListLength + start;
			if (end < 0) end = baseListLength + end;
		}

		if (start < end)
			for (float v = start; v <= end; v += interval)
				values.Add(v);
		else
			for (float v = start; v >= end; v -= interval)
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
				if (!evaluate.success) return evaluate;

				items.Add(evaluate.value);
				accum = "";
			}
		}
		evaluate = Evaluate(accum.Trim(), interpreter);
		if (!evaluate.success) return evaluate;

		items.Add(evaluate.value);

		return new Output(items);
	}

	public Output EvaluateList(string expr, Interpreter interpreter)
	{
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
			if (!tryEval.success) return tryEval;
			List<dynamic> baseList = tryEval.value;

			parts.RemoveAt(0);

			// iteratively evaluate the next lists, using their returned items as indexes
			foreach (string part in parts)
			{
				tryEval = EvaluateSingularList(part, false, baseList.Count, interpreter);
				if (!tryEval.success) return tryEval;
				List<dynamic> dynamicIndexes = tryEval.value;

				// only whole numbers allowed in the indexes
				foreach (dynamic index in dynamicIndexes)
				{
					if (index is not float && index is not int)
						return Errors.IndexListWithType(HF.DetermineTypeFromVariable(index), interpreter);
					if (Mathf.Round(index) != index)
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
					temp.Add(baseList[index < 0 ? ^Mathf.Abs(index) : index]); // handle - indexes
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

	public Output Evaluate(string expr, Interpreter interpreter)
	{
		if (DEBUGMODE) interpreter.LogColor($"Evaluating {expr}", Color.blue);

		#region blankcheck
		if (expr.Replace(" ", "") == "")
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
		string functionName = null;
		inString = false;
		int depth = 0;
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
					if (checkingIndex == startIndex - 1) foundFunction = false;
					else foundFunction = true;
					break;
				}

				checkingIndex--;
			}
			if (foundFunction)
			{
				functionName = expr[checkingIndex..startIndex];

				if (interpreter.functions.ContainsKey(functionName)) // valid function
				{
					Output tryEval = interpreter.ExtractArgs(expr[checkingIndex..(endIndex+1)], functionName, interpreter.functions[functionName].ArgumentNames.Count);
					if (!tryEval.success) return tryEval;
					List<dynamic> args = tryEval.value;

					tryEval = interpreter.RunFunction(interpreter.functions[functionName], args);
					if (!tryEval.success) return tryEval;

					expr = HF.ReplaceSection(expr, checkingIndex, endIndex, HF.ConvertToString(tryEval.value));
				}
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
			if (!tryEval.success) return tryEval;
			string evaledvalue = HF.ConvertToString(tryEval.value);

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
							!char.IsDigit(expr[i - 1]) &&   // previous char is not a digit (operator)
							expr[i - 1] != '.';             // and previous char is not .
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
						return Errors.OperatorInBadPosition(op, interpreter);

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
					if (!evaluateboolean.success) return evaluateboolean;

					string newToken;
					if (evaluateboolean.value is string) newToken = !evaluateboolean.value;
					else newToken = evaluateboolean.value ? "false" : "true";
					temp.Add(newToken);
					i++;
				}
				else // shouldnt be at end
					return Errors.OperatorInBadPosition("!", interpreter);
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
				if (operators.Contains(tokenStrings[t])) return Errors.OperatorInBadPosition(tokenStrings[t], interpreter);
			}
			else
			{
				if (!operators.Contains(tokenStrings[t])) return Errors.OperatorInBadPosition(interpreter);
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
				if (!parsed.success) return parsed;

				tokens.Add(parsed.value);
			}
			else
				tokens.Add(tokenString);
		}
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
			if (lmhrIndex == tokens.Count - 1 || lmhrIndex == 0) return Errors.OperatorInBadPosition(tokens[^1], interpreter); // operators shouldnt be at end
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
	}
}