using System;

[Flags]
public enum Flags {
	InLoop				= 1 << 0,
	ExpectSectionNext	= 1 << 1,
	ReturnData			= 1 << 2,
	DontRunNextLine		= 1 << 3,
	IfSucceeded			= 1 << 4,
	IfFailed			= 1 << 5,
	EnterFor			= 1 << 6,
	EnterWhile			= 1 << 7,
	Try					= 1 << 8,
	TrySucceded			= 1 << 9,
	TryFailed			= 1 << 10,
	MakeFunction		= 1 << 11,
	MakeClass			= 1 << 12,
	SkipToEnd			= 1 << 13,
	Break				= 1 << 14,
	Continue			= 1 << 15
}