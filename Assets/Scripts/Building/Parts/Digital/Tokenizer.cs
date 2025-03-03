using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class Tokenizer {
	public string RemoveComments(string lines) {
		int opstate = 0; // 0-looking for comment, 1-in oneline comment, 2-in multiline comment
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

	public List<Token> TokenizeLine(string line)
	{
		return new();
	}

	public Section SplitSection(string[] lines, int startLineNum)
	{
		List<Line> sectionLines = new();
		
		int indentation(int linenum)
		{
			string line = lines[linenum];
			int n = 0;
			int indentation = 0;
			while (char.IsWhiteSpace(line[n]))
			{
				if (line[n] == ' ') indentation += 1;
				else if (line[n] == '\t') indentation += LanguageConfig.SpacesPerTab;
				n++;
			}
			return indentation;
		}

		int startIndentation = indentation(0);
		int i = 0;
		while (i < lines.Length)
		{
			if (indentation(i) > startIndentation)
			{
				List<string> subSecStrings = new();
				int n = i; // get entire chunk of indented text
				while (n < lines.Length && indentation(n) > startIndentation)
				{
					subSecStrings.Add(lines[n]);
					n++;
				}

				sectionLines.Add(new(startLineNum + i, lines[i], // add a subsection
                    SplitSection(subSecStrings.ToArray(), n))); // recurse deeper sections, give starting index
			}
			else
			{
				sectionLines.Add(new(startLineNum + i, lines[i], TokenizeLine(lines[i])));
			}
		}
		return new(sectionLines);
	}

	public Section Tokenize(string lines) {
		lines = RemoveComments(lines);

		return SplitSection(lines.Split('\n'), 0);
	}
	
}