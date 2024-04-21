using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class Error
{
	public string Message { get; }
	public int Line { get; }
	private readonly Interpreter interpreter;

	public Error(string message, Interpreter interpreter)
	{
		Message = message;
		this.interpreter = interpreter;
		Line = interpreter.currentLine;
	}

	public override string ToString()
	{
		return $"An error has occurred on line {Line} ({interpreter.script.Lines[Line]}):\n {Message}";
	}
}
public class Output
{
	public bool success  { get; }
	public Error error   { get; private set; }
	public dynamic value { get; }


	public Output Success() => new(true);
	public Output(Error error)
	{
		this.error = error;
		value = "Error";
		success = false;
	}
	public Output(dynamic value)
	{
		this.value = value;
		success = true;
	}
	public override string ToString()
	{
		if (value == null) return "Output is null";
		if (success) return $"Output {value.ToString()} (Type: {value.GetType().FullName})";
		return error.ToString();
	}
}

public class Script
{
	public List<string> Lines { get; }

	public Script(List<string> lines)
	{
		Lines = lines;
	}
}
public class Function : MonoBehaviour
{
	public string Name { get; }
	public List<string> ArgumentNames { get; }
	public List<string> Script { get; }
	public Interpreter UsingInterpreter { get; }
	public Evaluator UsingEvaluator { get; }
	public Function(string name, List<string> script, List<string> argnames, Interpreter interpreter, Evaluator evaluator)
	{
		Name = name;
		ArgumentNames = argnames;
		Script = script;
		UsingInterpreter = interpreter;
		UsingEvaluator = evaluator;
	}
	public IEnumerator Run(List<dynamic> args, Action<Output> callback)
	{
		if (args.Count != ArgumentNames.Count)
		{
			callback (Errors.NoFunctionExists(Name, args.Count, UsingInterpreter));
			yield break; // callback + yield break equiv to return, have to retrieve in weird manner outside but works
		}

		Dictionary<string, dynamic> variablesStateBefore = UsingInterpreter.variables;
		for (int i = 0; i < args.Count; i++)
		{
			Output tryStore = UsingInterpreter.StoreVariable(ArgumentNames[i], args[i]);
			if (!tryStore.success)
			{
				callback(tryStore);
				yield break;
			}
		}

		dynamic result = null;
		yield return StartCoroutine(UsingInterpreter.InterpretCoroutine(Script, UsingEvaluator, callback =>
		{
			result = callback;
		}));

		UsingInterpreter.variables = variablesStateBefore;
		callback(result);
	}
}

public class Interpreter : MonoBehaviour
{
	public int currentLine;
	public Script script;
	public Dictionary<string, dynamic> variables = new();
	enum ControlStructure
	{
		Sequential = 0,
		Assignment = 1,
		Selection = 2,
		Iteration = 3,
		Return = 4
	}
	readonly string[] assignmentOperators = new string[] { "=", "+=", "-=", "++", "--" };

	public string[] keywords = new string[]
	{
		"if",
		"else",
		"try",
		"catch",
		"for",
		"log", // print
		"wait",
		"def",
		"return"
	};
	public string[] reservedVariableNames = new string[]
	{
		"__return__"
	};
	public dynamic __return__;

	public Dictionary<string, Function> Functions = new();

	#region internal methods
	public void Interpret(Script targetScript, Evaluator evaluator)
	{
		script = targetScript;
		StartCoroutine(StartInterpreting(evaluator));
	}
	public Output StoreVariable(string name, dynamic value)
	{
		if (reservedVariableNames.Contains(name) || keywords.Contains(name)) 
			return Errors.CannotSetKeyword(name, value);
		variables[name] = value;
		return new Output(true);
	}
	public Output FetchVariable(string name)
	{
		if (!variables.ContainsKey(name))
			return Errors.UnknownVariable(name, this);
		return new Output(variables[name]);
	}
	public Output DeleteVariable(string name)
	{
		if (!variables.ContainsKey(name))
			return Errors.UnknownVariable(name, this);
		variables.Remove(name);
		return new Output(true); // returns true if successful
	}
	public Output StoreFunction(string name, List<string> argNames, List<string> script, Evaluator evaluator)
	{
		// make sure function with this name and argnames doesnt already exist 
		foreach (Function f in Functions.Values)
			if (f.Name == name && f.ArgumentNames.Count == argNames.Count)
				return Errors.FunctionAlreadyExists(name, argNames.Count, this);

		Functions[name] = new (name, script, argNames, this, evaluator);
		return new Output(true); // return true on success
	}
	int GetIndent(string s)
	{
		int curIndent = 0;
		foreach (char c in s)
		{
			if (!char.IsWhiteSpace(c)) break;
			if (c == ' ') curIndent++;
			else if (c == '\t') curIndent += 4;
		}
		return curIndent;
	}
	List<dynamic> ConvertToList(Script script, int curIndent, ref int lineNum)
	{
		List<dynamic> lines = new();
		while (lineNum < script.Lines.Count)
		{
			string line = script.Lines[lineNum];
			string trimmedLine = line.Trim();

			if (string.IsNullOrWhiteSpace(trimmedLine))
			{
				lineNum++;
				continue;
			}

			int indent = GetIndent(line);

			if (indent > curIndent)
			{
				lines.Add(ConvertToList(script, indent, ref lineNum));
				lines.Add(script.Lines[lineNum].Trim()); // add current, not added because got broken out of
			}
			else if (indent < curIndent)
			{
				return lines;
			}
			else
			{
				lines.Add(trimmedLine);
			}
			lineNum++;
		}
		return lines;
	}
	void DumpState()
	{
		Debug.Log("**STATE DUMP**");
		Debug.Log("VARIABLES: " + HelperFunctions.ConvertToString(variables));
		Debug.Log("SCRIPT " + script);
		Debug.Log("SCRIPT LINES " + HelperFunctions.ConvertToString(script.Lines));
	}
	#endregion

	#region functions
	public void Log(dynamic str)
	{
		Debug.Log(HelperFunctions.ConvertToString(str));
	}

	#endregion

	/*
	public Output InterpretLine(string line)
	{
		string keyword = "";
		bool isVariable = true;
		int position = 0;
		foreach (string kw in keywords)
		{
			if (line.StartsWith(kw))
			{
				isVariable = false;
				keyword = kw;
				position = kw.Length;
				break;
			}
		}

		if (isVariable)
		{ // set
			string assignmentOperator = "";

		}
		else
		{
			
		}

		Debug.Log($"{line} var? {isVariable} kw {keyword}");
	}
	public Output InterpretLines(List<dynamic> list)
	{
		currentLine = 0;
		string curLineContents = "";
		int listIndex = 0;
		while (currentLine < list.Count && !curLineContents.StartsWith("return"))
		{
			if (list[currentLine] is not List<dynamic>)
				curLineContents = list[currentLine];
			else
				return Errors.unexp

			string keyword = "";
			bool isVariable = true;
			int position = 0;
			foreach (string kw in keywords)
			{
				if (line.StartsWith(kw))
				{
					isVariable = false;
					keyword = kw;
					position = kw.Length;
					break;
				}
			}
		}
		return new Output(true);
	}*/

	IEnumerator StartInterpreting(Evaluator evaluator)
	{
		Output result = null;
		yield return StartCoroutine(InterpretCoroutine(script.Lines, evaluator, (callback) => { result = callback; }));

		Log(result);
		DumpState();
	}
	public IEnumerator InterpretCoroutine(List<string> lines, Evaluator evaluator, Action<Output> callback)
	{
		/*
		int lineNum = 0;
		List<dynamic> lines = ConvertToList(script, 0, ref lineNum);
		Debug.Log(HelperFunctions.ConvertToString(lines));
		InterpretLines(lines);
		*/
		currentLine = 0;
		int lastIndentation = 0;
		string line;
		while (currentLine < lines.Count)
		{
			line = lines[currentLine];
			int indentation = GetIndent(line);
			line = line.Trim();

			if (string.IsNullOrWhiteSpace(line)) { currentLine++; continue; }// skip blank lines

			string keyword = "";
			int type = 1; // 0 - keyword, 1 - variable, 2 - function
			int position = 0;
			foreach (string kw in keywords)
			{
				if (line.StartsWith(kw))
				{
					type = 0;
					keyword = kw;
					position = kw.Length;
					break;
				}
			}

			if (type == 1)
			{
				// any assignment operators?
				bool containsAnyAO = assignmentOperators.Any(ao => HelperFunctions.ContainsSubstringOutsideQuotes(line, ao));
				
				if (!containsAnyAO) // dont handle if no ao, this is some expression
				{
					Output tryEval = evaluator.Evaluate(line, this);
					if (!tryEval.success)
					{
						callback(tryEval);
						yield break;
					}
				}

				// get the variable name first 
				string varName = "";
				for(int i = 0; i < line.Length; i++)
				{
					char c = line[i];
					if (!(char.IsLetter(c) || c == '_' || (char.IsNumber(c) && i != 0))) // variable naming criteria, if stoppped following then no longer in variable
						break;
					varName += c;
				}
				varName = varName.Trim();

				// extract the assignment operator (=, +=, -=, ++, --)
				string remaining = line[varName.Length..].Trim();
				string ao = "";
				foreach (string op in assignmentOperators)
				{
					if (remaining.StartsWith(op))
					{
						ao = op;
						break;
					}
				}
				// ao shouldn't be blank now, checked if there was a valid one back there
				
				remaining = remaining[ao.Length..].Trim(); // all that remains should be the expression

				dynamic exprValue;
				if (ao == "++" || ao == "--")
				{ // all they do is + or - 1, no need to eval
					Output fetch = FetchVariable(varName);
					if (!fetch.success)
					{
						callback(fetch);
						yield break;
					}

					// increment/decrement can only be done to existing number variables
					if (fetch.value is float v)
					{
						if (ao == "++")
							StoreVariable(varName, v + 1);
						else
							StoreVariable(varName, v - 1);
					}
					else
					{
						callback(Errors.UnsupportedOperation(ao[0].ToString(), HelperFunctions.DetermineTypeFromVariable(fetch.value), "number", this));
						yield break;
					}
				}
				else
				{
					Output tryEval = evaluator.Evaluate(remaining, this);
					if (!tryEval.success)
					{
						callback(tryEval);
						yield break;
					}

					// all other aos modify original, variable has to already exist
					dynamic variableValue = 0;
					if (ao != "=")
					{
						Output fetch = FetchVariable(varName);
						if (!fetch.success)
						{
							callback(fetch);
							yield break;
						}
						variableValue = fetch.value;
					}

					// if it was successful, store the value 
					switch (ao)
					{
						case "=":
							StoreVariable(varName, tryEval.value);
							break;
						case "+=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate($"({variableValue})+({tryEval.value})", this);
							if (!tryEval.success)
							{
								callback(tryEval);
								yield break;
							}

							StoreVariable(varName, tryEval.value);
							break;
						case "-=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate($"({variableValue})-({tryEval.value})", this);
							if (!tryEval.success)
							{
								callback(tryEval);
								yield break;
							}

							StoreVariable(varName, tryEval.value);
							break;

					}
				}
			}
			else
			{

			}
			currentLine++;
		}
		callback(new Output(true));
		yield break;
	}
}
