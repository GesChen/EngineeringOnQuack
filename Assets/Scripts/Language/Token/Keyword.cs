using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token {
	public partial class T_Keyword : Token {
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

		public enum Kws {
			None,
			If,
			Else,
			For,
			While,
			Break,
			Continue,
			Pass,
			Return,
			Try,
			Except,
			Finally,
			Raise,
		}

		public Kws Value;
		public string StringValue;

		public T_Keyword(string keyword) {
			StringValue = keyword;
			Value = keyword switch {
				"if"		=> Kws.If,
				"else"		=> Kws.Else,
				"for"		=> Kws.For,
				"while"		=> Kws.While,
				"break"		=> Kws.Break,
				"continue"	=> Kws.Continue,
				"pass"		=> Kws.Pass,
				"return"	=> Kws.Return,
				"try"		=> Kws.Try,
				"except"	=> Kws.Except,
				"finally"	=> Kws.Finally,
				"raise"		=> Kws.Raise,
				_ => Kws.None
			};
		}

		public override string ToString() {
			return $"#K {StringValue}";
		}
	}
}
