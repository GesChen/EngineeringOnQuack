using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token
{
	public partial class Reference : Token {
		public bool Exists;

		public bool IsInstanceVariable;
		public bool IsListItem;

		public Data Data;
		public Data Parent;

		public int ListIndex;

		// most normal case, x.y where x is parent and y is data
		public Reference(Reference parent, Data data)
		{
			Exists = true;
			IsInstanceVariable = true;
			IsListItem = false;
			Data = data;
			Parent = parent.Data;
			ListIndex = -1;
		}


	}
}