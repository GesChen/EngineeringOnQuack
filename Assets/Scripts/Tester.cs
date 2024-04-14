using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tester : MonoBehaviour
{
	public string expr;
	// Start is called before the first frame update
	void Start()
	{
		Interpreter interpreter = GetComponent<Interpreter>();
		interpreter.variables = new Dictionary<string, dynamic> { { "abc", 100 } };
		interpreter.script = new(new List<string> { expr }); // "(1+2*( 3 -4) )/ 5" });
		BetterEvaluator evaluator = GetComponent<BetterEvaluator>();
		Debug.Log(evaluator.Evaluate(interpreter.script.Lines[0], interpreter));
	}
	void Update()
	{

	}
}
