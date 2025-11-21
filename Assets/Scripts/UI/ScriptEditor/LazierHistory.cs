using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LazierHistory {
	public ScriptEditorRewritten SE;

	// [Serializable]
	public class Version {
		// [Serializable]
		public struct Caret {
			public int head;
			public int tail;
		}

		public Caret[] Carets;
		public string Content;
	}

	// ^1 = current
	// 0 = longest ago
	public List<Version> Versions = new();
	int undos = 0; // how many is from the end of versions
	// we are at, increment/decremented by undos
	// 0 is current

	public void Reset() {
		undos = 0;
		Versions.Clear();
	}

	public void RecordChange() {
		// no duplicates
		if (Versions.Count > 0) {
			string a = SE.Content;
			string b = Versions[^1].Content;
			if (a == b) return;
		}
		//Debug.Log($"recording");

		// recording changes in the middle of undo will prevent redoing
		// this is the simplest approach i think
		for (int i = 0; i < undos; i++) {
			Versions.RemoveAt(Versions.Count - 1);
		}
		undos = 0;

		Version v = new();

		// remember carets
		RememberCarets(v);

		// remember contents
		v.Content = SE.Content;

		// remember
		Versions.Add(v);
		if (Versions.Count > Config.ScriptEditor.MaxHistoryLength)
			Versions.RemoveAt(0);
	}

	void RememberCarets(Version v) {
		//Debug.Log($"remember carets");

		v.Carets = SE.Carets.Carets.Select(c =>
			new Version.Caret() {
				head = c.head,
				tail = c.tail
			}).ToArray();
	}

	public void UpdateLastCarets() {
		if (Versions.Count < 1) return;

		RememberCarets(Versions[^1]);
	}
	
	public void Undo() {
		//Debug.Log($"undo {undos}");

		if (undos == 0) { // at current
			RecordChange();
		}

		undos++;

		if (undos >= Versions.Count) { // limit
			undos = Versions.Count - 1;
			return; 
		}
		
		SetState(Versions[Versions.Count - 1 - undos]);
	}

	public void Redo() {
		//Debug.Log($"redo {undos}");
		if (undos == 0) return; // no redo at current
		undos--;

		SetState(Versions[Versions.Count - 1 - undos]);
	}

	public void SetState(Version version) {
		// set content
		SE.Content = version.Content;

		// set carets
		SE.Carets.SetMultipleCarets(
			version.Carets.Select(c =>
				(c.head, c.tail)).ToArray()
			);
		SE.ForceCaretOnState_Update();

	}
}