using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

public partial class Token {
	public partial class Operator : Token {
		public static readonly List<string> UnaryOperatorStrings			= new()
		{
			"+",
			"-",
			"!"
		};
		public static readonly List<string> ArithmeticOperatorStrings		= new()
		{
			"+",
			"-",
			"*",
			"/",
			"%",
			"^"
		};
		public static readonly List<string> ComparisonOperatorStrings		= new()
		{
			"==",
			"!=",
			">",
			"<",
			">=",
			"<="
		};
		public static readonly List<string> LogicalOperatorStrings		= new()
		{
			"&&",
			"||",
			"!&",
			"!|",
			"!!",
			"!"
		};
		public static readonly List<string> AssignmentOperatorStrings		= new()
		{
			"=",
			"+=",
			"-=",
			"*=",
			"/=",
			"^=",
			"%="
		};
		public static readonly List<string> RegionOperatorStrings			= new() {
			"(",
			")",
			"[",
			"]",
			"{",
			"}",
		};
		public static readonly List<string> SpecialOperatorStrings		= new()
		{
			".",
			",",
			"..",
			":"
		};
		public static readonly List<string> AllOperatorStrings			= new()
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
		public static readonly HashSet<string> AllOperatorStringsHashSet	= new(AllOperatorStrings);

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
		
		public static readonly List<Ops> UnaryOperators = new()
		{
			Ops.Plus,
			Ops.Minus,
			Ops.Not
		};
		public static readonly HashSet<Ops> UnaryOperatorsHashSet = new(UnaryOperators);
		public static readonly List<Ops> ArithmeticOperators = new()
		{
			Ops.Plus,
			Ops.Minus,
			Ops.Multiply,
			Ops.Divide,
			Ops.Mod,
			Ops.Power
		};
		public static readonly HashSet<Ops> ArithmeticOperatorsHashSet = new(ArithmeticOperators);
		public static readonly List<Ops> ComparisonOperators = new()
		{
			Ops.Equality,
			Ops.NotEquals,
			Ops.GreaterThan,
			Ops.LessThan,
			Ops.GreaterThanOrEqualTo,
			Ops.LessThanOrEqualTo
		};
		public static readonly HashSet<Ops> ComparisonOperatorsHashSet = new(ComparisonOperators);
		public static readonly List<Ops> LogicalOperators = new()
		{
			Ops.And,
			Ops.Or,
			Ops.Nand,
			Ops.Nor,
			Ops.Xor,
			Ops.Not
		};
		public static readonly HashSet<Ops> LogicalOperatorsHashSet = new(LogicalOperators);
		public static readonly List<Ops> AssignmentOperators = new()
		{
			Ops.Equals,
			Ops.PlusEquals,
			Ops.MinusEquals,
			Ops.MultiplyEquals,
			Ops.DivideEquals,
			Ops.PowerEquals,
			Ops.ModEquals
		};
		public static readonly HashSet<Ops> AssignmentOperatorsHashSet = new(AssignmentOperators);
		public static readonly List<Ops> RegionOperators = new() {
			Ops.OpenParentheses,
			Ops.CloseParentheses,
			Ops.OpenBracket,
			Ops.CloseBracket,
			Ops.OpenBrace,
			Ops.CloseBrace,
		};
		public static readonly HashSet<Ops> RegionOperatorsHashSet = new(RegionOperators);
		public static readonly List<Ops> SpecialOperators = new()
		{
			Ops.Dot,
			Ops.Comma,
			Ops.Ellipsis,
			Ops.Colon
		};
		public static readonly HashSet<Ops> SpecialOperatorsHashSet = new(SpecialOperators);
		public static readonly List<Ops> AllOperators = new()
		{
			Ops.Plus,
			Ops.Minus,
			Ops.Multiply,
			Ops.Divide,
			Ops.Mod,
			Ops.Power,
			Ops.Equality,
			Ops.NotEquals,
			Ops.GreaterThan,
			Ops.LessThan,
			Ops.GreaterThanOrEqualTo,
			Ops.LessThanOrEqualTo,
			Ops.And,
			Ops.Or,
			Ops.Nand,
			Ops.Nor,
			Ops.Xor,
			Ops.Not,
			Ops.Equals,
			Ops.PlusEquals,
			Ops.MinusEquals,
			Ops.MultiplyEquals,
			Ops.DivideEquals,
			Ops.PowerEquals,
			Ops.ModEquals,
			Ops.OpenParentheses,
			Ops.CloseParentheses,
			Ops.OpenBracket,
			Ops.CloseBracket,
			Ops.OpenBrace,
			Ops.CloseBrace,
			Ops.Dot,
			Ops.Comma,
			Ops.Ellipsis,
			Ops.Colon
		};
		public static readonly HashSet<Ops> AllOperatorHashSet = new(AllOperators);
		public static readonly Dictionary<Ops, int> NormalOperatorsPrecedence = new() {
			{ Ops.Plus,					7	},
			{ Ops.Minus,				7	},
			{ Ops.Multiply,				8	},
			{ Ops.Divide,				8	},
			{ Ops.Mod,					8	},
			{ Ops.Power,				9	},
			{ Ops.Equality,				6	},
			{ Ops.NotEquals,			6	},
			{ Ops.GreaterThan,			6	},
			{ Ops.LessThan,				6	},
			{ Ops.GreaterThanOrEqualTo,	6	},
			{ Ops.LessThanOrEqualTo,	6	},
			{ Ops.And,					5	},
			{ Ops.Or,					5	},
			{ Ops.Nand,					5	},
			{ Ops.Nor,					5	},
			{ Ops.Xor,					5	},
		};
		
		public string StringValue { get; private set; }
		public Ops Value { get; private set; }

		public OperatorType Type;
		public bool IsUnary;

		public Operator(string op) {
			SetStringValue(op);
		}

		public void SetStringValue(string op) {
			StringValue = op;

			if (ArithmeticOperatorStrings.Contains(op)) Type = OperatorType.Arithmetic;
			else if (ComparisonOperatorStrings.Contains(op)) Type = OperatorType.Comparison;
			else if (LogicalOperatorStrings.Contains(op)) Type = OperatorType.Logical;
			else if (AssignmentOperatorStrings.Contains(op)) Type = OperatorType.Assignment;
			else if (SpecialOperatorStrings.Contains(op)) Type = OperatorType.Special;

			IsUnary = UnaryOperatorStrings.Contains(op);

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