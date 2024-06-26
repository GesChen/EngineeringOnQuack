using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Diagnostics;

public class Tester : MonoBehaviour
{
	//public string expr;
	//public List<string> testCases = new();

	public bool debug;
	public Interpreter interpreter;
	public Evaluator evaluator;
	public string scriptFilePath = "Assets\\Scripts\\Testing\\testscript.quack";

	void Test()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		
		string[] contents = File.ReadAllLines(scriptFilePath);
		
		Script script = new (contents.ToList());


		interpreter.DEBUGMODE = debug;
		Output output = interpreter.Run(script, evaluator);
		
		UnityEngine.Debug.Log(output);
		if(debug) interpreter.DumpState();

		stopwatch.Stop();
		UnityEngine.Debug.Log(stopwatch.ElapsedMilliseconds);
	}

	private void Update()
	{
		//Test();
	}

	public void TestDebug()
	{
		UnityEngine.Debug.Log("Test");
	}
}
/*
		interpreter.variables = new Dictionary<string, dynamic> { { "abc", 100 }, { "ohno", "69" } };
		interpreter.script = new(new List<string> { expr }); // "(1+2*( 3 -4) )/ 5" });
		Evaluator evaluator = GetComponent<Evaluator>();
		Output test = evaluator.EvaluateList(interpreter.script.Lines[0], interpreter);

		if (!test.success) Debug.Log(test.error);
		else
		{
			List<dynamic> outputList = test.value;
			foreach (dynamic item in outputList)
			{
				Debug.Log($"{item} type {((object)item).GetType()}");
			}
		}

		for (int t = 0; t < testCases.Count; t++)
		{
			string tc = testCases[t];
			test = evaluator.Evaluate(tc, interpreter);
			Debug.Log(test.success);
			if (!test.success) Debug.Log(test.error);
			else Debug.Log(HelperFunctions.ConvertToString(test.value));
		}
	}
}*/