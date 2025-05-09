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
		public Vector2Int[] Carets;
		public int HeadCaretI;
		public int TailCaretI;
	
		public struct LineChange {
			public int LineNum;
			public string LineContents;

			public enum ChangeType {
				Addition,
				Change,
				Removal
			}

			public ChangeType Type;

			/* fuckass diff algo figure out at home with blender
			 * ABCCDEF
			 * ABGGJDFH
			 */
		}

		public LineChange[] Changes;
	}

	public List<Snapshot> Changes;

	public string[] LinesBefore;
	public string[] CurrentLines;

	public void RecordChange() {

		// find changed lines and store them in a snapshot of the past
		// like github diff

		LinesBefore = CurrentLines;
	}

	

	long[] LinesToLongs(string[] lines)
		=> lines.Select(l => HF.HashString(l)).ToArray();
}
