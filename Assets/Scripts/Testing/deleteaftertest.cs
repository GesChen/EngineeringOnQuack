using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
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
			FileExplorer.CreateNewFE(
				HF.GuaranteePath(
					Config.Path.LocalPath("Scripts").ToString()
				),
				new(
					FileExplorer.Type.SaveFile,
					null,
					path => {
						FileInfo info = new FileInfo(path);
						long sizeBytes = info.Length;
						return new[] {
							($"{sizeBytes} bytes", .5f)
						};
					},
					"Save",
					null,
					5
				)
			);
		}

		
		/*
				if (Input.GetKeyDown("e")) {
					ProceduralScriptEditor
					WindowManager.Instance.RealiseWindows()
				}*/
	}
}