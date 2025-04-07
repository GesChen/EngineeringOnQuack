using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LanguageConfig
{
	public const string VERSION = "01";
	public const bool DEBUG = true;
	public const int MaxContainerSerializeLength = 128;
	public const int SpacesPerTab = 4;
	public const int MaxWhileLoopIters = 1024;
	public const int RecursionDepthLimit = 1024;

	public static class Internal {
		public static readonly HashSet<string> AllMethods = new() {
			"breakpoint",
			"print",
			"num",
			"bool",
			"str",
			"list",
			"dict",
			"abs",
			"sqrt",
			"round",
			"sum",
			"max",
			"min",
		};

		public static readonly HashSet<string> AllLiterals = new() {
			"true",
			"false"
		};

		public static readonly HashSet<string> AllTypes = new() {
			"Number",
			"String",
			"Bool",
			"List",
			"Dict",
			"Function",
			"Error",
		};

		public static readonly HashSet<string> AllKeywords = new() {
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
	}

	public static class Colors {
		public static readonly Color Keyword	= new(206/255f,	23/255f,	23/255f);
		public static readonly Color Function	= new(230/255f,	121/255f,	255/255f);
		public static readonly Color Variable	= new(157/255f,	220/255f,	253/255f);
		public static readonly Color Unknown	= new(255/255f,	255/255f,	255/255f);
		public static readonly Color Symbol		= new(255/255f,	255/255f,	255/255f);
		public static readonly Color Literal	= new(19/255f,	223/255f,	19/255f);
		public static readonly Color Type		= new(242/255f,	175/255f,	22/255f);
		public static readonly Color Comment	= new(101/255f,	101/255f,	101/255f);
	}
}