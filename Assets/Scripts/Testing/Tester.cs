using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Diagnostics;

public class Tester : MonoBehaviour {

	public bool usefp1;
	public string filepath1;
	public string filepath2;
	public string logpath1;
	public string logpath2;
	public List<Color> colors;

	public int iters;
	public MemoryPart memory;
	public Interpreter interpreter;
	public Evaluator evaluator;

	public Cable IEcable;
	public Cable IMcable;

	public ScriptEditor editor;

	Script script;
	void Start() {
		BeforeTesting();
		Updatetest();
	}

	private void Update() {
		if (Input.GetKeyDown("w"))
			Updatetest();

		if (Input.GetKeyDown("r"))
			BeforeTesting();

		if (Input.GetKeyDown("e")) {
			script = null;
			BeforeTesting();
			Updatetest();

			Test();
		}

		if (Input.GetKey("q"))
			Test();

		if (Input.GetKeyDown("z")) {
			script = null;
			print("script nulled");
		}
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
		HF.WarnColor($"{ns} ns ({ns / 1e6} ms)", colors[0]);

		if (iters > 1)
			HF.WarnColor($"average {ns / iters} ns ({ns / 1e6 / iters} ms) each", colors[0]);
	}

	void BeforeTesting() {
		(CableConnection onItoMCC, CableConnection onMtoICC) = IMcable.Connect(interpreter, memory);
		interpreter.MemoryCC = onItoMCC;
		memory.InterpreterCC = onMtoICC;
		memory.Initialize(onMtoICC);
		HF.WarnColor($"memory initialized", colors[1]);
	}
	void Updatetest() {
		Tokenizer tokenizer = new();
		string path = usefp1 ? filepath1 : filepath2;
		if (File.Exists(path)) {
			string contents = File.ReadAllText(path);

			HF.WarnColor($"tokenizing {contents}", colors[1]);
			(Script scriptOut, Data output) = tokenizer.Tokenize(contents);

			if (output is Error) print(output);
			script = scriptOut;

			if (script != null) {
				using StreamWriter sw = File.CreateText(usefp1 ? logpath1 : logpath2);
				{
					//string json = ScriptSaveLoad.ConvertScriptToString(script);
					string json = ScriptSaveLoad.ConvertScriptToJson(script, true);
					sw.Write(json);

					//string reconstructed = ScriptSaveLoad.ReconstructJson(json);
					//sw.Write(reconstructed);
				}
				
				void load() => editor.Load(script);
				HF.Test(load, 1);
			}

			HF.WarnColor($"test updated to testcase file", colors[1]);
			HF.WarnColor($"updated script: \n{script}", colors[1]);
		}

	}
	void TestOnce() {
		if (iters == 1) return;

		Memory copy = memory.component.Copy();
		Data run = interpreter.Run(copy, script);
		print(run);
	}
	void ToTest() {

		Data run = interpreter.Run(memory.component, script);
		if (iters == 1)
			HF.WarnColor($"run out:" + run, colors[1]);
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