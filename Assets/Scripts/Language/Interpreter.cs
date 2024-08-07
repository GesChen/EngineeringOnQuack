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
		if (interpreter.script == null) return $"Error: {Message}";
		return $"An error has occurred on line {Line + 1} ({interpreter.script.Lines[Line].Trim()}):\n {Message}";
	}
}
public class Output
{
	public bool Success { get; }
	public Error Error { get; private set; }
	public dynamic Value { get; }

	public Output(Error error)
	{
		Error = error;
		Value = "Error";
		Success = false;
	}
	public Output(dynamic value)
	{
		this.Value = value;
		Success = true;
	}
	public override string ToString()
	{
		if (Value == null) return "Output is null";
		if (Success) return $"Output {HF.ConvertToString(Value)} (Type: {Value.GetType().FullName})";
		return Error.ToString();
	}
}

public class Script
{
	public List<string> Lines { get; }

	public Script(List<string> lines)
	{
		Lines = lines;
	}
	public override string ToString()
	{
		return HF.ConvertToString(Lines);
	}
}
public class Function
{
	public string Name { get; }
	public List<string> ArgumentNames { get; }
	public Script Script { get; }
	public Interpreter UsingInterpreter { get; set; } // TODO: idk what to do here, for constructor to reset interpreter, has to be able to be set, or else big workaround.
	public Evaluator UsingEvaluator { get; }
	public Function(
		string name,
		Script script,
		List<string> argnames,
		Interpreter interpreter,
		Evaluator evaluator)
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
	public Script Definition { get; }
	public Function Constructor { get; }
	public Function ToStringFunction { get; }

	public ClassDefinition(
		string name,
		Interpreter interpreter,
		List<dynamic> initializationScript,
		Function constructor,
		Function tostring)
	{
		Name = name;
		Definition = new Script(interpreter.ConvertToList(initializationScript, 1, 0)); // a bit backwards but it works hopefully not too inefficient
		Constructor = constructor;
		ToStringFunction = tostring;
	}

	public bool FunctionIsConstructor(Function function)
	{
		if (Constructor is null) return false;
		return function.ArgumentNames == Constructor.ArgumentNames && function.Name == Constructor.Name;
	}
	public override string ToString()
	{
		return $"Class definition object \"{Name}\"";
	}
}
public class ClassInstance
{
	//public string Name { get; }
	public ClassDefinition ClassDefinition { get; }
	public Interpreter OwnInterpreter { get; }
	public ClassInstance(ClassDefinition classDef, Interpreter ownInterpreter, Evaluator evaluator)
	{
		ClassDefinition = classDef;
		OwnInterpreter = ownInterpreter;
		ownInterpreter.evaluator = evaluator;
	}
	public Output Initialize()
	{
		return OwnInterpreter.Run(ClassDefinition.Definition);
	}
	public Output Construct(List<dynamic> args)
	{
		ClassDefinition.Constructor.UsingInterpreter = OwnInterpreter;
		return OwnInterpreter.RunFunction(ClassDefinition.Constructor, args);
	}
	public override string ToString()
	{
		if (ClassDefinition.ToStringFunction is not null)
		{
			Output eval = OwnInterpreter.Run(ClassDefinition.ToStringFunction.Script);
			if (!eval.Success) return $"Class instance of {ClassDefinition.Name}"; // return default if errored

			return HF.ConvertToString(eval.Value, false);
		}
		return $"Class instance of {ClassDefinition.Name}";
	}
}

public class Memory
{
	public List<Variable> Everything { get; private set; } // terible name
	public Interpreter Interpreter { get; private set; }

	public Memory(Interpreter interpreter)
	{
		Everything = new();
		Interpreter = interpreter;
	}

	
	public Dictionary<string, ClassDefinition> GetClasses()
	{
		return Everything
			.Where(kvp => kvp.Value is ClassDefinition)
			.ToDictionary(kvp => kvp.Name, kvp => (ClassDefinition)kvp.Value);
	}
	public Dictionary<string, Function> GetFunctions()
	{
		return Everything
			.Where(kvp => kvp.Value is Function)
			.ToDictionary(kvp => kvp.Name, kvp => (Function)kvp.Value);
	}

	public Output Store(string name, dynamic value)
	{
		if (Interpreter.keywords.Contains(name))
			return Errors.CannotSetKeyword(name, Interpreter);

		Variable variable = new(name, value);

		// overwrite old value if exists
		int oldIndex = Everything.FindIndex(item => item.Name == name);
		if (oldIndex == -1)
			Everything.Add(variable);
		else
			Everything[oldIndex] = variable; // overwrite

		return new Output(value);
	}
	public Output Fetch(string name) // retrieve a variable from memory 
	{
		if (!Everything.Any(item => item.Value == name))
			return Errors.UnknownVariable(name, Interpreter);

		return new Output(Everything.FirstOrDefault(item => item.Value == name)); // fetch
	}
	public bool VariableExists(string name)
	{
		return Everything.Any(item => item.Value == name);
	}
	public void Reset()
	{
		Everything = new();
	}
	public Output Delete(string name)
	{
		int index = Everything.FindIndex(item => item.Name == name);
		if (index == -1)
			return Errors.UnknownVariable(name, Interpreter);

		Everything.RemoveAt(index);
		return new Output(true); // returns true if successful
	}

	public List<string> GetAllNames()
	{
		return Everything.Select(item => item.Name).ToList();
	}

	public Output StoreFunction(string name, List<string> argNames, List<dynamic> script)
	{
		return StoreFunction(name, argNames, new Script(Interpreter.ConvertToList(script, 1, 0)));
	}
	public Output StoreFunction(string name, List<string> argNames, Script script)
	{
		Dictionary<string, Function> functions = GetFunctions();

		// make sure function with this name and argnames doesnt already exist 
		foreach (Function f in functions.Values)
			if (f.Name == name && f.ArgumentNames.Count == argNames.Count)
				return Errors.FunctionAlreadyExists(name, argNames.Count, Interpreter);

		/*debug*/
		if (Interpreter.DEBUGMODE) Interpreter.LogColor($"Storing new function \"{name}\" with arguments {HF.ConvertToString(argNames)}", Color.red);

		return Store(name, new Function(name, script, argNames, Interpreter, Interpreter.evaluator));
	}
}
public class Variable
{
	public string Name;
	public dynamic Value;

	public Variable(string name, dynamic value)
	{
		Name = name;
		Value = value;
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
	public string NAME;
	public bool DEBUGMODE = false;

	public bool isClass = false;

	public Evaluator evaluator;
	public int currentLine;
	public Script script;
	public Memory memory = null;
	public static readonly string[] assignmentOperators = new string[] { "=", "+=", "-=", "*=", "/=", "^=", "++", "--" };
	public static readonly string[] keywords = new string[]
	{
		"if",	// done -- !todo - remake the code, have else handling with the if, no weird logic needed
		"else",	// done
		"try",	// done
		"catch",// done
		"for",	// done
		"while",// done
		"log",	// done
		"dump", // debug purposes
		//"wait",	// done - removed, if there's a better solution in the future then might do idk
		"def",	// done
		"return",//done
		"class" // doing...
	};

	#region internal methods
	public void Initialize() // MUST be done before running anything
	{
		memory = new Memory(this);
	}

	public void LogColor(string str, Color color)
	{
		Debug.Log(string.Format("<color=#{0:X2}{1:X2}{2:X2}>{3}</color>", (byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), str));
	}
	public Output Run(Script targetScript)
	{
		if (evaluator == null) return Errors.InterpreterDoesntHaveEval(this);
		return Run(targetScript, evaluator);
	}
	public Output Run(Script targetScript, Evaluator evaluator)
	{
		// setup
		script = new(PreProcessScript(targetScript.Lines));
		List<dynamic> lines = ConvertToNested(script);
		this.evaluator = evaluator;

		Output result = Interpret(lines);

		return result;
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
		/*debug*/
		if (DEBUGMODE) LogColor($"Running function {F}", Color.red);
		if (args.Count != F.ArgumentNames.Count)
			return Errors.NoFunctionExists(F.Name, args.Count, F.UsingInterpreter);

		Script before = script;
		script = F.Script;

		// determine if this function is a constructor for a class or not, they are treated differently
		bool isConstructor = false;
		ClassDefinition constructorClass = null;
		foreach (ClassDefinition c in memory.GetClasses().Values)
		{
			if (c.FunctionIsConstructor(F))
			{
				isConstructor = true;
				constructorClass = c;
				break;
			}
		}

		if (!isConstructor) // normal case
		{
			Memory variablesStateBefore = F.UsingInterpreter.memory; // keep copy before running
			for (int i = 0; i < args.Count; i++)
			{
				Output tryStore = F.UsingInterpreter.memory.Store(F.ArgumentNames[i], args[i]);
				if (!tryStore.Success) { script = before; return tryStore; }
			}

			Output result = F.UsingInterpreter.Interpret(ConvertToNested(F.Script), depth + 1); // run it;

			// handle variables afterwards
			Memory temp = new (this);
			foreach (string varname in F.UsingInterpreter.memory.GetAllNames())
			{
				if (variablesStateBefore.VariableExists(varname)) // any new variables will not be contained within 
				{
					if (F.ArgumentNames.Contains(varname))
						temp.Store(varname, variablesStateBefore.Fetch(varname).Value); // recall starting
					else
						temp.Store(varname, F.UsingInterpreter.memory.Fetch(varname)); // or keep the changes
				}
			}
			F.UsingInterpreter.memory.Reset(); // restore 
			F.UsingInterpreter.memory = temp;

			script = before;
			return result;
		}
		else // constructor case
		{
			Interpreter newInterpreter = gameObject.AddComponent<Interpreter>();
			newInterpreter.DEBUGMODE = DEBUGMODE;
			newInterpreter.NAME = "CLASSINSTANCE";

			ClassInstance newClassInstance = new(constructorClass, newInterpreter, evaluator);


			// let the new instance run it's definition
			Output result = newClassInstance.Initialize();
			if (!result.Success) { script = before; return result; }

			// construct it with the constructor function, should be defined in the definition, if no constructor, then do nothing
			result = newClassInstance.Construct(args);
			if (!result.Success) { script = before; return result; }

			script = before;

			return new(newClassInstance);
		}
	}

	void UpdateCurrentLine(List<dynamic> workingScript, int localLine)
	{ // assumes nested form is not null
		dynamic item = workingScript[localLine];
		if (item is not ScriptLine) return;
		currentLine = item.LineNumber;
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
	public List<dynamic> ConvertToNested(Script script)
	{
		int lineRef = 0;
		return ConvertToNested(script, 0, ref lineRef);
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
	public Output ExtractArgs(string line, string keyword, int numExpected)
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
			else if (c == '(' && !inString) parenthesesDepth++;
			else if (c == ')' && !inString) parenthesesDepth--;

			if (parenthesesDepth >= 1 && !(c == '(' && parenthesesDepth == 1)) expression += c; // second check ensures starting parentheses not included

			if (c == ')' && parenthesesDepth == 0) { endPos = i; break; }
		}
		if (parenthesesDepth != 0 || endPos == -1)
		{
			return Errors.MismatchedParentheses(this);
		}

		// check if theres more stuff after the parentheses
		string rest = remaining[(endPos + 1)..].Trim();
		if (!string.IsNullOrWhiteSpace(rest) && rest.Length != 0)
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
			if (!tryEval.Success)
			{
				return tryEval;
			}
			results = tryEval.Value;
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

		List<string> commaPass = new();
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
				commaPass.Add(newLine);
		}

		List<string> blankLinePass = new();
		foreach (string line in commaPass)
		{
			if (!string.IsNullOrWhiteSpace(line))
				blankLinePass.Add(line);
		}

		return blankLinePass;
	}
	public void DumpState()
	{
		LogColor($"**STATE DUMP** [Current line: ({currentLine})] ({NAME})", Color.yellow);
		LogColor("VARIABLES: " + HF.ConvertToString(memory), Color.yellow);
		LogColor("FUNCTIONS: " + HF.ConvertToString(memory.GetFunctions()), Color.yellow);
		LogColor("CLASSES: " + HF.ConvertToString(memory.GetClasses()), Color.yellow);
		LogColor("SCRIPT LINES " + HF.ConvertToString(script.Lines), Color.yellow);
	}

	Output ProcessClass(string startLine, List<dynamic> definition)
	{
		// get class name
		int colonIndex = startLine.IndexOf(':');
		if (colonIndex == -1)
			return Errors.ExpectedColon(this);

		string name = startLine[5..colonIndex].Trim();
		/*debug*/
		if (DEBUGMODE) LogColor($"Creating new class \"{name}\"", Color.red);
		if (!HF.VariableNameIsValid(name))
			return Errors.InvalidClassName(name, this);

		if (memory.GetClasses().ContainsKey(name))
			return Errors.ClassAlreadyExists(name, this);

		// find the constructor
		List<string> constructorArgNames = new();
		List<dynamic> constructor = null;

		List<string> stringFunctionArgNames = new();
		List<dynamic> stringFunction = null;

		List<string> argNames = new();
		for (int i = 0; i < definition.Count; i++)
		{
			dynamic item = definition[i];
			if (item is ScriptLine && item.Line.StartsWith("def "))
			{
				string line = item.Line;
				currentLine = item.LineNumber;
				int startParenthesesPos = line.IndexOf('(');
				if (startParenthesesPos == -1) return Errors.ExpectedParentheses(this);

				string functionname = line[3..startParenthesesPos].Trim();
				if (functionname == name || functionname == "string") // the function name that is the same as the class name is the constructor
				{
					int endParenthesesIndex = line.IndexOf(')');
					string argsstring = line[(startParenthesesPos + 1)..endParenthesesIndex].Trim();
					if (string.IsNullOrWhiteSpace(argsstring)) // no args
					{
						argNames = new();
					}
					else
					{
						string[] args = argsstring.Split(',');
						foreach (string argName in args)
						{
							string trimmedArgName = argName.Trim();
							if (!HF.VariableNameIsValid(trimmedArgName))
								return Errors.InvalidVariableName(trimmedArgName, this);

							if (argNames.Contains(trimmedArgName))
								return Errors.DuplicateArguments(trimmedArgName, this);

							argNames.Add(trimmedArgName);
						}
					}

					if (i < definition.Count - 1 && definition[i + 1] is List<dynamic>)
					{
						if (functionname == name)
						{
							constructor = definition[i + 1];
							constructorArgNames = argNames;
						}
						else if (functionname == "string")
						{
							stringFunction = definition[i + 1];
							stringFunctionArgNames = argNames;
						}
					}
				}
			}
		}

		// convert dynamic lists into functions 
		Function constructorFunction = null;
		if (constructor is not null)
		{
			Output tryStore = memory.StoreFunction(name, constructorArgNames, constructor);
			if (!tryStore.Success) return tryStore;
			constructorFunction = tryStore.Value;
		}

		Function actualStringFunction = null;
		if (stringFunction is not null)
		{
			Output tryStore = memory.StoreFunction("string", stringFunctionArgNames, stringFunction);
			if (!tryStore.Success) return tryStore;
			actualStringFunction = tryStore.Value;
		}

		// create the new class, constructor is the new constructor function (returned from storefunction)
		ClassDefinition newClass = new(name, this, definition, constructorFunction, actualStringFunction);

		return memory.Store(name, newClass);
	}
	#endregion

	#region functions
	public void Log(dynamic str)
	{
		if (str is Output) str = str.Value; // output the value if its an output

		LogColor(HF.ConvertToString(str, false), Color.green);
	}

	#endregion

	public Output Interpret(List<dynamic> lines, int recursiondepth = 0)
	{
		if (recursiondepth > Config.MAX_RECURSION_DEPTH) return Errors.MaxRecursion(Config.MAX_RECURSION_DEPTH, this);
		//evaluator.DEBUGMODE = DEBUGMODE;

		int localLineNum = 0;
		string line = "";

		bool lastIfSucceeded = false;  // todo; refactor and dont have to use these weird things
		bool expectingElse = false;

		while (localLineNum < lines.Count)
		{
			#region preprocess
			if (lines[localLineNum] is ScriptLine)
			{
				line = lines[localLineNum].Line;
				UpdateCurrentLine(lines, localLineNum);
			}
			else // indented should have been handled by their respective functions, and skipped over
				return Errors.UnexpectedIndent(this);
			#endregion

			#region determine line type 
			string keyword = "";
			int type = 1; // -1 - expression, 0 - keyword, 1 - variable, 2 - function
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
			if (type != 0)
			{
				foreach (string varname in memory.GetAllNames())
				{
					if (line.StartsWith(varname))
					{
						type = 1;
						keyword = varname;
						position = varname.Length;
						break;
					}
				}
				if (type == 1)
				{
					int parenthesesStart = line.IndexOf('(');
					if (parenthesesStart != -1)
					{
						foreach (string fn in memory.GetFunctions().Keys)
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
			}
			#endregion

			#region debug
			if (DEBUGMODE)
			{
				if (lines[localLineNum] is ScriptLine) LogColor($"Running line {lines[localLineNum].LineNumber}: {lines[localLineNum].Line}, type: {type}", Color.cyan);
				else LogColor("Current line is indented list", Color.cyan);
			}
			#endregion

			if (type == 0) // keyword
			{
				if (keyword == "log")
				{
					Output getArgs = ExtractArgs(line, keyword, 1); // get the args
					if (!getArgs.Success) return getArgs;

					Log(getArgs.Value[0]); // perform actual function
				}
				else if (keyword == "dump")
				{
					Output getArgs = ExtractArgs(line, keyword, 0); // get the args
					if (!getArgs.Success) return getArgs;

					DumpState(); // perform actual function
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
					if (!tryEval.Success) return (tryEval);
					dynamic value = tryEval.Value;

					// attempt to force this value into a bool
					tryEval = evaluator.Evaluate($"true&&{HF.ConvertToString(value)}", this);
					if (!tryEval.Success) // couldn't be forced into a bool
						return Errors.ExpectedBoolInIf(HF.DetermineTypeFromVariable(value), this);

					bool condition = tryEval.Value; // should have outputted a bool if it didnt error
					#endregion

					if (condition)
					{ // execute proceeding code
						lastIfSucceeded = true;
						string rest = remaining[(colonIndex + 1)..];
						if (!string.IsNullOrEmpty(rest))
						{ // makes it rerun current line with new thing
							lines[localLineNum] = rest;
							localLineNum--; // bad solution but it gets ++ at end idk
							UpdateCurrentLine(lines, localLineNum);
						}
						else
						{
							localLineNum++; // step into the next part, whether be indent or normal
							UpdateCurrentLine(lines, localLineNum);
							if (localLineNum < lines.Count && lines[localLineNum] is List<dynamic>) // if next isn't indented list, dont do anything
							{
								// run everything indented
								List<dynamic> indentedLines = lines[localLineNum];
								Output result = (Interpret(indentedLines));
								if (!result.Success) return result;
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
							UpdateCurrentLine(lines, localLineNum);
						}
						else
						{ // nothing there, normal else
						  // same logic as if
							string rest = line[(colonIndex + 1)..].Trim();
							if (!string.IsNullOrEmpty(rest))
							{ // makes it rerun current line with new thing
								lines[localLineNum] = rest;
								localLineNum--; // bad solution but it gets ++ at end idk
								UpdateCurrentLine(lines, localLineNum);
							}
							else
							{ // run the rest
								localLineNum++; // step into the next part, whether be indent or normal
								UpdateCurrentLine(lines, localLineNum);
								if (localLineNum < lines.Count && lines[localLineNum] is List<dynamic>) // if next isn't indented list, dont do anything
								{
									// run everything indented
									List<dynamic> indentedLines = lines[localLineNum];
									Output result = Interpret(indentedLines);
									if (!result.Success) return result;
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
					if (!evalListString.Success) return evalListString;

					List<dynamic> list = new();
					if (evalListString.Value is not List<dynamic>)
						list = new List<dynamic> { evalListString.Value };
					else
						list = evalListString.Value;

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

						result = memory.Store(iterator, item); // store iterator
						if (!result.Success) return result;

						/*
						if (ScriptIsInstant(toRun))
							StartCoroutine(InterpretNestedForm(toRun, evaluator, return => { result = return; }));
						else
						*/
						result = Interpret(toRun);

						if (!result.Success) return result;
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
					if (!eval.Success) return eval;

					eval = evaluator.Evaluate("true&&" + HF.ConvertToString(eval.Value), this); // force it to be a bool
					if (!eval.Success || eval.Value is not bool)
						return Errors.UnableToParseAsBool(HF.ConvertToString(eval), this);
					bool result = eval.Value;
					#endregion

					while (result)
					{
						Output output = Interpret(toRun);
						if (!output.Success) return output;

						#region eval expr
						eval = evaluator.Evaluate(expr, this);
						if (!eval.Success) return eval;

						eval = evaluator.Evaluate("true&&" + HF.ConvertToString(eval.Value), this); // force it to be a bool
						if (!eval.Success || eval.Value is not bool)
							return Errors.UnableToParseAsBool(HF.ConvertToString(eval.Value), this);
						#endregion
						result = eval.Value;
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

					if (!output.Success)
					{ // find catch if fail
						if (lines[localLineNum + 1] is ScriptLine)
						{
							localLineNum++;
							UpdateCurrentLine(lines, localLineNum);
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
										return Errors.InvalidVariableName(errorVarName, this);

									Output trystore = memory.Store(errorVarName, output.Error.ToString());
									if (!trystore.Success) return trystore;
								}
								#endregion

								List<dynamic> toRun;
								if (localLineNum < lines.Count && lines[localLineNum + 1] is List<dynamic>) // if next isn't indented list, dont do anything
								{
									localLineNum++;
									UpdateCurrentLine(lines, localLineNum);
									toRun = lines[localLineNum];
								}
								else
								{
									toRun = new List<dynamic>() { line[5..].Trim() };
								}

								output = (Interpret(toRun));

								// it will not catch again, unless there is a try catch inside of torun
								if (!output.Success) return output;
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
						return Errors.InvalidFunctionName(name, this);

					List<string> argnames = new();
					int endParenthesesIndex = line.IndexOf(')');
					string argsstring = line[(startParenthesesIndex + 1)..endParenthesesIndex].Trim();
					if (string.IsNullOrWhiteSpace(argsstring)) // no args
					{
						argnames = new();
					}
					else
					{
						string[] args = argsstring.Split(',');
						foreach (string argName in args)
						{
							string trimmedArgName = argName.Trim();
							if (!HF.VariableNameIsValid(trimmedArgName))
								return Errors.InvalidVariableName(trimmedArgName, this);

							if (argnames.Contains(trimmedArgName))
								return Errors.DuplicateArguments(trimmedArgName, this);

							argnames.Add(argName.Trim());
						}
					}

					#endregion

					#region get the actual function definition
					List<dynamic> function;
					if (localLineNum < lines.Count && lines[localLineNum + 1] is List<dynamic>) // if next isn't indented list, dont do anything
					{
						localLineNum++;
						UpdateCurrentLine(lines, localLineNum);
						function = lines[localLineNum];
					}
					else
					{
						function = new List<dynamic>() { line[3..].Trim() };
					}

					if (FunctionIsEmpty(function))
						return Errors.EmptyFunction(this);
					#endregion

					Output tryStore = memory.StoreFunction(name, argnames, function);

					if (!tryStore.Success) return tryStore;
				}
				else if (keyword == "return")
				{   // it should hopefully be just as simple as this 
					string toReturn = line[6..];
					if (string.IsNullOrWhiteSpace(toReturn)) return new Output("");
					Output eval = evaluator.Evaluate(toReturn, this);

					return eval;
				}
				else if (keyword == "class")
				{ // aw hell naw
					if (localLineNum == lines.Count - 1 || lines[localLineNum + 1] is not List<dynamic>)
						return Errors.ExpectedClassDef(this);
					localLineNum++;
					UpdateCurrentLine(lines, localLineNum);

					Output result = ProcessClass(line, lines[localLineNum]);
					if (!result.Success) return result;
				}
			}
			else if (type == 1) // variables
			{
				/* two types of variable operations 
				 * 1. act on class, includes class name and . -> run the rest of the line inside that class instance's interpreter
				 * 2. variable assignment
				 */

				// any assignment operators?
				bool assignment = true;

				int periodIndex = line.IndexOf('.');
				if (periodIndex != -1)
				{
					string beforePeriod = line[..periodIndex];
					bool containsAnyAO = assignmentOperators.Any(ao => HF.ContainsSubstringOutsideQuotes(beforePeriod, ao));
					assignment = containsAnyAO;
				}
				// case 1 - contains assignment before .
				if (assignment)
				{
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
						return Errors.InvalidVariableName(varName, this);

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
						Output fetch = memory.Fetch(varName);
						if (!fetch.Success) return fetch;

						// increment/decrement can only be done to existing number variables
						if (fetch.Value is double v)
						{
							if (ao == "++")
								memory.Store(varName, v + 1);
							else
								memory.Store(varName, v - 1);
						}
						else
						{
							return Errors.UnsupportedOperation(ao[0].ToString(), HF.DetermineTypeFromVariable(fetch.Value), "number", this);
						}
					}
					else
					{
						Output tryEval = evaluator.Evaluate(remaining, this);
						if (!tryEval.Success) return tryEval;

						// all other aos modify original, variable has to already exist
						dynamic variableValue = 0;
						if (ao != "=")
						{
							Output fetch = memory.Fetch(varName);
							if (!fetch.Success) return fetch;
							variableValue = fetch.Value;
						}

						// if it was successful, store the value 
						switch (ao)
						{
							case "=":
								tryEval = memory.Store(varName, tryEval.Value);
								if (!tryEval.Success) return tryEval;
								break;
							case "+=":
								// try to add onto existing variable
								tryEval = evaluator.Evaluate(
									$"({HF.ConvertToString(variableValue)})" +
									$"+" +
									$"({HF.ConvertToString(tryEval.Value)})", this);
								if (!tryEval.Success) return tryEval;

								tryEval = memory.Store(varName, tryEval.Value);
								if (!tryEval.Success) return tryEval;
								break;
							case "-=":
								// try to add onto existing variable
								tryEval = evaluator.Evaluate(
									$"({HF.ConvertToString(variableValue)})" +
									$"-" +
									$"({HF.ConvertToString(tryEval.Value)})", this);
								if (!tryEval.Success) return tryEval;

								tryEval = memory.Store(varName, tryEval.Value);
								if (!tryEval.Success) return tryEval;
								break;
							case "*=":
								// try to add onto existing variable
								tryEval = evaluator.Evaluate(
									$"({HF.ConvertToString(variableValue)})" +
									$"*" +
									$"({HF.ConvertToString(tryEval.Value)})", this);
								if (!tryEval.Success)
									return tryEval;

								tryEval = memory.Store(varName, tryEval.Value);
								if (!tryEval.Success) return tryEval;
								break;
							case "/=":
								// try to add onto existing variable
								tryEval = evaluator.Evaluate(
									$"({HF.ConvertToString(variableValue)})" +
									$"/" +
									$"({HF.ConvertToString(tryEval.Value)})", this);
								if (!tryEval.Success) return tryEval;

								tryEval = memory.Store(varName, tryEval.Value);
								if (!tryEval.Success) return tryEval;
								break;
							case "^=":
								// try to add onto existing variable
								tryEval = evaluator.Evaluate(
									$"({HF.ConvertToString(variableValue)})" +
									$"^" +
									$"({HF.ConvertToString(tryEval.Value)})", this);
								if (!tryEval.Success) return tryEval;

								tryEval = memory.Store(varName, tryEval.Value);
								if (!tryEval.Success) return tryEval;

								break;
						}
					}
				}
				// case 2 
				else
				{
					if (periodIndex != -1) // if no . found, just ignore as it is probably just an expression and won't do anything
					{
						string name = line[..periodIndex].Trim();
						if (!memory.VariableExists(name)) return Errors.UnknownVariable(name, this);

						dynamic item = memory.Fetch(name).Value;
						if (item is ClassInstance)
						{
							ClassInstance instance = item;
							string toRun = line[(periodIndex + 1)..].Trim();
							Output result = instance.OwnInterpreter.Run(new(new() { toRun })); // run it with the class instance's interpreter
							if (!result.Success) return result;
						}
					}
				}
			}
			else if (type == 2)
			{   // function should be defined hopefully no exceptions??
				Output fetchFunc = memory.Fetch(keyword);
				if (!fetchFunc.Success) return fetchFunc;

				if (fetchFunc.Value is not Function)
					return Errors.VariableIsNotFunction(keyword, this);
				Output tryArgs = ExtractArgs(line, keyword, fetchFunc.Value.ArgumentNames.Count);
				if (!tryArgs.Success) return tryArgs;

				Output result = RunFunction(fetchFunc.Value, tryArgs.Value, recursiondepth);

				if (!result.Success) return result;
			}

			localLineNum++;
		}
		localLineNum--;

		return new("");
	}
}