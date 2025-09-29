using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static FileExplorer;
using static ScriptSaveLoad;
using System.Linq;

public class deleteaftertest : MonoBehaviour {
	private void Update() {
		if (Input.GetKeyDown("e")) {
			var tokenizer = new Tokenizer();
			string path = "C:\\Users\\gesch\\Tools\\Unity\\EngineeringOnQuack\\Assets\\Scripts\\Testing\\testing.qk";
			string contents = File.ReadAllText(path);

			(Script scriptOut, T_Data output) = tokenizer.Tokenize(contents);

			var script = scriptOut;

			string[] lines = script.OriginalText.Split('\n').Select(l => l.TrimEnd()).ToArray();
			SEProcedural.ScriptEditor.Load(lines);
		}

		if (Input.GetKeyDown("q")) {
			FileExplorer.ResetHistory();
			FileExplorer.LoadDirectory(
				"C:\\Users\\gesch\\Pictures",
				new[] {".png"},
				null,
				false
				);
		}
		/*
				if (Input.GetKeyDown("e")) {
					ProceduralScriptEditor
					WindowManager.Instance.RealiseWindows()
				}*/
	}
}