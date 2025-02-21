using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token
{
	public partial class Name : Token
	{
		public string StringValue;

		public Name(string name)
		{
			StringValue = name;
		}
	}
}
