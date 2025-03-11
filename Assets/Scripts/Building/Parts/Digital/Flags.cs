using System;

[Flags]
public enum Flags {
	None				= 1 << 0,
	InLoop				= 1 << 1,
	ExpectSectionNext	= 1 << 2,
	ReturnData			= 1 << 3,
	DontRunNextLine		= 1 << 4,
	IfSucceeded			= 1 << 5,
	IfFailed			= 1 << 6,
	EnterFor			= 1 << 7,
	EnterWhile			= 1 << 8,
	Try					= 1 << 9,
	TrySucceded			= 1 << 10,
	TryFailed			= 1 << 11,
	MakeFunction		= 1 << 12,
	MakeClass			= 1 << 13,
	SkipToEnd			= 1 << 14,
	Break				= 1 << 15,
	Continue			= 1 << 16
}