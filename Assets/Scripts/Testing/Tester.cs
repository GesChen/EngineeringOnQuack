using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System;

public class Tester : MonoBehaviour {
	[TextArea]
	public List<string> testCases = new();
	public int useTestCase;
	public List<Color> colors;

	public int iters;
	public MemoryPart memory;
	public Interpreter interpreter;
	public Evaluator evaluator;

	public Cable IEcable;
	public Cable IMcable;

	Section section;
	void Start() {
		BeforeTesting();
		Updatetest();
	}

	private void Update() {
		if (Input.GetKeyDown("w"))
			Updatetest();

		if (Input.GetKeyDown("r"))
			BeforeTesting();

		if (Input.GetKeyDown("e"))
			Test();

		if (Input.GetKey("q"))
			Test();
	}
	void Test() {
		TestOnce();

		Stopwatch sw = new();
		sw.Start();

		for (int i = 0; i < iters; i++) {
			ToTest();
		}
		sw.Stop();

		double ns = sw.ElapsedTicks * 100;
		HF.LogColor($"{ns} ns ({ns / 1e6} ms)",									colors[0]);
		HF.LogColor($"average {ns / iters} ns ({ns / 1e6 / iters} ms) each",	colors[0]);
	}

	void BeforeTesting() {
		(CableConnection onItoMCC, CableConnection onMtoICC) = IMcable.Connect(interpreter, memory);
		interpreter.MemoryCC = onItoMCC;
		memory.InterpreterCC = onMtoICC;
		memory.Initialize(onMtoICC);
		HF.LogColor($"memory initialized", colors[1]);
	}
	void Updatetest() {
		Tokenizer tokenizer = new();
		(Script scriptOut, Data output) = tokenizer.Tokenize(testCases[useTestCase]);

		if (output is Error)
			print(output);
		section = secout;

		HF.LogColor($"test updated to {testCases[useTestCase]}", colors[1]);
	}
	void TestOnce() {
		Memory before = memory.component.Copy();
		Data eval = evaluator.Evaluate(0, section.Lines[0].DeepCopy());
		memory.component = before; // make sure no changes are made to memory in the testonce
		print(eval);
	}
	void ToTest() {
		// TODO: FIX RANGE LIST! no work 
		Data eval = evaluator.Evaluate(0, section.Lines[0].DeepCopy());
		int i = 0;
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