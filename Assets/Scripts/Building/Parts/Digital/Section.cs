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

	public override string ToString() {
		return $"Section: {string.Join('\n', Lines)}";
	}
}
