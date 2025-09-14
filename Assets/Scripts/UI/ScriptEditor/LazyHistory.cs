using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LazyHistory : MonoBehaviour {
	public ScriptEditor SE;

	[Serializable]
	public class Version {
		[Serializable]
		public struct Caret {
			public Vector2Int head;
			public Vector2Int tail;
		}

		public Caret[] Carets;
		public int HeadCaretI;
		public int TailCaretI;

		public string[] Contents;
	}

	// ^1 = current
	// 0 = longest ago
	public List<Version> Versions = new();
	int undos = 0; // how many is from the end of versions
	// we are at, increment/decremented by undos
	// 0 is current

	public void Initialize() { } // to match history
	
	public void RecordChange() {
		Debug.Log("try");
		// no duplicates
		if (Versions.Count > 0) {
			string[] a = SE.LinesStringArray;
			string[] b = Versions[^1].Contents;
			if (a.Length == b.Length && a.SequenceEqual(b))
				return;
		}
		Debug.Log($"recording");

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
		v.Contents = SE.LinesStringArray;

		// remember
		Versions.Add(v);
		if (Versions.Count > Config.ScriptEditor.MaxHistoryLength)
			Versions.RemoveAt(0);
	}

	void RememberCarets(Version v) {
		Debug.Log($"remember carets");

		v.Carets = SE.carets.Select(c =>
			new Version.Caret() {
				head = c.head,
				tail = c.tail
			}).ToArray();

		v.HeadCaretI = SE.headCaretI;
		v.TailCaretI = SE.tailCaretI;
	}

	public void UpdateLastCarets() {
		if (Versions.Count < 1) return;

		RememberCarets(Versions[^1]);
	}
	
	public void Undo() {
		Debug.Log($"undo {undos}");
		if (undos == Versions.Count - 1) return;

		if (undos == 0) {
			RecordChange();
		}

		undos++;
		
		SetState(Versions[Versions.Count - 1 - undos]);
	}

	public void Redo() {
		Debug.Log($"redo {undos}");
		if (undos == 0) return; // no redo at current
		undos--;

		SetState(Versions[Versions.Count - 1 - undos]);
	}

	public void SetState(Version version) {

		// set content
		SE.SetLines(version.Contents);


		// set carets
		SE.ResetCarets();
		SE.AddMultipleCarets(
			version.Carets.Select(c =>
				(c.head, c.tail)).ToList()
			);
		SE.headCaretI = version.HeadCaretI;
		SE.tailCaretI = version.TailCaretI;
		SE.UpdateCarets();

	}
}