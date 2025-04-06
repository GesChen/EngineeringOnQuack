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
}