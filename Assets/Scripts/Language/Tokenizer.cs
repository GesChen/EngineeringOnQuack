using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;
using static Token;

// might make into static class?
public class Tokenizer {
	
	// intermediary format for lines that contains their line num
	public struct iLine {
		public string content;
		public int lineNum;
	}

	public string RemoveComments(string lines) {
		int state = 0; // 0-looking for comment, 1-in oneline comment, 2-in multiline comment
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
					state = state == 1 ? 0 : // toggle state 1
						(state == 2 ? 2 : 1); // maintain state 2, then switch to single
					i++;
				}
				else if (count == 3) { // --- multiline start
					state = state == 2 ? 0 : 2; // toggle state 2
					i += 2; // skip these
				}
				else if (count > 3) i += count; // big dash ----------- treated as one big comment
				else
					jumped = false;
			}
			else if (lines[i] == '\n' && state == 1) // end single line on newlines
				state = 0;

			if (i >= lines.Length) break;

			if ((state == 0 && !jumped)
				|| lines[i] == '\n')
				sb.Append(lines[i]);

			i++;
		}

		return sb.ToString();
	}

	static readonly HashSet<char> opchars = new() {
		'+', '-', '*', '/', '%', '^',			    // arithmetic
		'=', '!', '>', '<',						   // comparison
		'&', '|',								  // logic
		'(', ')', '[', ']', '{', '}',			 // region
		'.', ',', ':'							// special
	};
	public (List<Token>, T_Data) TokenizeLine(string line) {
		// line has been stripped and preprocessed already

		var chartypes = new {
			unknown = -1,
			space = 0,
			name = 1,
			number = 2,
			op = 3,
			str = 4
		};

		int chartype(char c) {
			if (c == ' ') return chartypes.space;					    // space
			if (char.IsLetter(c) || c == '_') return chartypes.name;   // name
			if (char.IsNumber(c)) return chartypes.number;			  // number
			if (opchars.Contains(c)) return chartypes.op;			 // operator
			if (c == '"' || c == '\'') return chartypes.str;		// string
			return -1;
		}

		List<Token> tokens = new();
		StringBuilder sb = new();
		int lastType = -1;
		int i = 0;

		T_Data maketoken() {
			// make new token out of past built sb
			string tokenString = sb.ToString();
			sb.Clear();

			if (tokenString == "")
				return T_Data.Success; // can be caused by string's custom processing code

			if (tokenString.All(c => char.IsDigit(c))) { // number 
				if (int.TryParse(tokenString, out int number))
					tokens.Add(new Primitive.Number(number));
				else
					return Errors.Custom("Couldn't parse number (wtf???)");
			}
			else if (tokenString.All(c => char.IsLetterOrDigit(c) || c == '_')) { // name/kw conditions
				if (char.IsDigit(tokenString[0])) // first char is not number
					return Errors.VarNameCannotStartWithNum();

				// keyword check
				if (Token.T_Keyword.KeywordsHashSet.Contains(tokenString))
					tokens.Add(new Token.T_Keyword(tokenString));
				else
					tokens.Add(new Token.T_Name(tokenString)); // otherwise add normally as name
			}
			else if (tokenString.All(c => opchars.Contains(c))) { // all operator symbols
				if (T_Operator.AllOperatorStringsHashSet.Contains(tokenString))
					tokens.Add(new T_Operator(tokenString));
				else
					return Errors.UnknownOperator(tokenString);
			}
			else
				return Errors.CouldntParse(line);

			return T_Data.Success;
		}

		while (i < line.Length) {
			char c = line[i];
			int type = chartype(c);
			if (type == chartypes.unknown) return (null, Errors.InvalidCharacter(c));

			bool lastOpThisNotFull = lastType == chartypes.op && // (sb (last op?) + this) is not valid op
				!Token.T_Operator.AllOperatorStrings.Contains(sb.ToString() + c); // put before the lasttype change to type

			if (i == 0 || 
				lastType == chartypes.space || 
				lastType == chartypes.str) 
				lastType = type;

			bool typechanged = lastType != type; // chartype changed, handle accordingly

			bool numtonameVV = // no switch when go from number to name or vice versa
				(lastType == chartypes.number && type == chartypes.name) || 
				(lastType == chartypes.name && type == chartypes.number);

			
			if ((typechanged && !numtonameVV) || lastOpThisNotFull) {
				T_Data output = maketoken();
				if (output is Error) return (null, output);
			}
			if (c != ' ') sb.Append(c); // add char to sb after and if its not space

			// still need custom string processing or else string contents get tokenized too
			if (type == chartypes.str) {
				sb.Clear(); // dont include ' in sb

				int start = i;
				int depth = 0;
				for (i++; i < line.Length; i++) { // inc i by 1 at start to avoid original quote
					char t = line[i];

					switch (t) {
						case ')':
						case ']':
						case '}':
							depth--;
							break;
					}

					if (t == c && depth == 0) break;

					switch (t) {
						case '(':
						case '[':
						case '{':
							depth++;
							break;
					}
				}
				if (i == line.Length) // eol, mismatched
					return (null, Errors.MismatchedSomething("quotes"));
				//i++;

				string str = line[start..(i + 1)]; // get actual string chunk	
				str = str[1..^1]; // trim off quotes

				tokens.Add(new Primitive.String(str));
			}

			lastType = type;
			i++;

			if (i == line.Length) { // at end do one manual token generation 
				// defo better way to do this but im too lazy rn
				
				T_Data output = maketoken();
				if (output is Error) return (null, output);
			}
		}
		return (tokens, T_Data.Success);
	}

	public string PreProcessLine(string line) {
		// trim line
		line = line.Trim();

		// turn tabs to space
		line = line.Replace('\t', ' ');

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

	public (Section, T_Data) SplitSection(iLine[] lines) {
		List<Line> sectionLines = new();

		int indentation(int linenum) {
			string line = lines[linenum].content;
			int n = 0;
			int indentation = 0;
			while (n < line.Length && char.IsWhiteSpace(line[n])) {
				if (line[n] == ' ') indentation += 1;
				else if (line[n] == '\t') indentation += Config.Language.SpacesPerTab;
				n++;
			}
			return indentation;
		}

		int startIndentation = indentation(0);
		int i = 0;
		while (i < lines.Length) {
			iLine iLine = lines[i];
			string line = PreProcessLine(iLine.content);

			if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

			if (indentation(i) > startIndentation) { // tokenize section
				List<iLine> subSecStrings = new();
				while (i < lines.Length && 
					(indentation(i) > startIndentation || string.IsNullOrWhiteSpace(lines[i].content))) {
					subSecStrings.Add(lines[i]);
					i++;
				}
				i--; // dont go into the next token

				// recurse deeper sections, give starting index
				(Section subsection, T_Data output) = SplitSection(subSecStrings.ToArray());
				if (output is Error) return (null, output);

				sectionLines.Add(new(iLine.lineNum , line, subsection)); // add a subsection
			}
			else { // tokenize line
				(List<Token> tokens, T_Data output) = TokenizeLine(line);
				if (output is Error) return (null, output);

				sectionLines.Add(new(iLine.lineNum, line, tokens));
			}

			i++;
		}
		return (new(sectionLines.ToArray()), T_Data.Success);
	}

	public (Script, T_Data) Tokenize(string text) {
		string preprocessed = RemoveComments(text);

		string[] lineStrings = preprocessed.Split('\n');
		List<iLine> iLines = new();
		for (int i = 0; i < lineStrings.Length; i++) {
			iLines.Add(new() {
				content = lineStrings[i],
				lineNum = i + 1
			});
		}

		(Section split, T_Data result) = SplitSection(iLines.ToArray());
		if (result is Error) return (null, result);

		Script newScript = new(split, text);

		return (newScript, T_Data.Success);
	}
}