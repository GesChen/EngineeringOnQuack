using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;
using System;

public class SyntaxHighlighter : MonoBehaviour {

	[Header("temporary colors")]
	public Color keywordColor;
	public Color memberFunctionColor;
	public Color variableColor;
	public Color unknownColor;
	public Color symbolColor;
	public Color literalColor;

	public enum Types {
		unassigned,
		keyword,
		membfunc,
		variable,
		unknown,
		symbol,
		literal
	}



	/// <summary>
	/// Converts a line (string) into a list of colors
	/// </summary>
	/// <returns>Array of color types that correspond with each character in the string</returns>
	public Types[] LineColorTypesArray(string line, ref ScriptEditor.LocalContext lcontext) {
		Types[] colors = new Types[line.Length];

		// this fat loop just highlights keywords, symbols and digits

		StringBuilder sb = new(); // look for keywords
		List<string> possibleKeywords = new(Token.Keyword.Keywords);
		bool kwMatches(string kw) => kw[sb.Length - 1] == sb[^1];
		for (int i = 0; i < line.Length; i++) {
			char c = line[i];
			if (c == ' ') { } // ignore spaces, leave as undef
			else if (char.IsDigit(c)) colors[i] = Types.literal;
			else if (!char.IsLetter(c)) colors[i] = Types.symbol;

			if (i != 0) possibleKeywords = possibleKeywords.Where(k => kwMatches(k)).ToList();

			bool highlightKw = possibleKeywords.Count == 1 && sb.Length == possibleKeywords[0].Length;
			if (highlightKw) // found matching keyword
				Array.Fill(colors, Types.keyword, i - sb.Length, sb.Length); // highlight the keyword

			if (possibleKeywords.Count == 0 || highlightKw) { // no keywords match
				sb.Clear(); // reset sb
				possibleKeywords = new(Token.Keyword.Keywords); // start again
			}
			sb.Append(c);
		}

		// turn everything else into name ig? and then specify existing after
		for (int i = 0; i < line.Length; i++)
			colors[i] = (colors[i] == Types.unassigned && line[i] != ' ') ? Types.unknown : colors[i];

		// fix numbers part of names
		for (int i = 1; i < line.Length; i++) {
			if ((char.IsDigit(line[i]) || line[i] == '_') && colors[i - 1] == Types.unknown)
				colors[i] = Types.unknown;
		}

		// determine existing names
		sb = new(); // reset sb
		for (int i = 0; i < line.Length; i++) {
			if (colors[i] == Types.unknown) {
				sb.Append(line[i]);
			}
			else {
				string name = sb.ToString();
				sb.Clear();

				if (name == "") continue;

				// fill existing names
				int findName = lcontext.Variables.FindIndex(v => v.Name == name);
				bool exists = findName != -1;
				if (exists)
					Array.Fill(colors, Types.variable, i - name.Length, name.Length);

				// fill members or functions (including internal)
				if ((exists && lcontext.Variables[findName].Type == 1) ||
					lcontext.InternalFunctions.Contains(name)) {
					Array.Fill(colors, Types.membfunc, i - name.Length, name.Length);
				}
			}
		}

		// handle comments

		return colors;
	}

	// what drugs were used to make this war crime
	public string TypeArrayToString(Types[] array) => new(array.Select(t => "_KMVUSL"[(int)t]).ToArray());
}