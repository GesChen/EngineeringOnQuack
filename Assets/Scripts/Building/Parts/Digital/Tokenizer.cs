using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class Tokenizer {
	public string RemoveComments(string lines) {
		int opstate = 0; // 0-looking for comment, 1-in oneline comment, 2-in multiline comment
		int searchstate = 0; // 0-looking for more -s 

		int i = 0;
		StringBuilder sb = new();

		int searchAhead() { // returns how many -s are ahead
			int s = i;
			int n = 1;
			while (s++ < lines.Length && lines[s] == '-') n++; // sorry
			return n;
		}

		while (i < lines.Length) {
			bool jumped = false;
			if (lines[i] == '-') {
				jumped = true;
				int count = searchAhead();
				if (count == 2) { // -- single line start
					opstate = opstate == 1 ? 0 : 1; // toggle state 1
					i++;
				}
				else if (count == 3) { // --- multiline start
					opstate = opstate == 2 ? 0 : 2; // toggle state 2
					i += 2; // skip these
				}
				else if (count > 3) i += count; // big dash ----------- treated as one big comment
				else
					jumped = false;
			}
			else if (lines[i] == '\n' && opstate == 1) // end single line on newlines
				opstate = 0;
			
			if (i >= lines.Length) break;

			if ((opstate == 0 && !jumped)
				|| lines[i] == '\n')
				sb.Append(lines[i]);

			i++;
		}

		return sb.ToString();
	}

	public Section Tokenize(string lines) {
		lines = RemoveComments(lines);
		
		return null;
	}
}