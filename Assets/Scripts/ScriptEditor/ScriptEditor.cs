using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScriptEditor : MonoBehaviour
{
	public List<string> Contents;

	[Serializable]
	public struct Config {
		
	}

	public Config CurrentConfig;

	public void LoadString(string str) {
		Contents = str.Split('\n').Select(s => s.TrimEnd()).ToList();
	}
	public string ExportString() {
		return string.Join('\n', Contents);
	}
}
