using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SHTest : MonoBehaviour
{
	public SyntaxHighlighter sh;
	public string test;

	void Start() {
		ScriptEditor.LocalContext lc = new() {
			InternalFunctions = new(ScriptEditor.DefaultInternalFuncs()),
			Variables = new() { new() { Name = "thing", Type = 0}, new() { Name = "foo", Type = 1 } }
		};
		
		
		
		
		var a = sh.LineColorTypesArray(test, ref lc);
		print(sh.TypeArrayToString(a));
	}
}
