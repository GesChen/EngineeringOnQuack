using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class History : MonoBehaviour {
	public ScriptEditor SE;

/*
	#region Change Types
	
	public abstract class Change {
		public History Main;
		public Caret Caret;
		public abstract void Undo();
		public abstract void Redo();
		protected abstract void Update();
	}

	public class AddText : Change {
		public string Text;
		public Vector2Int StartPos;

		public override void Undo() {
			var line = Main.SE.lines[StartPos.y];
			line.Content = line.Content.Remove(StartPos.x, Text.Length);

			Caret.head = StartPos;

			Update();
		}

		public override void Redo() {
			var line = Main.SE.lines[StartPos.y];

			if (StartPos.x > line.Content.Length) {
				Main.SE.PadToIndex(StartPos.x, StartPos.y, line);
			}

			line.Content = line.Content.Insert(StartPos.x, Text);

			Caret.head = new(StartPos.x + Text.Length, StartPos.y);

			Update();
		}

		protected override void Update() {
			Main.SE.UpdateLine(StartPos.y);

			Caret.MatchTail();
			Caret.ResetBlink();
			Caret.Update();
		}

		public AddText(History main, Caret caret, string text, Vector2Int startPos) {
			Main = main;
			Caret = caret;
			Text = text;
			StartPos = startPos;
		}
	}

	public class RemoveText : Change {
		public string RemovedText;
		public Vector2Int StartPos;

		public override void Undo() {
			var line = Main.SE.lines[StartPos.y];

			if (StartPos.x > line.Content.Length) {
				Main.SE.PadToIndex(StartPos.x, StartPos.y, line);
			}

			line.Content = line.Content.Insert(StartPos.x, RemovedText);

			Caret.head = new(StartPos.x + RemovedText.Length, StartPos.y);

			Update();
		}

		public override void Redo() {
			var line = Main.SE.lines[StartPos.y];
			line.Content = line.Content.Remove(StartPos.x, RemovedText.Length);

			Caret.head = StartPos;

			Update();
		}

		protected override void Update() {
			Main.SE.UpdateLine(StartPos.y);

			Caret.MatchTail();
			Caret.ResetBlink();
			Caret.Update();
		}

		public RemoveText(History main, Caret caret, string removedText, Vector2Int startPos) {
			Main = main;
			Caret = caret;
			RemovedText = removedText;
			StartPos = startPos;
		}
	}

	public class NewLine : Change {
		public Vector2Int At;
		public string StartIndent;
		public bool Split, Downwards;

		public override void Undo() {

			int newLI = At.y + (Downwards ? 1 : -1);

			if (Split) {
				var newLine = Main.SE.lines[newLI];
				string newLinedContents = newLine.Content[StartIndent.Length..];

				var brokenLine = Main.SE.lines[At.y];
				brokenLine.Content += newLinedContents;
			}

			Main.SE.DeleteLine(newLI);

			Caret.head = At;

			Update();
		}

		public override void Redo() {

			Update();
		}

		protected override void Update() {
			Main.SE.UpdateLine(StartPos.y);

			Caret.MatchTail();
			Caret.ResetBlink();
			Caret.Update();
		}

		public NewLine(History main, Caret caret, Vector2Int at) {
			Main = main;
			Caret = caret;
			At = at;
		}
	}

	public class CaretMove : Change {
		public Caret Caret;
		public Vector2Int OldPosition;
		public Vector2Int NewPosition;
		public override void Undo() {
		}
		public override void Redo() {
		}

		public CaretMove(History main, Caret caret, Vector2Int oldPos, Vector2Int newPos) {
			Main = main;
			Caret = caret;
			OldPosition = oldPos;
			NewPosition = newPos;
		}
	}

	public class MultiCaretMove : Change {
		public List<CaretMove> Moves;
		public override void Undo() {
		}
		public override void Redo() {
		}

		public MultiCaretMove(History main, List<CaretMove> moves) {
			Main = main;
			Moves = moves;
		}
	}

	#endregion
*/

	public class Snapshot {
		public struct Caret {
			public Vector2Int head;
			public Vector2Int tail;
		}
		public struct CaretState {
			public Caret[] Carets;
			public int HeadCaretI;
			public int TailCaretI;
		}

		public CaretState PrevCarets;
		public CaretState CurCarets;
	
		public struct LineChange {
			public int LineNum;
			public string LineContents;
			public string PrevContents;

			public enum ChangeType {
				Default,
				Addition,
				Modification,
				Deletion
			}

			public ChangeType Type;
		}

		public LineChange[] Changes;
	}

	List<Snapshot> Changes = new();
	int undos;

	Snapshot.Caret[] lastCarets;
	int lastHCI;
	int lastTCI;

	string[] LinesBefore;
	string[] CurrentLines;

	public void Initialize() {
		LinesBefore = SE.LinesStringArray;
		lastCarets = SE.carets.Select(c => new Snapshot.Caret() { head = c.head, tail = c.tail }).ToArray();
	}

	public void RecordChange() {
		HF.LogColor("recording", MoreColors.PastelYellow);

		CurrentLines = SE.LinesStringArray;

		// find changed lines and store them in a snapshot of the past
		// like github diff

		var diffs = BadDiffs.CheckDiffs(LinesBefore, CurrentLines);

		// this will be changed later

		// merge modifies that point to each other
		List<Snapshot.LineChange> merged = new();
		for (int aI = 0; aI < diffs.AItems.Length; aI++) {
			BadDiffs.Item aChange = diffs.AItems[aI];

			if (aChange.state == BadDiffs.State.Modified) {

				// look for the matching
				int bI = diffs.BItems.ToList().FindIndex(c => 
					c.state == BadDiffs.State.Modified && 
					c.pos == aI);

				// has to be found and match the other way around
				if (bI == -1 || aChange.pos != bI)
					throw new("matching modified not found, fix your code");

				merged.Add(new() {
					LineNum = aI,
					LineContents = CurrentLines[bI],
					PrevContents = LinesBefore[aI],
					Type = Snapshot.LineChange.ChangeType.Modification
				});
			} else
			if (aChange.state != BadDiffs.State.Normal) {

				merged.Add(new() {
					LineNum = aI,
					LineContents = LinesBefore[aI],
					Type =
						aChange.state == BadDiffs.State.Addition
						? Snapshot.LineChange.ChangeType.Addition
						: Snapshot.LineChange.ChangeType.Deletion
				});
			}
		}

		for (int bI = 0; bI < diffs.BItems.Length; bI++) {
			BadDiffs.Item bChange = diffs.BItems[bI];

			if (bChange.state == BadDiffs.State.Addition ||
				bChange.state == BadDiffs.State.Removal) {

				merged.Add(new() {
					LineNum = bI,
					LineContents = CurrentLines[bI],
					Type =
						bChange.state == BadDiffs.State.Addition
						? Snapshot.LineChange.ChangeType.Addition
						: Snapshot.LineChange.ChangeType.Deletion
				});
			}
		}

		if (merged.Count == 0) {
			//print("same");
			SetLasts();
			return;
		}

		var curCarets = SE.carets.Select(c => new Snapshot.Caret() { head = c.head, tail = c.tail }).ToArray();

		Snapshot.CaretState prevState = new() {
			Carets = lastCarets,
			HeadCaretI = SE.headCaretI,
			TailCaretI = SE.tailCaretI
		};

		Snapshot.CaretState curState = new() {
			Carets = curCarets,
			HeadCaretI = lastHCI,
			TailCaretI = lastTCI
		};

		Snapshot snap = new() {
			PrevCarets = prevState,
			CurCarets = curState,
			Changes = merged.ToArray()
		};

		if (undos != 0)
			Changes.RemoveRange(Changes.Count - undos, undos);
		undos = 0;
		Changes.Add(snap);

		while (Changes.Count > Config.ScriptEditor.MaxHistoryLength) {
			Changes.RemoveAt(0);
		}

		SetLasts();
		LinesBefore = CurrentLines;
	}

	void SetLasts() {
		lastCarets = SE.carets.Select(c => new Snapshot.Caret() { head = c.head, tail = c.tail }).ToArray();
		lastHCI = SE.headCaretI;
		lastTCI = SE.tailCaretI;
	}

	void ResetCarets(Snapshot.CaretState caretState) {
		if (caretState.Carets is null) return;

		SE.ResetCarets();
		SE.AddMultipleCarets(caretState.Carets.Select(c => (c.head, c.tail)).ToList());
		SE.headCaretI = caretState.HeadCaretI;
		SE.tailCaretI = caretState.TailCaretI;
		SE.UpdateCarets();
	}

	public void Undo() {
		RecordChange();
		if (undos == Changes.Count || Changes.Count == 0) return;

		Snapshot ssToUndo = Changes[Changes.Count - 1 - undos];
		undos++;

		UndoChanges(ssToUndo.Changes);
		ResetCarets(ssToUndo.PrevCarets);

		LinesBefore = SE.LinesStringArray;
		lastCarets = ssToUndo.PrevCarets.Carets;
	}

	void UndoChanges(Snapshot.LineChange[] changes) {
		var sorted = changes.OrderByDescending(c => c.LineNum);

		var additions = sorted.Where(c => c.Type == Snapshot.LineChange.ChangeType.Addition).ToArray();
		var deletions = sorted.Where(c => c.Type == Snapshot.LineChange.ChangeType.Deletion).ToArray();
		var modifys = sorted.Where(c => c.Type == Snapshot.LineChange.ChangeType.Modification).ToArray();

		List<int> updatedIndexes = new();
		RevertAdditions(additions, ref updatedIndexes);
		RevertDeletions(deletions, ref updatedIndexes);
		UseModifications(modifys, ref updatedIndexes, true);

		// update
		List<int> gotUpdated = new();
		foreach (int toUpdate in updatedIndexes) {
			if (gotUpdated.Contains(toUpdate)) continue;

			List<int> updated = SE.UpdateLine(toUpdate);
			gotUpdated.AddRange(updated);
		}
	}

	void RevertAdditions(IEnumerable<Snapshot.LineChange> changes, ref List<int> updatedIndexes) {
		foreach (var change in changes) {
			SE.DeleteLine(change.LineNum);

			for (int i = 0; i < updatedIndexes.Count; i++)
				if (updatedIndexes[i] > change.LineNum)
					updatedIndexes[i]--;

			if (!updatedIndexes.Contains(change.LineNum))
				updatedIndexes.Add(change.LineNum);
		}
	}

	void RevertDeletions(IEnumerable<Snapshot.LineChange> changes, ref List<int> updatedIndexes) {
		foreach (var change in changes) {
			SE.InsertLine(change.LineContents, change.LineNum);

			for (int i = 0; i < updatedIndexes.Count; i++)
				if (updatedIndexes[i] >= change.LineNum)
					updatedIndexes[i]++;

			if (!updatedIndexes.Contains(change.LineNum))
				updatedIndexes.Add(change.LineNum);
		}
	}

	void UseModifications(IEnumerable<Snapshot.LineChange> changes, ref List<int> updatedIndexes, 
		bool usePrevious) {
		foreach (var change in changes) {
			SE.lines[change.LineNum].Content = usePrevious ? change.PrevContents : change.LineContents;

			if (!updatedIndexes.Contains(change.LineNum))
				updatedIndexes.Add(change.LineNum);
		}
	}

	public void Redo() {
		RecordChange();

		if (undos == 0 || Changes.Count == 0) return;

		undos--;
		Snapshot ssToUndo = Changes[Changes.Count - 1 - undos];

		RedoChanges(ssToUndo.Changes);
		ResetCarets(ssToUndo.CurCarets);

		LinesBefore = SE.LinesStringArray;
		lastCarets = ssToUndo.CurCarets.Carets;
	}

	void RedoChanges(Snapshot.LineChange[] changes) {
		var sorted = changes.OrderByDescending(c => c.LineNum);

		var additions = sorted.Where(c => c.Type == Snapshot.LineChange.ChangeType.Addition).ToArray();
		var deletions = sorted.Where(c => c.Type == Snapshot.LineChange.ChangeType.Deletion).ToArray();
		var modifys = sorted.Where(c => c.Type == Snapshot.LineChange.ChangeType.Modification).ToArray();

		List<int> updatedIndexes = new();
		UseModifications(modifys, ref updatedIndexes, false);
		RevertDeletions(additions, ref updatedIndexes);
		RevertAdditions(deletions, ref updatedIndexes);

		// update
		List<int> gotUpdated = new();
		foreach (int toUpdate in updatedIndexes) {
			if (gotUpdated.Contains(toUpdate)) continue;

			List<int> updated = SE.UpdateLine(toUpdate);
			gotUpdated.AddRange(updated);
		}
	}
}