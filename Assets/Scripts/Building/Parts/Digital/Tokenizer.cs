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

	static readonly HashSet<char> opchars = new() {
		'+', '-', '*', '/', '%', '^', // arithmetic
		'=', '!', '>', '<',          // comparison
		'&', '|',                   // logic
		'.', ','                   // special
	};
	static readonly HashSet<char> regionchars = new() {
		'(', ')', '[', ']', '{', '}', '"', '\''
	};
	static readonly Dictionary<char, char> regionpairs = new() {
		{'(' , ')'}, {'[', ']'}, {'{' , '}'}, {'"' , '"'}, {'\'' , '\''}
	};
	string regionName(char c) => c switch {
		'(' => "parentheses",
		'[' => "brackets",
		'{' => "braces",
		'"' or '\'' => "quotes",
		_ => ""
	};

	public (List<Token>, Data) TokenizeLine(string line) {
		// line has been stripped and preprocessed already

		static int chartype(char c) {
			if (char.IsLetter(c) || c == '_') return 0;	   // name
			if (char.IsNumber(c)) return 1;				  // number
			if (opchars.Contains(c)) return 2;			 // operator
			if (regionchars.Contains(c)) return 3;		// region
			return -1;
		}

		List<Token> tokens = new();
		StringBuilder sb = new();
		int lastType = -1;
		int i = 0;

		while (i < line.Length) {
			char c = line[i];
			int type = chartype(c);
			if (type == -1) return (null, Errors.InvalidCharacter(c));

			switch (type) {
				case 0:

					break;
				case 1:

					break;
				case 2:

					break;
				case 3: // region symbol, find region and process as own token
					int start = i;
					char lookfor = regionpairs[c]; // determine end char
					while (i < line.Length && line[i] != lookfor) i++; // increase i until eol or end char
					if (i == line.Length - 1) // eol, mismatched
						return (null, Errors.MismatchedSomething(regionName(c)));
					i++; // include the end char

					switch (c) {
						case '(':
							break;
						case '[':
							break;
						case '{':
							break;
						case '"':
						case '\'':

							break;
					}

					break;
			}

			i++;
		}
		return (tokens, Data.Success);
	}

	public string PreProcessLine(string line) {
		line = line.Trim();
		StringBuilder sb = new();

		int i = 0;
		bool inSpaces = false;
		while (i < line.Length) {
			char c = line[i];

			if (inSpaces) {
				if (c == ' ') { // skip if still in spaces
					i++;
					continue;
				}
				inSpaces = false;
			}

			if (!inSpaces) {
				if (c == ' ')
					inSpaces = true;

				sb.Append(c);
			}

			i++;
		}

		return sb.ToString();
	}

	public (Section, Data) SplitSection(string[] lines, int startLineNum) {
		List<Line> sectionLines = new();

		int indentation(int linenum) {
			string line = lines[linenum];
			int n = 0;
			int indentation = 0;
			while (n < line.Length && char.IsWhiteSpace(line[n])) {
				if (line[n] == ' ') indentation += 1;
				else if (line[n] == '\t') indentation += LanguageConfig.SpacesPerTab;
				n++;
			}
			return indentation;
		}

		int startIndentation = indentation(0);
		int i = 0;
		while (i < lines.Length) {
			string line = PreProcessLine(lines[i]);
			if (string.IsNullOrEmpty(line)) { i++; continue; }

			if (indentation(i) > startIndentation) {
				List<string> subSecStrings = new();
				int n = i; // get entire chunk of indented text
				while (n < lines.Length && indentation(n) > startIndentation) {
					subSecStrings.Add(lines[n]);
					n++;
				}

				// recurse deeper sections, give starting index
				(Section subsection, Data output) = SplitSection(subSecStrings.ToArray(), n);
				if (output is Error) return (null, output);

				sectionLines.Add(new(startLineNum + i, line, subsection)); // add a subsection
			}
			else {
				(List<Token> tokens, Data output) = TokenizeLine(lines[i]);
				if (output is Error) return (null, output);

				sectionLines.Add(new(startLineNum + i, line, tokens));
			}

			i++;
		}
		return (new(sectionLines), Data.Success);
	}

	public (Section, Data) Tokenize(string lines) {
		lines = RemoveComments(lines);

		(Section split, Data result) = SplitSection(lines.Split('\n'), 0);
		if (result is Error) return (null, result);

		return (split, Data.Success);
	}
}