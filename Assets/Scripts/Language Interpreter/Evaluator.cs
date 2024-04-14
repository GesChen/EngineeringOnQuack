using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;

/* operations:
 * 0  - number type, no operation
 * 1  +
 * 2  -
 * 3  *
 * 4  /
 * 5  ^ 
 * 6  %
 * 7  ==
 * 8  !=
 * 9  <
 * 10 >
 * 11 <=
 * 12 >=
 * 13 &&
 * 14 ||
 * 15 !  - this type of node will only have a left side, to be evaluated as not
 */

class ExpressionPart
{
	public bool valueType;
	public double value;
	public string operation;
	public ExpressionPart(double value)
	{
		valueType = true;
		this.value = value;
	}
	public ExpressionPart(string operation)
	{
		this.operation = operation;
	}
	public override string ToString()
	{
		return $"valueType {valueType}, value {value}, operation {operation}";
	}
}

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
		{ ">=", 4 },
		{ "&&", 4 },
		{ "||", 4 },
		{ "!" , 4 },
		{ "!&", 4 },
		{ "&!", 4 },
		{ "!|", 4 },
		{ "|!", 4 },
		{ "!!", 4 }
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
		"&!",
		"!|",
		"|!",
		"!!"
	};
	readonly string[] booleanOperators =
	{
		"==",
		"!=",
		"&&",
		"||",
		"!",
		"!&",
		"&!",
		"!|",
		"|!",
		"!!"
	};

	void Log(string s)
	{
		Debug.Log(s);
	}
	bool IsOperator(string s, int i)
	{
		if (operators.Contains(s[i].ToSafeString())) return true; // one char check - is operator
		if (i != s.Length - 1 && operators.Contains(new string(new char[] { s[i], s[i + 1] }))) return true; // two char check - is operator
		if (i != 0 && operators.Contains(new string(new char[] { s[i - 1], s[i] }))) return true; // two char check backwards - is operator

		return false;
	}
	bool ValidNumber(string s, int i)
	{
		char c = s[i];
		bool validNegative = c == '-' && (i == 0 || IsOperator(s, i - 1));
		return char.IsDigit(c) || validNegative || c == '.';
	}
	public Output Evaluate(string expression, Interpreter interpreter)
	{
		Log($"Evaluating expression: {expression}");
		// part 1: split into tokens
		// complex example: (1+2*(3-4))/5
		// output: evaluated value of 1+2*(3-4), /, 5

		List<ExpressionPart> parts = new();
		StringBuilder accumBuilder = new();
		int state = 0; // 0 - number; 1 - finding matching parentheses; 2 - operator
		int deepness = 0;
		for (int i = 0; i < expression.Length; i++)
		{
			char c = expression[i];
			Log($"Char {c} at {i}, state {state}, {parts.Count} parts, builder is {accumBuilder.ToString()}");
			if (state == 0)
			{
				if (ValidNumber(expression, i))
				{
					Log("valid");
					accumBuilder.Append(c);
				}
				else
				{
					Log("invalid, building");
					string builtString = accumBuilder.ToString();
					accumBuilder.Clear();

					if (double.TryParse(builtString, out double parsedValue))
					{
						parts.Add(new ExpressionPart(parsedValue));
						accumBuilder.Clear();
					}

					if (c == '(') // start of parentheses, switch to parentheses
					{
						state = 1;
					}
					else if (char.IsLetter(c))
					{
						state = 2;
					}
					else if (IsOperator(expression, i)) // operator, switch to operator
					{
						state = 3;
					}
					else
					{
						return new Output(new Error($"Unable to parse {builtString}", interpreter));
					}
					Log($"state: {state}");
				}
			} // else if for parentheses to skip first parentheses
			if (state == 1) // search for matching end parentheses, ignore all else 
			{
				accumBuilder.Append(c);
				if (c == '(')
					deepness++;
				else if (c == ')')
				{
					deepness--;
					if (deepness == 0) // found matching end 
					{
						string subexp = accumBuilder.ToString();
						accumBuilder.Clear();

						// strip outer parentheses and evaluate, then set that part to the evaluated value
						// deeper parenthese chains will cause more recursion obviously
						subexp = subexp[1..^1];
						Output output = Evaluate(subexp, interpreter);
						if (!output.Success) return output; // incase there was an error while parsing

						double evaluatedValue = output.Value;

						parts.Add(new ExpressionPart(evaluatedValue));

						if (i != expression.Length - 1) // don't go to operator if end of expression, 
						{
							state = 3;
							continue;
						}
					}
				}
				else if (i == expression.Length - 1) // havent found matching, this is error
				{
					return new Output(new Error("Mismatched parentheses", interpreter));
				}
			}
			else if (state == 2) // get variable 
			{
				// accumulate full string, no operators allowed inside variable names!!! 

				if (!IsOperator(expression, i))
				{
					accumBuilder.Append(c);
				}
				else
				{ // got full variable name
					string variableName = accumBuilder.ToString();
					accumBuilder.Clear();
					if (interpreter.variables.Keys.Contains(variableName))
					{
						dynamic value = interpreter.variables[variableName];
						bool isNumeric = value is int || value is float || value is double;
						if (isNumeric)
						{
							double doubleValue = (double)value;
							parts.Add(new ExpressionPart(doubleValue));
							state = 3;
						}
					}
					else
					{ // variable not found
						return new Output(new Error($"Unknown variable: \"{variableName}\"", interpreter));
					}
				}

			}
			if (state == 3) // get operator 
			{
				Log("getting operator");
				if (IsOperator(expression, i))
				{
					Log("valid operator");
					if (i == expression.Length - 1) return new Output(new Error("Operator at end of expression", interpreter));

					string doubleOperator = new(new char[] { c, expression[i + 1] });
					if (operators.Contains(doubleOperator))
					{ // double length
						parts.Add(new ExpressionPart(doubleOperator));
					}
					else if (operators.Contains(c.ToSafeString()))
					{ // single length 
						parts.Add(new ExpressionPart(c.ToSafeString()));
					}
					state = 0; // look for values
				}
				else
				{
					return new Output(new Error($"Invalid operator / Unable to find operator \"{c}\"", interpreter));
				}
			}
		}
		// if number at end, it would not be accumulated. manually add to parts
		if (state == 0)
		{
			string builtString = accumBuilder.ToString();
			accumBuilder.Clear();

			if (double.TryParse(builtString, out double parsedValue))
			{
				parts.Add(new ExpressionPart(parsedValue));
				accumBuilder.Clear();
			}
			else
			{
				return new Output(new Error($"Unable to parse {builtString}", interpreter));
			}
		}

		foreach (ExpressionPart part in parts)
		{
			Log(part.ToString());
		}

		// part 1.5: validate that operators and parts are in correct positions
		for (int p = 0; p < parts.Count; p++)
		{
			ExpressionPart part = parts[p];
			// first part cannot be operator
			if (p == 0 && !part.valueType) return new(new Error("Operator at start of expression", interpreter));

			// numbers should be on odd indexes
			if (p % 2 == 0 && !part.valueType)
				return new(new Error("Operator in bad position", interpreter));
			if (p % 2 == 1 && part.valueType)
				if (part.valueType) return new(new Error("Number in bad position", interpreter));
		}

		// part 2: actually evaluate the parts

		while (parts.Count > 1)
		{
			// step 1: find highest ranked operator
			int highestRankOpIndex = 0;
			int highestRankOpRank = -1;
			string highestRankOp = "";
			for (int p = 0; p < parts.Count; p++)
			{
				ExpressionPart part = parts[p];
				if (!part.valueType) // only look at operators
				{
					int rank = operatorRanks[part.operation];
					if (rank > highestRankOpRank)
					{
						highestRankOpRank = rank;
						highestRankOpIndex = p;
						highestRankOp = part.operation;
					}
				}
			}
			if (highestRankOp == "") return new(new Error("How the fuck did this happen", interpreter));
			if (highestRankOpIndex == 0) // not sure how this would happen but operator at star
				return new(new Error("Operator at start of expression", interpreter));
			else if (highestRankOpIndex == parts.Count - 1) // same with end, should be caught earlier
				return new(new Error("Operator at end of expression", interpreter));

			// step 2: find parts to left and right 
			double left = parts[highestRankOpIndex - 1].value;
			double right = parts[highestRankOpIndex + 1].value;

			// step 3: check if operation is boolean
			bool leftBool = false;
			bool rightBool = false;
			bool isBooleanOperation = booleanOperators.Contains(highestRankOp);
			if (isBooleanOperation)
			{
				if (left == 1)
					leftBool = true;
				else if (left == 0)
					leftBool = false;
				else
					return new(new Error($"Attempted to interpret {left} as a boolean", interpreter));

				if (right == 1)
					rightBool = true;
				else if (right == 0)
					rightBool = false;
				else
					return new(new Error($"Attempted to interpret {right} as a boolean", interpreter));
			}

			// step 4: evaluate
			double evaluatedValue;
			switch (highestRankOp)
			{
				case "+":
					evaluatedValue = left + right;
					break;

				case "-":
					evaluatedValue = left - right;
					break;

				case "*":
					evaluatedValue = left * right;
					break;

				case "/":
					evaluatedValue = left / right;
					break;

				case "^":
					evaluatedValue = Math.Pow(left, right);
					break;

				case "%":
					evaluatedValue = left % right;
					break;

				case "==":
					evaluatedValue = leftBool == rightBool ? 1 : 0;
					break;

				case "!=":
					evaluatedValue = leftBool != rightBool ? 1 : 0;
					break;

				case "<":
					evaluatedValue = left < right ? 1 : 0;
					break;

				case ">":
					evaluatedValue = left > right ? 1 : 0;
					break;

				case "<=":
					evaluatedValue = left <= right ? 1 : 0;
					break;

				case ">=":
					evaluatedValue = left >= right ? 1 : 0;
					break;

				case "&&":
					evaluatedValue = leftBool && rightBool ? 1 : 0;
					break;

				case "||":
					evaluatedValue = leftBool || rightBool ? 1 : 0;
					break;

				case "!&":
					evaluatedValue = !(leftBool && rightBool) ? 1 : 0;
					break;

				case "&!":
					evaluatedValue = !(leftBool && rightBool) ? 1 : 0;
					break;

				case "!|":
					evaluatedValue = !(leftBool || rightBool) ? 1 : 0;
					break;

				case "|!":
					evaluatedValue = !(leftBool || rightBool) ? 1 : 0;
					break;

				case "!!":
					evaluatedValue = leftBool ^ rightBool ? 1 : 0;
					break;


				default:
					return new(new Error($"Could not find operator {highestRankOp} (how the fuck did this happen?)", interpreter));
			}

			// step 5: replace original parts with output
			parts.RemoveRange(highestRankOpIndex - 1, 3);
			parts.Insert(highestRankOpIndex - 1, new(evaluatedValue));
		}
		return new Output(parts[0].value);
	}
}