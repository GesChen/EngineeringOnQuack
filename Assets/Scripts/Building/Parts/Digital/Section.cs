using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Section
{
	public Line[] Lines;

	public Section(Line[] lines)
	{
		Lines = lines;
	}
	public Section() {
		Lines = new Line[0];
	}

	public override string ToString() {
		return $"Section ({Lines.Length}): {string.Join('\n', Lines.ToList())}";
	}
}
