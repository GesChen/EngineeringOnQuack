using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token
{
	public partial class Operator : Token
	{
		public static List<string> UnaryOperators = new()
		{
			"+",
			"-",
			"!"
		};
		public static List<string> ArithmeticOperators = new()
		{
			"+",
			"-",
			"*",
			"/",
			"%",
			"^"
		};
		public static List<string> ComparisonOperators = new()
		{
			"==",
			"!=",
			">",
			"<",
			">=",
			"<="
		};
		public static List<string> LogicalOperators = new()
		{
			"&&",
			"||",
			"!&",
			"!|",
			"!!",
			"!"
		};
		public static List<string> AssignmentOperators = new()
		{
			"=",
			"+=",
			"-=",
			"++",
			"--",
			"*=",
			"/=",
			"^=",
			"%="
		};
		public static List<string> SpecialOperators = new()
		{
			".",
			",",
			"...",
			":"
		};
		public static List<string> AllOperators = new()
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
			"++",
			"--",
			"*=",
			"/=",
			"^=",
			"%=",
			".",
			",",
			"...",
			":"
		};

		public enum OperatorType
		{
			Arithmetic,
			Comparison,
			Logical,
			Assignment,
			Special
		}

		public string StringValue { get; private set; }

		public OperatorType Type;
		public bool IsUnary;

		public Operator(string op)
		{
			SetStringValue(op);
		}

		public void SetStringValue(string op)
		{
			StringValue = op;

			if (ArithmeticOperators.Contains(op)) Type = OperatorType.Arithmetic;
			else if (ComparisonOperators.Contains(op)) Type = OperatorType.Comparison;
			else if (LogicalOperators.Contains(op)) Type = OperatorType.Logical;
			else if (AssignmentOperators.Contains(op)) Type = OperatorType.Assignment;
			else if (SpecialOperators.Contains(op)) Type = OperatorType.Special;

			IsUnary = UnaryOperators.Contains(op);
		}
	}
}
