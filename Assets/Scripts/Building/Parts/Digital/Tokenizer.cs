using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;
using System.Drawing;

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
		'+', '-', '*', '/', '%', '^',			    // arithmetic
		'=', '!', '>', '<',						   // comparison
		'&', '|',								  // logic
		'(', ')', '[', ']', '{', '}',			 // region
		'.', ',', ':'							// special
	};
	/*static readonly char singlequote = @"'"[0];
	static readonly HashSet<char> regionchars = new() {
		'(', ')', '[', ']', '{', '}', '"', singlequote
	};
	static readonly Dictionary<char, char> regionpairs = new() {
		{'(' , ')'}, {'[', ']'}, {'{' , '}'}, {'"' , '"'}, {singlequote , singlequote}
	};
	string regionName(char c) => c switch {
		'(' => "parentheses",
		'[' => "brackets",
		'{' => "braces",
		'"' or '\'' => "quotes",
		_ => ""
	};*/

	public (List<Token>, Data) TokenizeLine(string line) {
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
			if (c == ' ') return chartypes.space;					     // space
			if (char.IsLetter(c) || c == '_') return chartypes.name;    // name
			if (char.IsNumber(c)) return chartypes.number;			   // number
			if (opchars.Contains(c)) return chartypes.op;			  // operator
			if (c == '"' || c == '\'') return chartypes.str;		// string
			return -1;
		}

		List<Token> tokens = new();
		StringBuilder sb = new();
		int lastType = -1;
		int i = 0;

		Data maketoken() {
			// make new token out of past built sb
			string tokenString = sb.ToString();
			sb.Clear();

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
				if (Token.Keyword.Keywords.Contains(tokenString))
					tokens.Add(new Token.Keyword(tokenString));
				else
					tokens.Add(new Token.Name(tokenString)); // otherwise add normally as name
			}
			else if (tokenString.All(c => opchars.Contains(c))) { // all operator symbols
				if (Token.Operator.AllOperators.Contains(tokenString))
					tokens.Add(new Token.Operator(tokenString));
				else
					return Errors.UnknownOperator(tokenString);
			}
			else
				return Errors.CouldntParse(line);

			return Data.Success;
		}

		while (i < line.Length) {
			char c = line[i];
			int type = chartype(c);
			if (type == chartypes.unknown) return (null, Errors.InvalidCharacter(c));

			bool lastOpThisNotFull = lastType == chartypes.op && // (sb (last op?) + this) is not valid op
				!Token.Operator.AllOperators.Contains(sb.ToString() + c); // put before the lasttype change to type

			if (i == 0 || 
				lastType == chartypes.space || 
				lastType == chartypes.str) 
				lastType = type;

			bool typechanged = lastType != type; // chartype changed, handle accordingly

			bool numtonameVV = // no switch when go from number to name or vice versa
				(lastType == chartypes.number && type == chartypes.name) || 
				(lastType == chartypes.name && type == chartypes.number);

			
			if ((typechanged && !numtonameVV) || lastOpThisNotFull) {
				Data output = maketoken();
				if (output is Error) return (null, output);
			}
			if (c != ' ') sb.Append(c); // add char to sb after and if its not space

			// old code for region symbol custom processing, no more subexpressions now
			/*// c is region symbol -> find entire region and process as own token
			if (type == 3) {
				sb.Clear(); // reset sb

				int start = i;
				char lookfor = regionpairs[c]; // determine end char
				i++;
				while (i < line.Length && line[i] != lookfor) i++; // increase i until eol or end char
				if (i == line.Length) // eol, mismatched
					return (null, Errors.MismatchedSomething(regionName(c)));
				i++;

				string region = line[start..i];
				region = region[1..^1]; // trim off ends

				switch (c) {
					case '(':
					case '[':
					case '{':
						// add subexp wih proper source
						(List<Token> subtokens, Data output) = TokenizeLine(region);
						if (output is Error) return (null, output);

						tokens.Add(new Token.SubExpression(subtokens,
							c switch {
								'(' => Token.SubExpression.Source.Parentheses,
								'[' => Token.SubExpression.Source.Brackets,
								'{' => Token.SubExpression.Source.Braces,
								_ => Token.SubExpression.Source.Parentheses // idk how to error here :(
							}));
						break;

					case '"':
					case '\'':
						// add string primitive

						tokens.Add(new Primitive.String(region));
						break;
				}
			}
*/
			
			// still need custom string processing or else string contents get tokenized too
			if (type == chartypes.str) {
				sb.Clear(); // dont include ' in sb

				int start = i;
				i++;
				while (i < line.Length && line[i] != c) i++; // increase i until eol or end char
				if (i == line.Length) // eol, mismatched
					return (null, Errors.MismatchedSomething("quotes"));
				//i++;

				string str = line[start..(i + 1)];
				str = str[1..^1]; // trim off quotes

				tokens.Add(new Primitive.String(str));
			}

			lastType = type;
			i++;

			if (i == line.Length) { // at end do one manual token generation 
				// defo better way to do this but im too lazy rn
				
				Data output = maketoken();
				if (output is Error) return (null, output);
			}
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
				(List<Token> tokens, Data output) = TokenizeLine(line);
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