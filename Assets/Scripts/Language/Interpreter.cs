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
public class Function
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
	public override string ToString()
	{
		return $"{Name}: takes arguments {HF.ConvertToString(ArgumentNames)}";
	}
}
public class ClassDefinition
{
	public string Name { get; }
	public Interpreter OwnInterpreter { get; }
	public Script Definition { get; }
	public Script Constructor { get; }
	public ClassDefinition(string name, Evaluator evaluator, GameObject mainGameobject, List<dynamic> initializationScript, List<dynamic> constructor)
	{
		Name = name;
		OwnInterpreter = mainGameobject.AddComponent<Interpreter>();
		OwnInterpreter.evaluator = evaluator;
		Definition = new Script(OwnInterpreter.ConvertToList(initializationScript, 1, 0)); // a bit backwards but it works hopefully not too inefficient
		Constructor = new Script(OwnInterpreter.ConvertToList(constructor, 1, 0));	
	}
	
}
public class ClassInstance
{
	public string Name;
	public Interpreter OwnInterpreter { get; }
	public ClassInstance(ClassDefinition classDef, string name)
	{

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
	public bool DEBUGMODE = false;
	public static readonly int MAX_RECURSION_DEPTH = 20;
	
	public bool isClass = false;

	public Evaluator evaluator;
	public int currentLine;
	public Script script;
	public Dictionary<string, dynamic> variables = new();
	public Dictionary<string, Function> functions = new();
	public Dictionary<string, ClassDefinition> classes = new();
	static readonly string[] assignmentOperators = new string[] { "=", "+=", "-=", "*=", "/=", "^=", "++", "--" };
	static readonly string[] keywords = new string[]
	{
		"if",	// done -- !todo - remake the code, have else handling with the if, no weird logic needed
		"else",	// done
		"try",	// done
		"catch",// done
		"for",	// done
		"while",// done
		"log",	// done
		//"wait",	// done - removed, if there's a better solution in the future then might do idk
		"def",	// done
		"return",//done
		"class" // doing...
	};

	#region internal methods
	public void LogColor(string str, Color color)
	{
		Debug.Log(string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), str));
	}
	public void Interpret(Script targetScript)
	{
		if (evaluator == null) return;
		Interpret(targetScript, evaluator);
	}
	public void Interpret(Script targetScript, Evaluator evaluator)
	{
		this.evaluator = evaluator;

		script = targetScript;
		StartInterpreting();
	}

	public Output StoreVariable(string name, dynamic value)
	{
		if (keywords.Contains(name))
			return Errors.CannotSetKeyword(name, this);
		if (classes.ContainsKey(name))
			return Errors.AlreadyIsClass(name, this);

		variables[name] = value;
		return new Output(true);
	}
	public Output FetchVariable(string name)
	{
		if (!variables.ContainsKey(name))
			return Errors.UnknownVariable(name, this);
		return new Output(variables[name]);
	}
	public void ResetVariables()
	{
		variables = new();
	}
	public Output DeleteVariable(string name)
	{
		if (!variables.ContainsKey(name))
			return Errors.UnknownVariable(name, this);
		variables.Remove(name);
		return new Output(true); // returns true if successful
	}
	public Output StoreFunction(string name, List<string> argNames, List<dynamic> script)
	{
		// make sure function with this name and argnames doesnt already exist 
		foreach (Function f in functions.Values)
			if (f.Name == name && f.ArgumentNames.Count == argNames.Count)
				return Errors.FunctionAlreadyExists(name, argNames.Count, this);

		functions[name] = new(name, script, argNames, this, evaluator);
		return new Output(true); // return true on success
	}
	bool FunctionIsEmpty(List<dynamic> function)
	{
		// checks if the function is empty or only contains empty strings 
		if (function == null || function.Count == 0) return true;

		foreach (dynamic d in function)
		{
			if (d is ScriptLine && !string.IsNullOrWhiteSpace(d.Line)) return false;
			else if (d is List<dynamic>) if (!FunctionIsEmpty(d)) return false;
		}

		return true;
	}
	public Output RunFunction(Function F, List<dynamic> args, int depth = 0)
	{
		if (args.Count != F.ArgumentNames.Count)
			return Errors.NoFunctionExists(F.Name, args.Count, F.UsingInterpreter);

		Dictionary<string, dynamic> variablesStateBefore = F.UsingInterpreter.variables; // keep copy before running
		F.UsingInterpreter.ResetVariables();
		for (int i = 0; i < args.Count; i++)
		{
			Output tryStore = F.UsingInterpreter.StoreVariable(F.ArgumentNames[i], args[i]);
			if (!tryStore.success) return tryStore;
		}

		Output result = F.UsingInterpreter.Interpret(F.Script,  depth + 1);

		F.UsingInterpreter.variables = variablesStateBefore;
		return result;
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
				lineNum--;
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
	public List<string> ConvertToList(List<dynamic> script, int spacesPerIndent, int curIndent)
	{
		List<string> lines = new();
		foreach (dynamic i in script)
		{
			if (i is ScriptLine)
				lines.Add(new string(' ', curIndent * spacesPerIndent) + i.Line); // make new line with correct indentation and add to lines
			else if (i is List<dynamic>) // not sure how it would break??? instead of having else???
				lines.AddRange(ConvertToList(i, spacesPerIndent, curIndent + 1));
		}

		return lines;
	}
	Output ExtractArgs(string line, string keyword, int numExpected)
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
		List<dynamic> results;
		if (string.IsNullOrWhiteSpace(expression))
		{
			results = new List<dynamic>();
		}
		else
		{
			expression = "[" + expression + "]"; // eval it as a list
			Output tryEval = evaluator.Evaluate(expression, this);
			if (!tryEval.success)
			{
				return tryEval;
			}
			results = tryEval.value;
		}
		// value should be a list of the args (i swear if it somehow doesnt)

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
			if (!string.IsNullOrWhiteSpace(line))
				result.Add(newLine);
		}

		return result;
	}

	void DumpState()
	{
		LogColor("**STATE DUMP**", Color.yellow);
		LogColor("VARIABLES: " + HF.ConvertToString(variables), Color.yellow);
		LogColor("FUNCTIONS: "+HF.ConvertToString(functions), Color.yellow);
		LogColor("SCRIPT " + script, Color.yellow);
		LogColor("SCRIPT LINES " + HF.ConvertToString(script.Lines), Color.yellow);
	}
	
	Output ProcessClass(string startLine, List<dynamic> definition)
	{
		// get class name
		int colonIndex = startLine.IndexOf(':');
		if (colonIndex == -1)
			return Errors.ExpectedColon(this);

		string name = startLine[5..colonIndex].Trim();
		if (!HF.VariableNameIsValid(name))
			return Errors.BadClassName(name, this);

		if (classes.ContainsKey(name))
			return Errors.ClassAlreadyExists(name, this);

		// find the constructor
		List<dynamic> constructor = new();
		for (int i = 0; i < definition.Count; i++)
		{
			dynamic item = definition[i];
			if (item is ScriptLine && item.Line.StartsWith("def "))
			{
				string line = item.Line;
				int startParenthesesPos = line.IndexOf('(');
				currentLine = item.LineNumber;
				if (startParenthesesPos == -1) return Errors.ExpectedParentheses(this);

				string functionname = line[3..startParenthesesPos].Trim();
				if (functionname == name) // the function name that is the same as the class name is the constructor
				{
					constructor.Add(i);
					if (i < definition.Count - 1 && definition[i + 1] is List<dynamic>)
					{
						constructor.Add(definition[i + 1]);
						definition.Remove(definition[i + 1]);
					}
					definition.Remove(item);
				}
			}
		}

		// create the new class
		ClassDefinition newClass = new(name, evaluator, gameObject, definition, constructor);

		classes.Add(name, newClass);

		return new(true);
	}
	#endregion

	#region functions
	public void Log(dynamic str)
	{
		LogColor(HF.ConvertToString(str, false), Color.green);
	}

	#endregion

	void StartInterpreting()
	{
		double start = Time.timeAsDouble;

		int lineNum = 0;

		script = new(PreProcessScript(script.Lines));
		List<dynamic> lines = ConvertToNested(script, 0, ref lineNum);
		
		Output result = Interpret(lines);

		Debug.Log(result);
		Debug.Log($"runtime: {Time.timeAsDouble - start}s");
		DumpState();
	}

	public Output Interpret(List<dynamic> lines, int recursiondepth = 0)
	{
		if (recursiondepth > MAX_RECURSION_DEPTH) return Errors.MaxRecursion(MAX_RECURSION_DEPTH, this);
		evaluator.DEBUGMODE = DEBUGMODE;

		int localLineNum = 0;
		string line = "";

		bool lastIfSucceeded = false;  // todo; refactor and dont have to use these weird things
		bool expectingElse = false;

		while (localLineNum < lines.Count)
		{
			#region debug
			if (DEBUGMODE)
			{
				if (lines[localLineNum] is ScriptLine) LogColor($"Running line {lines[localLineNum].Line}", Color.cyan);
				else LogColor("Current line is indented list", Color.cyan);
			}
			#endregion

			#region preprocess
			if (lines[localLineNum] is ScriptLine)
			{
				line = lines[localLineNum].Line;
				currentLine = lines[localLineNum].LineNumber;
			}
			else // indented should have been handled by their respective functions, and skipped over
				return Errors.UnexpectedIndent(this);
			#endregion

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
			if (type == 1)
			{
				int parenthesesStart = line.IndexOf('(');
				if (parenthesesStart != -1)
				{
					foreach (string fn in functions.Keys)
					{
						if (line[..parenthesesStart] == fn)
						{
							type = 2;
							keyword = fn;
							position = fn.Length;
							break;
						}
					}
				}
			}
			#endregion

			if (type == 0) // keyword
			{
				if (keyword == "log")
				{
					Output getArgs = ExtractArgs(line, keyword, 1); // get the args
					if (!getArgs.success) return getArgs;

					Log(getArgs.value[0]); // perform actual function
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
					if (colonIndex == -1) return (Errors.ExpectedColon(this));
					string expression = remaining[..colonIndex];
					Output tryEval = evaluator.Evaluate(expression, this);
					if (!tryEval.success) return (tryEval);
					dynamic value = tryEval.value;

					// attempt to force this value into a bool
					tryEval = evaluator.Evaluate($"true&&{HF.ConvertToString(value)}", this);
					if (!tryEval.success) // couldn't be forced into a bool
						return Errors.ExpectedBoolInIf(HF.DetermineTypeFromVariable(value), this);

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
								Output result = (Interpret(indentedLines));
								if (!result.success) return result;
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

					if (!expectingElse) return Errors.UnexpectedElse(this);
					#region determine if this is an else if 
					int colonIndex = line.IndexOf(':');
					if (colonIndex == -1) return Errors.ExpectedColon(this);
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
							return Errors.ExpectedColon(this);
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
									Output result = Interpret(indentedLines);
									if (!result.success) return result;
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

					if (inIndex == -1) return Errors.ExpectedCustom("in", this);
					else if (colonIndex == -1) return Errors.ExpectedCustom(":", this);

					string iterator = line[3..inIndex].Trim();
					string listString = line[(inIndex + 2)..colonIndex].Trim();

					if (string.IsNullOrEmpty(iterator)) return Errors.ExpectedCustom("iterator", this);
					else if (string.IsNullOrEmpty(listString)) return Errors.ExpectedCustom("list", this);

					// turn list string to actual list
					Output evalListString = evaluator.Evaluate(listString, this);
					if (!evalListString.success) return evalListString;

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

						Output result = null;

						result = StoreVariable(iterator, item); // store iterator
						if (!result.success) return result;

						/*
						if (ScriptIsInstant(toRun))
							StartCoroutine(InterpretNestedForm(toRun, evaluator, return => { result = return; }));
						else
						*/
						result = Interpret(toRun);

						if (!result.success) return result;
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
						return Errors.ExpectedColon(this);
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
					if (!eval.success) return eval;

					eval = evaluator.Evaluate("true&&" + HF.ConvertToString(eval.value), this); // force it to be a bool
					if (!eval.success || eval.value is not bool)
						return Errors.UnableToParseAsBool(HF.ConvertToString(eval), this);
					bool result = eval.value;
					#endregion

					while (result)
					{
						Output output = Interpret(toRun);
						if (!output.success) return output;

						#region eval expr
						eval = evaluator.Evaluate(expr, this);
						if (!eval.success) return eval;

						eval = evaluator.Evaluate("true&&" + HF.ConvertToString(eval.value), this); // force it to be a bool
						if (!eval.success || eval.value is not bool)
							return Errors.UnableToParseAsBool(HF.ConvertToString(eval.value), this);
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

					Output output = Interpret(toTry);

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
								if (colonIndex == -1) return Errors.ExpectedColon(this);

								string errorVarName = line[5..colonIndex].Trim();
								if (!string.IsNullOrWhiteSpace(errorVarName))
								{
									if (!HF.VariableNameIsValid(errorVarName))
										return Errors.BadVariableName(errorVarName, this);

									Output trystore = StoreVariable(errorVarName, output.error.ToString());
									if (!trystore.success) return trystore;
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

								output = (Interpret(toRun));

								// it will not catch again, unless there is a try catch inside of torun
								if (!output.success) return output;
							}
						}
					}
				}
				else if (keyword == "catch")
				{
					return Errors.UnexpectedCatch(this);
				}
				else if (keyword == "def")
				{
					#region get the name and args
					int startParenthesesIndex = line.IndexOf('(');
					if (startParenthesesIndex == -1)
						return Errors.ExpectedParentheses(this);

					string name = line[3..startParenthesesIndex].Trim();
					if (!HF.VariableNameIsValid(name))
						return Errors.BadFunctionName(name, this);

					List<string> argnames = new();
					int endParenthesesIndex = line.IndexOf(')');
					string argsstring = line[(startParenthesesIndex + 1)..endParenthesesIndex].Trim();
					string[] args = argsstring.Split(',');
					foreach (string argName in args)
					{
						if (!HF.VariableNameIsValid(argName))
							return Errors.BadVariableName(argName, this);

						if (argnames.Contains(argName))
							return Errors.DuplicateArguments(argName, this);

						argnames.Add(argName);
					}

					#endregion

					#region get the actual function definition
					List<dynamic> function;
					if (localLineNum < lines.Count && lines[localLineNum + 1] is List<dynamic>) // if next isn't indented list, dont do anything
					{
						localLineNum++;
						function = lines[localLineNum];
					}
					else
					{
						function = new List<dynamic>() { line[3..].Trim() };
					}

					if (FunctionIsEmpty(function))
						return Errors.EmptyFunction(this);
					#endregion

					Output tryStore = StoreFunction(name, argnames, function);

					if (!tryStore.success) return tryStore;
				}
				else if (keyword == "return")
				{   // it should hopefully be just as simple as this 
					string toReturn = line[6..];
					Output eval = evaluator.Evaluate(toReturn, this);

					return eval;
				}
				else if (keyword == "class")
				{ // aw hell naw
					if (localLineNum == lines.Count - 1 || lines[localLineNum + 1] is not List<dynamic>)
						return Errors.ExpectedClassDef(this);
					localLineNum++;

					Output result = ProcessClass(line, lines[localLineNum]);
					if (!result.success) return result;
				}
			}
			else if (type == 1) // set variable
			{
				// any assignment operators?
				bool containsAnyAO = assignmentOperators.Any(ao => HF.ContainsSubstringOutsideQuotes(line, ao));

				if (!containsAnyAO) // dont handle if no ao, this is some expression
				{
					Output tryEval = evaluator.Evaluate(line, this);
					if (!tryEval.success) return tryEval;
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

				if (!HF.VariableNameIsValid(varName))
					return Errors.BadVariableName(varName, this);

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
					if (!fetch.success) return fetch;

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
						return Errors.UnsupportedOperation(ao[0].ToString(), HF.DetermineTypeFromVariable(fetch.value), "number", this);
					}
				}
				else
				{
					Output tryEval = evaluator.Evaluate(remaining, this);
					if (!tryEval.success) return tryEval;

					// all other aos modify original, variable has to already exist
					dynamic variableValue = 0;
					if (ao != "=")
					{
						Output fetch = FetchVariable(varName);
						if (!fetch.success) return fetch;
						variableValue = fetch.value;
					}

					// if it was successful, store the value 
					switch (ao)
					{
						case "=":
							tryEval = StoreVariable(varName, tryEval.value);
							if (!tryEval.success) return tryEval;
							break;
						case "+=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate(
								$"({HF.ConvertToString(variableValue)})" +
								$"+" +
								$"({HF.ConvertToString(tryEval.value)})", this);
							if (!tryEval.success) return tryEval;

							tryEval = StoreVariable(varName, tryEval.value);
							if (!tryEval.success) return tryEval;
							break;
						case "-=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate(
								$"({HF.ConvertToString(variableValue)})" +
								$"-" +
								$"({HF.ConvertToString(tryEval.value)})", this);
							if (!tryEval.success) return tryEval;

							tryEval = StoreVariable(varName, tryEval.value);
							if (!tryEval.success) return tryEval;
							break;
						case "*=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate(
								$"({HF.ConvertToString(variableValue)})" +
								$"*" +
								$"({HF.ConvertToString(tryEval.value)})", this);
							if (!tryEval.success)
							return tryEval;

							tryEval = StoreVariable(varName, tryEval.value);
							if (!tryEval.success) return tryEval;
							break;
						case "/=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate(
								$"({HF.ConvertToString(variableValue)})" +
								$"/" +
								$"({HF.ConvertToString(tryEval.value)})", this);
							if (!tryEval.success) return tryEval;

							tryEval = StoreVariable(varName, tryEval.value);
							if (!tryEval.success) return tryEval;
							break;
						case "^=":
							// try to add onto existing variable
							tryEval = evaluator.Evaluate(
								$"({HF.ConvertToString(variableValue)})" +
								$"^" +
								$"({HF.ConvertToString(tryEval.value)})", this);
							if (!tryEval.success) return tryEval;

							tryEval = StoreVariable(varName, tryEval.value);
							if (!tryEval.success) return tryEval;

							break;
					}
				}
			}
			else if (type == 2)
			{   // function should be defined hopefully no exceptions??
				Output tryArgs = ExtractArgs(line, keyword, functions[keyword].ArgumentNames.Count);
				if (!tryArgs.success) return tryArgs;

				Output result = RunFunction(functions[keyword], tryArgs.value, recursiondepth);

				if (!result.success) return result;
			}

			localLineNum++;
		}

		return Output.Success();
		
	}
}