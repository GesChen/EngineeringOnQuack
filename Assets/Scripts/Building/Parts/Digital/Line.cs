using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	public override string ToString() {
		if (LineType == LineTypeEnum.Line)
			return $"Line ({Tokens.Count}): {OriginalString}";
		else
			return $"SubSection: {Section}";
	}
}
