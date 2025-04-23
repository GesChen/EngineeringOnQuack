using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;
using System;

// probably one of the most unmaintainable files ever, good luck and im sorry lol

public class SyntaxHighlighter : MonoBehaviour {

	//[Header("temporary colors")]
	//[ColorUsage(false)] public Color keywordColor;
	//[ColorUsage(false)] public Color functionColor;
	//[ColorUsage(false)] public Color variableColor;
	//[ColorUsage(false)] public Color unknownColor;
	//[ColorUsage(false)] public Color symbolColor;
	//[ColorUsage(false)] public Color literalColor;
	//[ColorUsage(false)] public Color typeColor;
	//[ColorUsage(false)] public Color commentColor;

	public enum Types {
		unassigned,
		keyword,
		func,
		variable,
		unknown,
		symbol,
		literal,
		type,
		comment
	}

	Dictionary<Types, string> TypeToHex;
	void Awake() {
		TypeToHex = new() {
			{ Types.unassigned , "#000" },
			{ Types.keyword		, ColorUtility.ToHtmlStringRGB(Config.Language.Colors.Keyword)	},
			{ Types.func		, ColorUtility.ToHtmlStringRGB(Config.Language.Colors.Function)	},
			{ Types.variable	, ColorUtility.ToHtmlStringRGB(Config.Language.Colors.Variable)	},
			{ Types.unknown		, ColorUtility.ToHtmlStringRGB(Config.Language.Colors.Unknown)	},
			{ Types.symbol		, ColorUtility.ToHtmlStringRGB(Config.Language.Colors.Symbol)	},
			{ Types.literal		, ColorUtility.ToHtmlStringRGB(Config.Language.Colors.Literal)	},
			{ Types.type		, ColorUtility.ToHtmlStringRGB(Config.Language.Colors.Type)		},
			{ Types.comment		, ColorUtility.ToHtmlStringRGB(Config.Language.Colors.Comment)	}
		};
	}

	/// <summary>
	/// Converts a line (string) into a list of colors
	/// This stupid function is probably more complicated than the actual tokenizer
	/// </summary>
	/// <returns>Array of color types that correspond with each character in the string</returns>
	public Types[] ParseLineToColorList(string line, ScriptEditor.Context lcontext) {
		Types[] colors = new Types[line.Length];

		if (string.IsNullOrWhiteSpace(line)) 
			return Enumerable.Repeat(Types.unassigned, line.Length).ToArray();

		DeleteOutOfScope(line, lcontext, out int indent);

		HighlightSymbolsDigits(line, ref colors);

		// turn everything else into name ig? and then specify existing after
		for (int i = 0; i < line.Length; i++)
			colors[i] = (colors[i] == Types.unassigned && line[i] != ' ') ? Types.unknown : colors[i];

		// fix numbers part of names
		for (int i = 1; i < line.Length; i++) {
			if ((char.IsDigit(line[i]) || line[i] == '_') && colors[i - 1] == Types.unknown)
				colors[i] = Types.unknown;
		}

		HandleStringLiterals(line, ref colors);

		FindNewNames(line, lcontext, colors, indent);

		HighlightExistingNames(line, lcontext, ref colors);

		HandleComments(line, lcontext, ref colors);

		return colors;
	}

	void DeleteOutOfScope(string line, ScriptEditor.Context lcontext, out int indent) {
		// find current indentation
		int i = 0;
		while (i < line.Length && line[i] == ' ') i++;

		// -1000 readability +1 speed
		/*lcontext.Variables =

			// set all -1 indentations to current
			lcontext.Variables.Select(v =>
			(v.IndentLevel < 0 && // neg
				(v.IndentLevel != int.MinValue ? // not an indentation at zero 
					i > -v.IndentLevel : // normal check, we went up (cant stay or go down)
					i > 0 // we went up from 0
			)) ? 
				new ScriptEditor.LCVariable() {
					Name = v.Name,
					Type = v.Type,
					IndentLevel = i
				}
			: v)

			// remove all vars with lower indentation
			.Where(v => v.IndentLevel >= i).ToList(); */

		List<int> toRemove = new();
		for (int v = 0; v < lcontext.Variables.Count; v++) {
			var vr = lcontext.Variables[v];
			if (vr.IndentLevel < 0) {
				bool wentUp = i > -vr.IndentLevel;
				if (vr.IndentLevel == int.MinValue)
					wentUp = i > 0;

				if (!wentUp) toRemove.Add(v);

				vr.IndentLevel = i;
				lcontext.Variables[v] = vr;
			}

			// remove all vars with higher indentation
			if (vr.IndentLevel > i) {
				if (!toRemove.Contains(v))
					toRemove.Add(v);
			}
		}

		toRemove.Sort(); // probably can remove this cuz it should be sorted by default
		toRemove.Reverse(); // go in reverse order

		// remove
		foreach(int v in toRemove) {
			lcontext.Variables.RemoveAt(v);
		}

		// set the out value
		indent = i;
	}

	void HighlightSymbolsDigits(string line, ref Types[] colors) {
		for (int i = 0; i < line.Length; i++) {
			char c = line[i];
			if (c == ' ') { } // ignore spaces, leave as undef
			else if (char.IsDigit(c)) colors[i] = Types.literal;
			else if (!char.IsLetter(c)) colors[i] = Types.symbol;
		}
	}

	void HandleStringLiterals(string line, ref Types[] colors) {
		for (int i = 0; i < line.Length; i++) {
			char c = line[i];
			if (c == '"' || c == '\'') {
				colors[i] = Types.literal; // a little repeating isnt too bad
				i++;
				while (i < line.Length && line[i] != c) {
					colors[i] = Types.literal;
					i++;
				}

				if (i < line.Length)
					colors[i] = Types.literal;
			}
		}
	}

	void FindNewNames(
		string line,
		ScriptEditor.Context lcontext,
		Types[] colors,
		int indent) {

		// ordered by likelihood for optimization

		if (FindNewVariables(line, lcontext, colors, indent)) return;
		
		if (FindNewFunctions(line, lcontext, colors, indent)) return;

		FindNewTypes(line, lcontext, colors, indent);
	}

	bool FindNewVariables(string line, ScriptEditor.Context lcontext, Types[] colors, int indent) {
		bool foundAnyVariables = false;

		for (int i = line.Length - 1; i >= 0; i--) { // another goddamn loop. but backwards. 
			if (line[i] == '=' && colors[i] == Types.symbol) {
				i--; // skip the =

				// find the name before this
				while (i > 0 && colors[i] == Types.unassigned) i--; // skip spaces

				StringBuilder sb = new(); // could slice but lazy
				while (i >= 0 && colors[i] == Types.unknown) { // find entire name
					sb.Insert(0, line[i]);
					i--;
				}

				// dont highlight members
				while (i > 0 && colors[i] == Types.unassigned) i--; // skip spaces
				bool isMember = i >= 0 && line[i] == '.' && colors[i] == Types.symbol;

				string newName = sb.ToString();
				if (newName != "" && !isMember) {
					lcontext.Variables.Add(new() {
						Name = newName,
						Type = 0,
						IndentLevel = indent
					});

					foundAnyVariables = true;
				}
			}
		}

		return foundAnyVariables;
	}

	bool FindNewFunctions(string line, ScriptEditor.Context lcontext, Types[] colors, int indent) {
		for (int i = line.Length - 1; i > 0; i--) {
			if (line[i] == ':' && colors[i] == Types.symbol &&
				IsFunctionDeclaration(
					line,
					colors,
					i,
					out List<string> args,
					out string newName)) {

				lcontext.Variables.Add(new() {
					Name = newName,
					Type = ScriptEditor.LCVariable.Types.MembFunc,
					IndentLevel = indent
				});

				foreach (string arg in args) {
					lcontext.Variables.Add(new() {
						Name = arg,
						Type = 0,
						IndentLevel = indent != 0 ? -indent : int.MinValue// it will be handled on the next line
					});
				}

				return true;
			}
		}
		return false;
	}

	bool IsFunctionDeclaration(string line, Types[] colors, int i, out List<string> args, out string name) { // holy fuck this code sucks
		args = new();
		name = "";

		i--; // skip :
		while (i > 0 && colors[i] == Types.unassigned) i--; // skip spaces
		if (i < 0) return false; // should be on ) now
		if (line[i] != ')') return false;

		// find args
		List<string> tempArgs = new();
		StringBuilder sb = new();
		int depth = 0;
		while (i > 0) {
			if (line[i] == '(') depth++;
			else if (line[i] == ')') depth--;
			if (line[i] == '(' && depth == 0) break;

			if (colors[i] != Types.unknown && sb.ToString() != "") { // realise the arg
				tempArgs.Add(sb.ToString());
				sb.Clear();
			} else
			if (colors[i] == Types.unknown) { // build the arg
				sb.Insert(0, line[i]);
			}

			i--;
		}
		tempArgs.Add(sb.ToString());
		// should be at ( now or 0
		if (i == 0) return false;
		while (i > 0 && colors[i] != Types.unknown) i--;
		if (i < 0) return false;

		sb = new(); // if performance is needed, replace with slice
		while (i > 0 && colors[i] == Types.unknown) {
			sb.Insert(0, line[i]);
			i--;
		}

		args = tempArgs;
		name = sb.ToString();

		return true;
	}

	bool FindNewTypes(string line, ScriptEditor.Context lcontext, Types[] colors, int indent) {
		int i = line.Length - 1;
		while (i > 0 && !(line[i] == ':' && colors[i] == Types.symbol)) i--;
		if (i < 0) return false;
		if (!(line[i] == ':' && colors[i] == Types.symbol)) return false;
		i--;
		if (colors[i] != Types.unknown) return false;

		// get name
		StringBuilder sb = new();
		while (i >= 0 && colors[i] == Types.unknown) {
			sb.Insert(0, line[i]);
			i--;
		}
		if (i != -1) { // has to be at the start
			while (i > 0 && colors[i] == Types.unassigned) i--;
			if (i != -1) return false; // has to be at the start
		}
		// this is a new type
		lcontext.Variables.Add(new() {
			Name = sb.ToString(),
			Type = ScriptEditor.LCVariable.Types.Type,
			IndentLevel = indent
		});

		// this is now a thing
		lcontext.Variables.Add(new() {
			Name = "this",
			Type = ScriptEditor.LCVariable.Types.Type,
			IndentLevel = indent != 0 ? -indent : int.MinValue
		});

		return true;
	}

	void HighlightExistingNames(
		string line,
		ScriptEditor.Context lcontext,
		ref Types[] colors) {

		StringBuilder sb = new(); // reset sb
		for (int i = 0; i < line.Length; i++) {
			if (colors[i] == Types.unknown) {
				sb.Append(line[i]);
			} else {
				CheckName(line, lcontext, ref colors, sb, i);
			}
		}
		CheckName(line, lcontext, ref colors, sb, line.Length);
	}

	void CheckName(
		string line,
		ScriptEditor.Context lcontext,
		ref Types[] colors,
		StringBuilder sb,
		int i) {

		string name = sb.ToString();
		sb.Clear();

		if (name == "") return;

		// fill existing names
		int findName = lcontext.Variables.FindIndex(v => v.Name == name);
		bool exists = findName != -1;

		// fill members and functions (including internal)
		int indexBeforeName = i - name.Length - 1;
		while (indexBeforeName > 0 && colors[indexBeforeName] == Types.unassigned) indexBeforeName--;
		bool isMember = indexBeforeName > 0 && line[indexBeforeName] == '.';

		int findParentheses = i;
		while (findParentheses < line.Length && colors[findParentheses] == Types.unassigned) findParentheses++;
		bool isUnknownFunction = findParentheses < line.Length && line[findParentheses] == '(';

/*
		bool isMember = // might refactor this ugly block sometime but if it works dont fix it
			indexBeforeName > 1 &&								// has to be smth before the .
			line[indexBeforeName] == '.' &&						// has to be a dot
			colors[indexBeforeName] == Types.symbol &&			// dot is symbol
			(colors[indexBeforeName - 1] == Types.func ||	//
			colors[indexBeforeName - 1] == Types.variable ||	//
			colors[indexBeforeName - 1] == Types.type);			// thing before dot exists
*/

		bool isFunction =
			(exists && lcontext.Variables[findName].Type == ScriptEditor.LCVariable.Types.MembFunc) ||
			Config.Language.Internal.AllMethods.Contains(name);

		bool isType = (exists && lcontext.Variables[findName].Type == ScriptEditor.LCVariable.Types.Type);

		bool isLiteral = Config.Language.Internal.AllLiterals.Contains(name);

		bool isKeyword = Config.Language.Internal.AllKeywords.Contains(name);

		// ordered by prececdence (lower is higher) also there has to be a better way of doing this man
		Types toHighlight = Types.unknown;
		if (exists || isMember)					toHighlight = Types.variable;
		if (isFunction || isUnknownFunction)	toHighlight = Types.func;
		if (isType)								toHighlight = Types.type;
		if (isLiteral)							toHighlight = Types.literal;
		if (isKeyword)							toHighlight = Types.keyword;

		Array.Fill(colors, toHighlight, i - name.Length, name.Length);
	}

	void HandleComments(
		string line,
		ScriptEditor.Context lcontext,
		ref Types[] colors) {
		
		int state = lcontext.InComment ? 2 : 0; // 0-normal, 1-in comment, 2-in multiline
		
		for (int i = 0; i < line.Length; i++) {
			if (line[i] == '-' && colors[i] == Types.symbol) {
				int count = 0;
				while (i < line.Length) {
					if (line[i] == '-' && colors[i] == Types.symbol)
						count++;
					else break;
					
					i++;
				}
				// toggle comment states
				if (count == 2) state = state == 1 ? 0 : // toggle state 1
						(state == 2 ? 2 : 1); // maintain state 2, then switch to single

				else if (count == 3) state = state == 2 ? 0 : 2;
				// dont need to worry about long dashes

				// comment out the dashes
				if (count > 1)
					Array.Fill(colors, Types.comment, i - count, count);
			}

			lcontext.InComment = state != 0;
			if (i < line.Length && lcontext.InComment)
				colors[i] = Types.comment;
		}
		if (state != 2) lcontext.InComment = false; // reset unless in multiline
	}

	// what drugs were used to make this war crime
	public string TypeArrayToString(Types[] array) => new(array.Select(t => "_KFVUSLT#"[(int)t]).ToArray());

	public string TagLine(string line, Types[] types) {
		string generateTag(Types type)
			=> $"<#{TypeToHex[type]}>";
		string endColor = "</color>";

		StringBuilder sb = new();
		Types last = Types.unassigned;
		for (int i = 0; i < line.Length; i++) {
			Types current = types[i];
			if (current != last || i == 0) {
				if (i != 0) sb.Append(endColor);

				while (i < line.Length && types[i] == Types.unassigned) {
					sb.Append(line[i]);
					i++;
				}

				if (i < line.Length) {
					current = types[i];
					sb.Append(generateTag(current));
				}
			}
			last = current;

			if (i < line.Length)
				sb.Append(line[i]);
		}
		return sb.ToString();
	}
}