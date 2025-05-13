using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class Config {
	public static class Language {
		public static readonly string VERSION = "01";
		public static readonly bool DEBUG = false;
		public static readonly int MaxContainerSerializeLength = 128;
		public static readonly int SpacesPerTab = 4;
		public static readonly bool IndentWithTabs = true;
		public static readonly int MaxWhileLoopIters = 1024;
		public static readonly int RecursionDepthLimit = 1024;

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
}