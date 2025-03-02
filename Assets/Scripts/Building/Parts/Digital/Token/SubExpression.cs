using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token {
	public partial class SubExpression : Token {
		public enum Source {
			Parentheses,
			Brackets,
			Braces
		}

		public List<Token> Tokens;
		public Source From;

		public SubExpression(List<Token> tokens, Source from) {
			Tokens = tokens;
			From = from;
		}
	}
}

