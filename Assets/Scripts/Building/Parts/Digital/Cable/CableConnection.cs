using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableConnection
{
	public Cable Cable;
	public Part Part;

	public CableConnection(Cable cable, Part part) {
		Cable = cable;
		Part = part;
	}
}