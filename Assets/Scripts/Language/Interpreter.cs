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
		return $"An error has occurred on line {Line + 1} ({interpreter.script.Lines[Line].Trim()}):\n {Message}";
	}
}
public class Output
{
	public bool success  { get; }
	public Error error   { get; private set; }
	public dynamic value { get; }


	public static Output Success() { return new(true); }
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
	public List<dynamic> Script { get; }
	public Interpreter UsingInterpreter { get; }
	public Evaluator UsingEvaluator { get; }
	public Function(string name, List<dynamic> script, List<string> argnames, Interpreter interpreter, Evaluator evaluator)
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
		yield return StartCoroutine(UsingInterpreter.InterpretNestedForm(Script, UsingEvaluator, callback => { result = callback; }));

		UsingInterpreter.variables = variablesStateBefore;
		callback(result);
	}
}

public class ScriptLine
{
	public string Line { get; }
	public int LineNumber { get; }
	public ScriptLine(string line, int linenum)
	{
		Line = line;
		LineNumber = linenum;
	}
}

public class Interpreter : MonoBehaviour
{
	public int currentLine;
	public Script script;
	public Dictionary<string, dynamic> variables = new();
	public Dictionary<string, Function> Functions = new();
	readonly string[] assignmentOperators = new string[] { "=", "+=", "-=", "*=", "/=", "^=", "++", "--" };

	public readonly string[] keywords = new string[]
	{
		"if",	// done -- !todo - remake the code, have else handling with the if, no weird logic needed
		"else",	// done
		"try",	// !todo
		"catch",// !todo
		"for",	// done
		"while",// done
		"log",	// done
		"wait",	// done
		"def",	// !todo
		"return",// !todo
		"class" // !todo...
	};

	#region internal methods
	public void LogColor(string str, Color color)
	{
		Debug.Log(string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), str));
	}
	public void Interpret(Script targetScript, Evaluator evaluator)
	{
		script = targetScript;
		StartCoroutine(StartInterpreting(evaluator));
	}

	public Output StoreVariable(string name, dynamic value)
	{
		if (keywords.Contains(name)) 
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
	public Output StoreFunction(string name, List<string> argNames, List<dynamic> script, Evaluator evaluator)
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
	public List<dynamic> ConvertToNested(Script script, int curIndent, ref int lineNum)
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
				lines.Add(ConvertToNested(script, indent, ref lineNum));
				if (lineNum < script.Lines.Count)
					lines.Add(new ScriptLine(script.Lines[lineNum].Trim(), lineNum)); // add current, not added because got broken out of
			}
			else if (indent < curIndent)
			{
				return lines;
			}
			else
			{
				lines.Add(new ScriptLine(trimmedLine, lineNum));
			}
			lineNum++;
		}
		return lines;
	}
	Output ExtractArgs(string line, string keyword, int numExpected, Evaluator evaluator)
	{
		// rest of line should be the expression
		string remaining = line[keyword.Length..];
		bool inString = false;
		int parenthesesDepth = 0;
		string expression = "";
		int endPos = -1;
		for (int i = 0; i < remaining.Length; i++)
		{
			char c = remaining[i];
			if (c == '"') inString = !inString;
			if (c == '(' && !inString) parenthesesDepth++;
			else if (c == ')' && !inString) parenthesesDepth--;
			else if (parenthesesDepth == 1) expression += c;
			if (c == ')' && parenthesesDepth == 0) { endPos = i; break; }
		}
		if (parenthesesDepth != 0 || endPos == -1)
		{
			return Errors.MismatchedParentheses(this);
		}

		string rest = remaining[(endPos + 1)..].Trim();
		if (string.IsNullOrWhiteSpace(rest) && rest.Length != 0)
		{
			return Errors.UnexpectedStatementAfterParentheses(rest, this);
		}

		// if error checks succeeded, try eval, then print result
		expression = "[" + expression + "]"; // eval it as a list
		Output tryEval = evaluator.Evaluate(expression, this);
		if (!tryEval.success)
		{
			return tryEval;
		}
		// value should be a list of the args (i swear if it somehow doesnt)

		List<dynamic> results = tryEval.value;
		if (results.Count != numExpected)
		{
			return Errors.UnexpectedNumberofArgs(keyword, numExpected, results.Count, this);
		}

		return new Output(results);
	}
	bool ScriptIsInstant(List<dynamic> script)
	{
		foreach (dynamic line in script)
		{
			if (line is ScriptLine)
			{
				if (line.Line.TrimStart().StartsWith("wait"))
					return false;
			}
			else
			{
				if (!ScriptIsInstant(line)) return false;
			}
		}
		return true;
	}

	void DumpState()
	{
		LogColor("**STATE DUMP**", Color.yellow);
		LogColor("VARIABLES: " + HelperFunctions.ConvertToString(variables), Color.yellow);
		LogColor("SCRIPT " + script, Color.yellow);
		LogColor("SCRIPT LINES " + HelperFunctions.ConvertToString(script.Lines), Color.yellow);
	}
	
	List<string> PreProcessScript(List<string> lines)
	{
		/* task: remove all comments from the code (will not be run by script)
		 * -- single line comment, can be ended with matching --
		 * --- multi line comment, can go across multiple lines
		 */

		List<string> result = new();
		bool inString;
		bool inSingleLine;
		bool inMultiLine = false;
		foreach (string line in lines) // pass 1 - remove single line commenting
		{
			// this is very bad code :(
			// but it works;;;;
			string newLine = "";
			inString = false;
			inSingleLine = false;
			int i = 0;
			while (i < line.Length)
			{
				char c = line[i];

				if (c == '"') inString = !inString;

				if (i < line.Length - 1)
				{
					if (c == '-' && line[i + 1] == '-')
					{
						if (i < line.Length - 2 && line[i + 2] == '-')
						{
							inMultiLine = !inMultiLine;
							i += 3; // avoid self + last 2 being recognized as single line, may also terminate running this line
						}
						else inSingleLine = !inSingleLine;
					}
				}

				if (i < line.Length) // i may be have been modified
					if (!(inSingleLine || inMultiLine)) newLine += line[i];
				
				i++;
			}
			result.Add(newLine);
		}

		return result;
	}
	#endregion

	#region functions
	public void Log(dynamic str)
	{
		LogColor(HelperFunctions.ConvertToString(str, false), Color.green);
	}

	#endregion

	IEnumerator StartInterpreting(Evaluator evaluator)
	{
		double start = Time.timeAsDouble;
		Output result = null;
		//yield return StartCoroutine(InterpretCoroutine(script.Lines, evaluator, callback => { result = callback; }));

		int lineNum = 0;
		
		List<dynamic> lines = ConvertToNested(script, 0, ref lineNum);
		/*if (ScriptIsInstant(lines))
			StartCoroutine(InterpretNestedForm(lines, evaluator, callback => { result = callback; }));
		else*/
		// tried to make it not yield wait if not needed, but there is still some delay, and result will be null if its too slow
		yield return StartCoroutine(InterpretNestedForm(lines, evaluator, callback => { result = callback; }));

		Debug.Log(result);
		Debug.Log($"runtime: {Time.timeAsDouble - start}s");
		DumpState();
	}

	public IEnumerator InterpretNestedForm(List<dynamic> lines, Evaluator evaluator, Action<Output> callback)
	{
		evaluator.DEBUGMODE = false;

		int localLineNum = 0;
		string line = "";

		bool lastIfSucceeded = false;
		bool expectingElse = false;

		while (localLineNum < lines.Count)
		{
			if (lines[localLineNum] is ScriptLine)
			{
				line = lines[localLineNum].Line;
				currentLine = lines[localLineNum].LineNumber;
			}
			else
			{ // indented should have been handled by their respective functions, and skipped over
				callback(Errors.UnexpectedIndent(this));
				yield break;
			}

			#region determine line type 
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
			#endregion

			if (type == 0) // keyword
			{
				if (keyword == "log")
				{
					Output getArgs = ExtractArgs(line, keyword, 1, evaluator); // get the args
					if (!getArgs.success)
					{
						callback(getArgs);
						yield break;
					}

					Log(getArgs.value[0]); // perform actual function
				}
				else if (keyword == "wait")
				{
					Output getArgs = ExtractArgs(line, keyword, 1, evaluator); // get the args
					if (!getArgs.success)
					{
						callback(getArgs);
						yield break;
					}

					yield return new WaitForSeconds(getArgs.value[0]); // perform actual function
				}
				else if (keyword == "if")
				{ // condition should be between if and 
					#region evaluate expression in if 
					string remaining = line[2..];
					bool inString = false;
					int colonIndex = -1;
					for (int i = 0; i < remaining.Length; i++)
					{
						char c = remaining[i];
						if (c == '"') inString = !inString;
						if (!inString && c == ':')
						{
							colonIndex = i;
							break;
						}
					}
					if (colonIndex == -1)
					{
						callback(Errors.ExpectedColon(this));
						yield break;
					}

					string expression = remaining[..colonIndex];
					Output tryEval = evaluator.Evaluate(expression, this);
					if (!tryEval.success)
					{
						callback(tryEval);
						yield break;
					}
					dynamic value = tryEval.value;

					// attempt to force this value into a bool
					tryEval = evaluator.Evaluate($"true&&{HelperFunctions.ConvertToString(value)}", this);
					if (!tryEval.success)
					{ // couldn't be forced into a bool
						callback(Errors.ExpectedBoolInIf(HelperFunctions.DetermineTypeFromVariable(value), this));
						yield break;
					}
					bool condition = tryEval.value; // should have outputted a bool if it didnt error
					#endregion

					if (condition)
					{ // execute proceeding code
						lastIfSucceeded = true;
						string rest = remaining[(colonIndex + 1)..];
						if (!string.IsNullOrEmpty(rest))
						{ // makes it rerun current line with new thing
							lines[localLineNum] = rest;
							localLineNum--; // bad solution but it gets ++ at end idk
						}
						else
						{
							localLineNum++; // step into the next part, whether be indent or normal
							if (localLineNum < lines.Count && lines[localLineNum] is List<dynamic>) // if next isn't indented list, dont do anything
							{
								// run everything indented
								List<dynamic> indentedLines = lines[localLineNum];
								Output result = null;
								yield return StartCoroutine(InterpretNestedForm(indentedLines, evaluator, callback => { result = callback; }));
								if (!result.success)
								{
									callback(result);
									yield break;
								}
							}
						}
					}
					else
					{ // skip past indented section
						lastIfSucceeded = false;
						if (localLineNum < lines.Count && lines[localLineNum + 1] is List<dynamic>) localLineNum++; // skip next indented part 
					}
					expectingElse = true; // start to expect else, will be reset if next line is same indent as if
				}
				else if (keyword == "else")
				{ // from end of else to : shouldnt contain anything other than if, otherwise error
				  // this has similar logic to if statements;;

					if (!expectingElse)
					{
						callback(Errors.UnexpectedElse(this));
						yield break;
					}

					#region determine if this is an else if 
					int colonIndex = line.IndexOf(':');
					if (colonIndex == -1)
					{
						callback(Errors.ExpectedColon(this));
						yield break;
					}
					string inside = line[4..colonIndex].Trim();
					bool isElseIf = false;
					if (!string.IsNullOrEmpty(inside))
					{
						if (inside[..2] == "if")
						{
							isElseIf = true;
						}
						else
						{
							callback(Errors.ExpectedColon(this));
							yield break;
						}
					}
					else
					{
						isElseIf = false;
					}
					#endregion

					if (!lastIfSucceeded)
					{
						if (isElseIf)
						{
							// this is kinda hacky, BUT it should probably work in MOST cases?
							// replace this line with the actual if statement, and rerun this line..
							// cuz that's tenically whats its doing anyway
							lines[localLineNum] = line[4..].TrimStart();
							localLineNum--;
						}
						else
						{ // nothing there, normal else
						  // same logic as if
							string rest = line[(colonIndex + 1)..].Trim();
							if (!string.IsNullOrEmpty(rest))
							{ // makes it rerun current line with new thing
								lines[localLineNum] = rest;
								localLineNum--; // bad solution but it gets ++ at end idk
							}
							else
							{ // run the rest
								localLineNum++; // step into the next part, whether be indent or normal
								if (localLineNum < lines.Count && lines[localLineNum] is List<dynamic>) // if next isn't indented list, dont do anything
								{
									// run everything indented
									List<dynamic> indentedLines = lines[localLineNum];
									Output result = null;
									yield return StartCoroutine(InterpretNestedForm(indentedLines, evaluator, callback => { result = callback; }));
									if (!result.success)
									{
										callback(result);
										yield break;
									}
								}
							}

							expectingElse = false; // shouldn't be any more after this 
						}
					}
					else
					{// skip past indent
						if (localLineNum < lines.Count && lines[localLineNum + 1] is List<dynamic>) localLineNum++; // skip next indented part 

						// else ifs can be followed by other else or else if 
						expectingElse = isElseIf;
					}
				}
				else if (keyword == "for")
				{
					#region find iterator and list
					bool inString = false;
					int inIndex = -1;
					int colonIndex = -1;
					for (int i = 0; i < line.Length; i++)
					{
						char c = line[i];
						if (c == '"') inString = !inString;

						else if (!inString && i < line.Length - 1 && c == 'i' && line[i + 1] == 'n' && inIndex == -1) // find "in
							inIndex = i;
						else if (!inString && c == ':' && colonIndex == -1)
							colonIndex = i;
					}

					if (inIndex == -1)
					{
						callback(Errors.ExpectedCustom("in", this));
						yield break;
					}
					else if (colonIndex == -1)
					{
						callback(Errors.ExpectedCustom(":", this));
						yield break;
					}

					string iterator = line[3..inIndex].Trim();
					string listString = line[(inIndex + 2)..colonIndex].Trim();

					if (string.IsNullOrEmpty(iterator))
					{
						callback(Errors.ExpectedCustom("iterator", this));
						yield break;
					}
					else if (string.IsNullOrEmpty(listString))
					{
						callback(Errors.ExpectedCustom("list", this));
						yield break;
					}

					// turn list string to actual list
					Output evalListString = evaluator.Evaluate(listString, this);
					if (!evalListString.success)
					{
						callback(evalListString);
						yield break;
					}

					List<dynamic> list = new();
					if (evalListString.value is not List<dynamic>)
						list = new List<dynamic> { evalListString.value };
					else
						list = evalListString.value;

					#endregion

					// should now successfully have an iterator variable name and a list to iterate through

					List<dynamic> toRun;
					if (localLineNum < lines.Count && lines[localLineNum + 1] is List<dynamic>) // if next isn't indented list, dont do anything
					{
						localLineNum++;
						toRun = lines[localLineNum];
					}
					else
					{
						toRun = new List<dynamic>() { line[colonIndex..] };
					}

					// run torun on each item in the list
					foreach (dynamic item in list)
					{
						StoreVariable(iterator, item); // store iterator

						Output result = null;

						/*
						if (ScriptIsInstant(toRun))
							StartCoroutine(InterpretNestedForm(toRun, evaluator, callback => { result = callback; }));
						else
						*/
						yield return StartCoroutine(InterpretNestedForm(toRun, evaluator, callback => { result = callback; }));

						if (!result.success)
						{
							callback(result);
							yield break;
						}
					}
				}
				else if (keyword == "while")
				{
					#region get the expression to check
					int colonIndex = -1;
					bool inString = false;
					for (int i = 0; i < line.Length; i++)
					{
						char c = line[i];
						if (c == '"') inString = !inString;
						else if (!inString && c == ':')
						{
							colonIndex = i; break;
						}
					}

					if (colonIndex == -1)
					{
						callback(Errors.ExpectedColon(this));
						yield break;
					}
					#endregion

					List<dynamic> toRun;
					if (localLineNum < lines.Count && lines[localLineNum + 1] is List<dynamic>) // if next isn't indented list, dont do anything
					{
						localLineNum++;
						toRun = lines[localLineNum];
					}
					else
					{
						toRun = new List<dynamic>() { line[colonIndex..] };
					}

					#region eval expr
					string expr = line[5..colonIndex].Trim();
					Output eval = evaluator.Evaluate(expr, this);
					if (!eval.success)
					{
						callback(eval);
						yield break;
					}
					eval = evaluator.Evaluate("true&&" + HelperFunctions.ConvertToString(eval.value), this); // force it to be a bool
					if (!eval.success || eval.value is not bool)
					{
						callback(Errors.UnableToParseAsBool(HelperFunctions.ConvertToString(eval), this));
						yield break;
					}
					bool result = eval.value;
					#endregion

					while (result)
					{
						Output output = null;
						yield return StartCoroutine(InterpretNestedForm(toRun, evaluator, callback => { output = callback; }));
						if (!output.success)
						{
							callback(output);
							yield break;
						}

						#region eval expr
						eval = evaluator.Evaluate(expr, this);
						if (!eval.success)
						{
							callback(eval);
							yield break;
						}
						eval = evaluator.Evaluate("true&&" + HelperFunctions.ConvertToString(eval.value), this); // force it to be a bool
						if (!eval.success || eval.value is not bool)
						{
							callback(Errors.UnableToParseAsBool(HelperFunctions.ConvertToString(eval.value), this));
							yield break;
						}
						#endregion
						result = eval.value;
					}
				}
				else if (keyword == "try")
				{
					// run the inner script
					List<dynamic> toTry;
					if (localLineNum < lines.Count && lines[localLineNum + 1] is List<dynamic>) // if next isn't indented list, dont do anything
					{
						localLineNum++;
						toTry = lines[localLineNum];
					}
					else
					{
						toTry = new List<dynamic>() { line[3..].Trim() };
					}

					Output output = null;
					yield return StartCoroutine(InterpretNestedForm(toTry, evaluator, callback => { output = callback; }));
					
					if (!output.success)
					{ // find catch if fail
						if (lines[localLineNum + 1] is ScriptLine)
						{
							localLineNum++;
							currentLine = lines[localLineNum].LineNumber;
							if (lines[localLineNum].Line.Trim().StartsWith("catch"))
							{
								#region find the variable name to store 
								line = lines[localLineNum].Line.Trim();
								int colonIndex = line.IndexOf(':');
								if (colonIndex == -1)
								{
									callback(Errors.ExpectedColon(this));
									yield break;
								}

								string errorVarName = line[5..colonIndex].Trim();
								if (!string.IsNullOrWhiteSpace(errorVarName))
								{
									if (!HelperFunctions.VariableNameIsValid(errorVarName))
									{
										callback(Errors.BadVariableName(errorVarName, this));
										yield break;
									}

									StoreVariable(errorVarName, output.error.ToString());
								}
								#endregion

								List<dynamic> toRun;
								if (localLineNum < lines.Count && lines[localLineNum + 1] is List<dynamic>) // if next isn't indented list, dont do anything
								{
									localLineNum++;
									toRun = lines[localLineNum];
								}
								else
								{
									toRun = new List<dynamic>() { line[5..].Trim() };
								}

								yield return StartCoroutine(InterpretNestedForm(toRun, evaluator, callback => { output = callback; }));

								// it will not catch again, unless there is a try catch inside of torun
								if (!output.success)
								{
									callback(output);
									yield break;
								}
							}
						}
					}
				}
				else if (keyword == "catch")
				{ // catch was already handled in the try block, shouldn't see it again
					callback(Errors.UnexpectedCatch(this));
					yield break;
				}
			}
			else if (type == 1) // set variable
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
				for (int i = 0; i < line.Length; i++)
				{
					char c = line[i];
					if (!(char.IsLetter(c) || c == '_' || (char.IsNumber(c) && i != 0))) // variable naming criteria, if stoppped following then no longer in variable
						break;
					varName += c;
				}
				varName = varName.Trim();

				if (!HelperFunctions.VariableNameIsValid(varName))
				{
					callback(Errors.BadVariableName(varName, this));
					yield break;
				}

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
							tryEval = evaluator.Evaluate(
								$"({HelperFunctions.ConvertToString(variableValue)})" +
								$"+" +
								$"({HelperFunctions.ConvertToString(tryEval.value)})", this);
							if (!tryEval.success)
							{
								callback(tryEval);
								yield break;
							}

							StoreVariable(varName, tryEval.value);
							break;
						case "-=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate(
								$"({HelperFunctions.ConvertToString(variableValue)})" +
								$"-" +
								$"({HelperFunctions.ConvertToString(tryEval.value)})", this);
							if (!tryEval.success)
							{
								callback(tryEval);
								yield break;
							}

							StoreVariable(varName, tryEval.value);
							break;
						case "*=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate(
								$"({HelperFunctions.ConvertToString(variableValue)})" +
								$"*" +
								$"({HelperFunctions.ConvertToString(tryEval.value)})", this);
							if (!tryEval.success)
							{
								callback(tryEval);
								yield break;
							}

							StoreVariable(varName, tryEval.value);
							break;
						case "/=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate(
								$"({HelperFunctions.ConvertToString(variableValue)})" +
								$"/" +
								$"({HelperFunctions.ConvertToString(tryEval.value)})", this);
							if (!tryEval.success)
							{
								callback(tryEval);
								yield break;
							}

							StoreVariable(varName, tryEval.value);
							break;
						case "^=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate(
								$"({HelperFunctions.ConvertToString(variableValue)})" +
								$"^" +
								$"({HelperFunctions.ConvertToString(tryEval.value)})", this);
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

			//if (!line.StartsWith("if")) expectingElse = false;

			localLineNum++;
		}

		callback(Output.Success());
		yield break;
	}
}