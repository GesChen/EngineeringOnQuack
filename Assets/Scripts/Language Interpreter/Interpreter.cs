using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Error
{
    public string Message { get; private set; }
    public int Line { get; private set; }
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
    public bool success  { get; private set; }
    public Error error   { get; private set; }
	public dynamic value { get; private set; }


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
[System.Serializable]
public class Script
{
    public List<string> Lines { get; private set; }

    public Script(List<string> lines)
	{
		Lines = lines;
	}
}
public class Interpreter : MonoBehaviour
{
    public int currentLine;
    public Script script;
    public Dictionary<string, dynamic> variables = new()
    {
        //{ "true", 1 },
        //{ "false",0 }
    };

    public void Interpret(Script script, Evaluator evaluator)
    {
        StartCoroutine(InterpretCoroutine(script, evaluator));
    }
    public void StoreVariable(string name, dynamic value)
    {
        variables[name] = value;
    }
    public Output FetchVariable(string name)
    {
        if (!variables.ContainsKey(name))
            return Errors.UnknownVariable(name, this);
        return new Output(variables[name]);
    }
    private IEnumerator InterpretCoroutine(Script script, Evaluator evaluator)
    {
        foreach(string line in script.Lines)
        {
            yield return new();
        }
    }
}
