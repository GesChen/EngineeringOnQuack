using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tester : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		Interpreter interpreter = GetComponent<Interpreter>();
		interpreter.script = new(new List<string> { "(1+2*(3-4))/5" });
		Evaluator evaluator = GetComponent<Evaluator>();
		Debug.Log(evaluator.Evaluate(interpreter.script.Lines[0], interpreter));
	}
}
