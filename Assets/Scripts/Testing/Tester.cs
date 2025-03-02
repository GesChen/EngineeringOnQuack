using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class Tester : MonoBehaviour
{
	//public string expr;
	//public List<string> testCases = new();

	public bool debug;
	public bool testTime;
	public MemoryPart memory;
	public Interpreter interpreter;
	public Evaluator evaluator;

	public Cable IEcable;
	public Cable IMcable;

	void Start()
	{
		Line line = new(0, "", new List<Token>() { new Primitive.Number(5), new Token.Operator("."), new Primitive.Number(2) });
		evaluator.Evaluate(0, line);


	}
		/*
		// conect i and e
		(CableConnection onItoECC, CableConnection onEtoICC) = IEcable.Connect(interpreter, evaluator);
		interpreter.EvaluatorCC = onItoECC;
		evaluator.InterpreterCC = onEtoICC;
		print(IEcable);

		// conect i and m
		(CableConnection onItoMCC, CableConnection onMtoICC) = IMcable.Connect(interpreter, memory);
		interpreter.MemoryCC = onItoMCC;
		memory.InterpreterCC = onMtoICC;
		print(IMcable);

		memory.Initialize(onMtoICC);

		// test type for a so not setting member of primitive
		Memory testMem = new(onMtoICC);
		Type testType = new("test", testMem);
		
		// a
		Data a = new("a", testType, memory.component);
		Token.Reference ar = Token.Reference.ExistingGlobalReference("a", a);

		// a.b
		Token.Reference b = Token.Reference.NewMemberReference(ar, "b");

		Data c = new Primitive.Number(7);

		// a.b = c
		Data set = b.SetData(c);

		print(set);
	}
		*/

	/*
	public Interpreter interpreter;
	public Evaluator evaluator; 
	public string scriptFilePath = "Assets\\Scripts\\Testing\\testscript.quack";
	public TextAsset scriptAsset;
	public string testexpr;
	void Start()
	{
		Test();
	}
	void tokentest()
	{
		Output t = evaluator.Tokenize(testexpr, interpreter);

		UnityEngine.Debug.Log(string.Join('\n', t.Value));
	}
	void newevaltest()
	{
		Output t = evaluator.Evaluate(testexpr, interpreter);
		UnityEngine.Debug.Log(t);
	}
	void Test()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		scripttest();

		stopwatch.Stop();
		double time = stopwatch.Elapsed.TotalMilliseconds;
		if(testTime) UnityEngine.Debug.Log($"ms: {time} ({1 / stopwatch.Elapsed.TotalSeconds} fps)");
	}
	void scripttest()
	{
		string[] contents = File.ReadAllLines(scriptFilePath);

		Script script = new(contents.ToList());


		interpreter.DEBUGMODE = debug;
		interpreter.memory.Reset();
		Output output = interpreter.Run(script, evaluator);

		UnityEngine.Debug.Log(output);
		if (debug) interpreter.DumpState();

	}

	private void Update()
	{
		Test();
	}

	public void TestDebug()
	{
		UnityEngine.Debug.Log("Test");
	}
	*/
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