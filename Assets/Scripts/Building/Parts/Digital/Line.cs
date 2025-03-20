using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Line
{
	public int RealLineNumber;
	public string OriginalString;
	
	public enum LineTypeEnum
	{
		Line,
		Section
	}

	public LineTypeEnum LineType;
	
	public List<Token> Tokens;
	public Section Section;

	public Line(int lineNum, string lineString, List<Token> tokens)
	{
		RealLineNumber = lineNum;
		OriginalString = lineString;
		LineType = LineTypeEnum.Line;
		Tokens = tokens;
	}
	public Line(int lineNum, string lineString, Section section)
	{
		RealLineNumber = lineNum;
		OriginalString = lineString;
		LineType = LineTypeEnum.Section;
		Section = section;
	}

	public Line CopyWithNewTokens(List<Token> newTokens) {
		return new(
			RealLineNumber,
			OriginalString,
			newTokens);
	}

	public override string ToString() {
		if (LineType == LineTypeEnum.Line)
			return $"Line {RealLineNumber} ({Tokens.Count}): {OriginalString}";
		else
			return $"Sub{Section}";
	}
	public string TokenList() {
		return string.Join(" ", Tokens.Select(c=>$"<{c}>"));
	}

	public Line DeepCopy() {
		return new(RealLineNumber, string.Copy(OriginalString), new List<Token>(Tokens));
	}
}