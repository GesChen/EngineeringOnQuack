using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token {
	public partial class Keyword : Token {
		public static readonly List<string> Keywords = new()
		{
			"if",
			"else",
			"for",
			"while",
			"break",
			"continue",
			"pass",
			"return",
			"try",
			"except",
			"finally",
			"raise"
		};
		public static readonly HashSet<string> KeywordsHashSet = new(Keywords);

		public string StringValue;

		public Keyword(string keyword) {
			StringValue = keyword;
		}

		public override string ToString() {
			return $"#K {StringValue}";
		}
	}
}
