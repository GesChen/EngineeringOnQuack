using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class BetterEvaluator : MonoBehaviour
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
		return original.Substring(0, startIndex) + replaceWith + original.Substring(endIndex + 1);
	}

	float ParsePart(string s, Interpreter interpreter)
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

			if (interpreter.variables.ContainsKey(s))
			{
				dynamic variable = interpreter.variables[s];
				if (variable is float || variable is int)
					value = (float)variable;
				else if (variable is string)
					value = float.Parse(variable);

				if (negative)
					value *= -1;
			}
			else
			{ // not a parseable float string, or a variable
				return float.NaN;
			}
		}
		return value;
	}

	public Output Evaluate(string expr, Interpreter interpreter)
	{
		if (expr.Contains('"')) 
			return Errors.AttemptedEvalStringAsExpr(interpreter); // cant be parsing a string!
		expr = expr.Replace(" ", ""); // remove all spaces
		Log($"evaling {expr}");

		// handle parentheses
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
			string evaledvalue = Convert.ToString(Evaluate(newexpr, interpreter).Value);

			// replace the chunk of parentheses with the evalled value 
			expr = ReplaceSection(expr, parenthesesStartIndex, parenthesesEndIndex, evaledvalue);
			Log(expr);
		}

		// tokenize the expression
		List<string> tokens = new List<string>();
		string accum = "";
		for (int i = 0; i < expr.Length; i++)
		{
			char c = expr[i];

			if (char.IsDigit(c) || c == '.' || char.IsLetter(c)) // digits, . and letters are never operators
			{
				accum += c;
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
					tokens.Add(accum);
					accum = "";
					string op = c.ToString();
					// handle 2 len operators
					if (i != expr.Length - 1)
					{
						char next = expr[i + 1];
						if (!char.IsDigit(next) && next != '.' && next != '-' && char.IsSymbol(next)) // next isnt digit, . or -, and isnt letter therefore should be operator
						{
							i++; // already checked to see if last, shouldnt break hopefully
							op += expr[i]; // add next char to the operator
						}
					}
					else // additional check can be added, operators shouldnt be at end
						return Errors.OperatorInBadPosition(op, interpreter);

					if (operators.Contains(op))
					{ // valid operator
						tokens.Add(op);
					}
					else
						return Errors.OperatorDoesntExist(op, interpreter);
				}
			}
		}
		tokens.Add(accum);

		foreach (string t in tokens) Debug.Log($"token {t}");

		// check if operators are in correct positions (odd indices), not in front
		for (int t = 0; t < tokens.Count; t++)
		{
			if (t % 2 == 0)
			{
				if (operators.Contains(tokens[t])) return Errors.OperatorInBadPosition(tokens[t], interpreter);
			}
			else
			{
				if (!operators.Contains(tokens[t])) return Errors.OperatorInBadPosition(interpreter);
			}
		}

		// iteratively evaluate with pemdas
		while (tokens.Count > 1)
		{
			// find index of left most and highest ranking operator
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

			// operator definitely has items on both sides
			// attempt to turn left and right values into floats
			string leftString = tokens[lmhrIndex - 1];
			float left = ParsePart(leftString, interpreter);
			if (float.IsNaN(left))
				return Errors.UnableToParse(leftString, interpreter);

			string rightString = tokens[lmhrIndex + 1];
			float right = ParsePart(rightString, interpreter);
			if (float.IsNaN(right))
				return Errors.UnableToParse(rightString, interpreter);

			Debug.Log($"left {left} operator {tokens[lmhrIndex]} right {right}");

			string operation = tokens[lmhrIndex];
			float result = 0;

			bool leftb = left == 1;
			bool rightb = right == 1;
			try
			{
				switch (operation)
				{
					case "+":  result = left + right; break;
					case "-":  result = left - right; break;
					case "*":  result = left * right; break;
					case "/":  result = left / right; break;
					case "^":  result = Mathf.Pow(left, right); break;
					case "%":  result = left % right; break;
					case "==": result = left == right ? 1 : 0; break;
					case "!=": result = left != right ? 1 : 0; break;
					case "<":  result = left < right ? 1 : 0; break;
					case ">":  result = left > right ? 1 : 0; break;
					case "<=": result = left <= right ? 1 : 0; break;
					case ">=": result = left >= right ? 1 : 0; break;
					case "&&": result = leftb && rightb ? 1 : 0; break;
					case "||": result = leftb || rightb ? 1 : 0; break;
					case "!":  result = !leftb ? 1 : 0; break;
					case "!&": result = !(leftb && rightb) ? 1 : 0; break;
					case "!|": result = !(leftb || rightb) ? 1 : 0; break;
					case "!!": result = leftb ^ rightb ? 1 : 0; break;
				}
			}
			catch
			{
				return Errors.OperationFailed($"{left}{operation}{right}", interpreter);
			}

			tokens.RemoveAt(lmhrIndex + 1); // remove right side
			tokens[lmhrIndex] = result.ToString(); // replace operator with result
			tokens.RemoveAt(lmhrIndex - 1); // remove left

			Debug.Log($"result {result}");
		}
		return new Output(float.Parse(tokens[0]));
	}
}
