using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Evaluator : MonoBehaviour
{
	readonly Dictionary<string, int> operatorRanks = new()
	{
		{ "+" , 0 },
		{ "-" , 0 },
		{ "*" , 1 },
		{ "/" , 1 },
		{ "^" , 2 },
		{ "%" , 2 },
		{ "==", 3 },
		{ "!=", 3 },
		{ "<" , 3 },
		{ ">" , 3 },
		{ "<=", 3 },
		{ ">=", 3 },
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

	void Log(string s)
	{
		Debug.Log(s);
	}
	string ReplaceSection(string original, int startIndex, int endIndex, string replaceWith)
	{
		return original[..startIndex] + replaceWith + original[(endIndex + 1)..];
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
			if (!result.success) { return new Output(result.error); }
			else
			{
				dynamic variable = result.value;
				if (variable is float || variable is int)
				{
					value = (float)variable;
				}
				else if (variable is string)
				{
					try {
						value = float.Parse(variable);
					}
					catch { // not a parseable float string, or a variable
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

	bool CheckListForm(string s)
	{
		int depth = 0;
		foreach(char c in s)
		{
			if (c == '[') depth++;
			else if (c == ']') depth--;
		}
		return depth == 0; // well formed lists should have equal [ and ], therefore closed
	}

	string DetermineTypeFromString(string s)
	{
		if (s.Length == 0) return null;

		if (s[0] == '"' && s[^1] == '"') return "string";
		else if (s[0] == '"' && s[^1] != '"' || s[0] != '"' && s[^1] == '"') return "malformed string"; // start is " but not end, or end is " but not start
		
		if (s[0] == '[' && s[^1] == ']') return "list";
		else if (s[0] == '[' && s[^1] != ']' || s[0] != '[' && s[^1] == ']') return "malformed list"; // start is " but not end, or end is " but not start

		bool isnumber = true;
		foreach (char c in s) if (!(char.IsDigit(c) || c == '.' || c == '-')) isnumber = false;
		if (isnumber) return "number";

		if (s == "true" || s == "false") return "bool";
		return "variable"; //TODO!!!!!!!!!!
	}

	string DetermineTypeFromVariable(dynamic v)
	{
		if (v is string) return "string";
		else if (v is int || v is float || v is long) return "number";
		else if (v is bool) return "bool";
		else if (v is List<dynamic>) return "list";
		return "unknown";
	}

	Output DynamicStringParse(string s, Interpreter interpreter)
	{
		string type = DetermineTypeFromString(s);

		if (type == "malformed string") return Errors.MalformedString(s, interpreter);
		else if (type == "malformed list") return Errors.MalformedList(s, interpreter);

		dynamic value = null;

		if (type == "number")
		{
			Output result = ParseFloatPart(s, interpreter);
			if (!result.success) return new Output(result.error);
			value = result.value;
		}
		else if (type == "string") value = s.Trim('"');
		else if (type == "bool") value = s == "true";
		else if (type == "list")
		{
			Output attemptR = EvaluateList(s, interpreter);
			if (!attemptR.success) return new Output(attemptR.error);

			value = attemptR.value;
		}
		else if (type == "variable")
		{
			Output result = interpreter.FetchVariable(s);
			if (!result.success) return new Output(result.error);
			value = result.value;
		}

		return new Output(value);
	}

	public Output EvaluateList(string expr, Interpreter interpreter)
	{
		if (!CheckListForm(expr)) return Errors.MalformedList(expr, interpreter);
		expr = expr[1..^1];

		List<dynamic> items = new();
		
		bool inString = false;
		int depth = 0;
		string accum = "";
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
				if (!evaluate.success) return new Output(evaluate.error);

				items.Add(evaluate.value);
				accum = "";
			}
		}
		evaluate = Evaluate(accum.Trim(), interpreter);
		if (!evaluate.success) return new Output(evaluate.error);

		items.Add(evaluate.value);

		return new Output(items);
	}

	public string ConvertToString(dynamic value)
	{
		if (value is string) return $"\"{value}\"";
		else if (value is int || value is float || value is bool) return value.ToString();
		else if (value is List<dynamic>)
		{
			string builtString = "[";
			for (int i = 0; i < value.Count; i++)
			{
				builtString += ConvertToString(value[i]);
				if (i < value.Count - 1) builtString += ", ";
			}
			builtString += "]";
			return builtString;
		}
		return value.ToString();
	}

	public Output Evaluate(string expr, Interpreter interpreter)
	{
		#region remove all spaces except inside ""
		string tempstring = "";
		bool inQuotes = false;
		foreach(char c in expr)
		{
			if (c == '"') inQuotes = !inQuotes;
			if (c != ' ' || inQuotes) // anything but space unless in quotes
				tempstring += c;
		}
		expr = tempstring;
		#endregion

		#region handle parentheses
		while (expr.Contains('(') || expr.Contains(')'))
		{
			// find the first instance of (
			int parenthesesStartIndex = expr.IndexOf('(');
			int parenthesesEndIndex = -1;

			// search for matching parentheses
			int depth = 0;
			for (int i = parenthesesStartIndex; i < expr.Length; i++)
			{
				char c = expr[i];
				if (c == '(') depth++;
				if (c == ')')
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
			string evaledvalue = ConvertToString(Evaluate(newexpr, interpreter).value);

			// replace the chunk of parentheses with the evalled value 
			expr = ReplaceSection(expr, parenthesesStartIndex, parenthesesEndIndex, evaledvalue);
			Log(expr);
		}
		#endregion

		#region tokenize the expression
		List<string> tokenStrings = new();
		string accum = "";
		bool instring = false;
		bool exitGrace = false; // this is because when you exit string or list, it wants to interpret final char as operator
		int listDepth = 0;
		for (int i = 0; i < expr.Length; i++)
		{
			char c = expr[i];

			if (c == '"') { instring = !instring; exitGrace = true; }
			else if (c == '[') listDepth++;
			else if (c == ']') { listDepth--; exitGrace = true; }

			if (char.IsDigit(c) || c == '.' || char.IsLetter(c) // digits, . and letters are never operators
				|| instring || exitGrace // also ignore anything when in a string
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
						if (operators.Contains(c.ToString()+next.ToString())) // would adding the next char still make it an operator
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
					if (!evaluateboolean.success) return new Output(evaluateboolean.error);

					string newToken;
					if (evaluateboolean.value is string) newToken = evaluateboolean.value;
					else newToken = evaluateboolean.value ? "true" : "false";
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
				if (!parsed.success) return new Output(parsed.error);

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
			string leftType = DetermineTypeFromVariable(left);

			dynamic right = tokens[lmhrIndex + 1];
			string rightType = DetermineTypeFromVariable(right);

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
						else if (rightType == "list")
						{
							right = 1;
						}
					}

					switch (operation)
					{
						case "+":  result = left + right; break;
						case "-":  result = left - right; break;
						case "*":  result = left * right; break;
						case "/":  result = left / right; break;
						case "^":  result = Mathf.Pow(left, right); break;
						case "%":  result = left % right; break;
						case "==": result = left == right; break;
						case "!=": result = left != right; break;
						case "<":  result = left < right; break;
						case ">":  result = left > right; break;
						case "<=": result = left <= right; break;
						case ">=": result = left >= right; break;
					}
				}
				else if (leftType == "string")
				{
					if (operation != "+") return Errors.UnsupportedOperation(operation, "string", rightType, interpreter);
					right = ConvertToString(right);

					result = left + right;
				}
				else if (leftType == "list")
				{
					if (operation != "+") return Errors.UnsupportedOperation(operation, "list", rightType, interpreter);
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