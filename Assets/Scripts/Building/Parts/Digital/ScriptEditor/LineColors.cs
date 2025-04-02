using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Linq;

public class LineColors : MonoBehaviour {

	[Header("temporary colors")]
	public Color keywordColor;
	public Color existingColor;
	public Color nameColor;
	public Color symbolColor;
	public Color literalColor;

	public enum Types {
		keyword,
		existing,
		name,
		symbol,
		literal
	}

	/// <summary>
	/// Converts a line (string) into a list of colors
	/// </summary>
	/// <returns>Array of color types that correspond with each character in the string</returns>
	public Types[] LineColorTypesArray(string line) {
		Types[] colors = new Types[line.Length];

		StringBuilder sb = new(" "); // look for keywords
		List<string> possibleKeywords = new(Token.Keyword.Keywords);
		bool kwMatches(string kw) => kw[sb.Length - 1] == sb[^1];
		for (int i = 0; i < line.Length; i++) {
			char c = line[i];
			if (char.IsSymbol(c)) colors[i] = Types.symbol;
			else if (char.IsDigit(c)) colors[i] = Types.literal;

			possibleKeywords = possibleKeywords.Where(k => kwMatches(k)).ToList();
			if (possibleKeywords.Count == 0) { // no keywords match
				sb.Clear(); // reset sb
				possibleKeywords = new(Token.Keyword.Keywords); // start again
			} else 
			if (possibleKeywords.Count == 1) { // found matching keyword
				
				for (int h = i - sb.Length; h < i; h++) // highlight the keyword
					colors[h] = Types.keyword;
			}
			sb.Append(c);
		}


		return colors;
	}

}