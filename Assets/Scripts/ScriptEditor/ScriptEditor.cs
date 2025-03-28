using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class ScriptEditor : MonoBehaviour
{
	public List<string> Contents;

	public float fontSize;
	public GameObject linePrefab;

	public void LoadString(string str) {
		Contents = str.Split('\n').Select(s => s.TrimEnd()).ToList();
	}
	public string ExportString() {
		return string.Join('\n', Contents);
	}
}
