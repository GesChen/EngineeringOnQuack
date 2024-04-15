using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListEvaluator : MonoBehaviour
{
	bool CheckListForm(string s)
	{
		int depth = 0;
		foreach (char c in s)
		{
			if (c == '[') depth++;
			else if (c == ']') depth--;
		}
		return depth == 0; // well formed lists should have equal [ and ], therefore closed
	}

	public Output EvaluateList(string expr, Evaluator evaluator, Interpreter interpreter)
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
				evaluate = evaluator.Evaluate(accum.Trim(), interpreter);
				if (!evaluate.success) return new Output(evaluate.error);

				items.Add(evaluate.value);
				accum = "";
			}
		}
		evaluate = evaluator.Evaluate(accum.Trim(), interpreter);
		if (!evaluate.success) return new Output(evaluate.error);

		items.Add(evaluate.value);

		return new Output(items);
	}

}
