using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Section
{
	public List<Line> Lines;

	public Section(List<Line> lines)
	{
		Lines = lines;
	}
	public Section() {
		Lines = new();
	}

	public override string ToString() {
		return $"Section ({Lines.Count}): {string.Join('\n', Lines)}";
	}
}
