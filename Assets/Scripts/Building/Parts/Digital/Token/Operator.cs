using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

public partial class Token {
	public partial class Operator : Token {
		public static readonly List<string> UnaryOperators			= new()
		{
			"+",
			"-",
			"!"
		};
		public static readonly List<string> ArithmeticOperators		= new()
		{
			"+",
			"-",
			"*",
			"/",
			"%",
			"^"
		};
		public static readonly List<string> ComparisonOperators		= new()
		{
			"==",
			"!=",
			">",
			"<",
			">=",
			"<="
		};
		public static readonly List<string> LogicalOperators		= new()
		{
			"&&",
			"||",
			"!&",
			"!|",
			"!!",
			"!"
		};
		public static readonly List<string> AssignmentOperators		= new()
		{
			"=",
			"+=",
			"-=",
			"*=",
			"/=",
			"^=",
			"%="
		};
		public static readonly List<string> RegionOperators			= new() {
			"(",
			")",
			"[",
			"]",
			"{",
			"}",
		};
		public static readonly List<string> SpecialOperators		= new()
		{
			".",
			",",
			"...",
			":"
		};
		public static readonly List<string> AllOperators			= new()
		{
			"+",
			"-",
			"*",
			"/",
			"%",
			"^",
			"==",
			"!=",
			">",
			"<",
			">=",
			"<=",
			"&&",
			"||",
			"!&",
			"!|",
			"!!",
			"!",
			"=",
			"+=",
			"-=",
			"*=",
			"/=",
			"^=",
			"%=",
			"(",
			")",
			"[",
			"]",
			"{",
			"}",
			".",
			",",
			"..",
			":"
		};
		public static readonly HashSet<string> AllOperatorsHashSet	= new(AllOperators);

		public enum OperatorType {
			Arithmetic,
			Comparison,
			Logical,
			Assignment,
			Special
		}

		public enum Ops {
			None,
			Plus,
			Minus,
			Multiply,
			Divide,
			Mod,
			Power,
			Equality,
			NotEquals,
			GreaterThan,
			LessThan,
			GreaterThanOrEqualTo,
			LessThanOrEqualTo,
			And,
			Or,
			Nand,
			Nor,
			Xor,
			Not,
			Equals,
			PlusEquals,
			MinusEquals,
			MultiplyEquals,
			DivideEquals,
			PowerEquals,
			ModEquals,
			OpenParentheses,
			CloseParentheses,
			OpenBracket,
			CloseBracket,
			OpenBrace,
			CloseBrace,
			Dot,
			Comma,
			Ellipsis,
			Colon
		}

		public string StringValue { get; private set; }
		public Ops Value { get; private set; }

		public OperatorType Type;
		public bool IsUnary;

		public Operator(string op) {
			SetStringValue(op);
		}

		public void SetStringValue(string op) {
			StringValue = op;

			if (ArithmeticOperators.Contains(op)) Type = OperatorType.Arithmetic;
			else if (ComparisonOperators.Contains(op)) Type = OperatorType.Comparison;
			else if (LogicalOperators.Contains(op)) Type = OperatorType.Logical;
			else if (AssignmentOperators.Contains(op)) Type = OperatorType.Assignment;
			else if (SpecialOperators.Contains(op)) Type = OperatorType.Special;

			IsUnary = UnaryOperators.Contains(op);

			Value = op switch {
				"+"		=> Ops.Plus					,
				"-"		=> Ops.Minus				,
				"*"		=> Ops.Multiply				,
				"/"		=> Ops.Divide				,
				"%"		=> Ops.Mod					,
				"^"		=> Ops.Power				,
				"=="	=> Ops.Equality				,
				"!="	=> Ops.NotEquals			,
				">"		=> Ops.GreaterThan			,
				"<"		=> Ops.LessThan				,
				">="	=> Ops.GreaterThanOrEqualTo	,
				"<="	=> Ops.LessThanOrEqualTo	,
				"&&"	=> Ops.And					,
				"||"	=> Ops.Or					,
				"!&"	=> Ops.Nand					,
				"!|"	=> Ops.Nor					,
				"!!"	=> Ops.Xor					,
				"!"		=> Ops.Not					,
				"="		=> Ops.Equals				,
				"+="	=> Ops.PlusEquals			,
				"-="	=> Ops.MinusEquals			,
				"*="	=> Ops.MultiplyEquals		,
				"/="	=> Ops.DivideEquals			,
				"^="	=> Ops.PowerEquals			,
				"%="	=> Ops.ModEquals			,
				"("		=> Ops.OpenParentheses		,
				")"		=> Ops.CloseParentheses		,
				"["		=> Ops.OpenBracket			,
				"]"		=> Ops.CloseBracket			,
				"{"		=> Ops.OpenBrace			,
				"}"		=> Ops.CloseBrace			,
				"."		=> Ops.Dot					,
				","		=> Ops.Comma				,
				".."	=> Ops.Ellipsis				,
				":"		=> Ops.Colon				,
				_		=> Ops.None
			};
		}

		public override string ToString() {
			return $"#O {StringValue}";
		}
	}
}