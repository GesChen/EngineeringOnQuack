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
    public bool Success  { get; private set; }
    public Error Error   { get; private set; }
	public dynamic Value { get; private set; }


	public Output(Error error)
    {
        Error = error;
        Value = "Error";
        Success = false;
    }
    public Output(dynamic value)
    {
        Value = value;
        Success = true;
    }

    public override string ToString()
    {
        if (Value == null) return "Output is null";
        if (Success) return $"Output {Value.ToString()} (Type: {Value.GetType().FullName})";
        return Error.ToString();
    }
}
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
        { "true", 1 },
        { "false",0 }
    };

    public void Interpret(Script script)
    {
        StartCoroutine(InterpretCoroutine(script));
    }
    private IEnumerator InterpretCoroutine(Script script)
    {
        foreach(string line in script.Lines)
        {
            yield return new();
        }
    }
}
