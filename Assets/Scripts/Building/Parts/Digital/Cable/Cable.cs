using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cable : MonoBehaviour
{
	public CableConnection connectionA;
	public CableConnection connectionB;

	public CableConnection OtherCC(CableConnection cc) {
		if (cc == connectionA) return connectionB;
		if (cc == connectionB) return connectionA;
		throw new("requested cc wasn't either A or B");
	}

	public (CableConnection, CableConnection) Connect(Part a, Part b) {
		CableConnection aCC = new(this, a);
		connectionA = aCC;

		CableConnection bCC = new(this, b);
		connectionB = bCC;

		return (aCC, bCC);
	}

	public CableConnection MakeSingleConnection(Part other, bool sideB = false) {
		CableConnection connection = new(this, other);

		if (sideB)
			connectionB = connection;
		else
			connectionA = connection;

		return connection;
	}

	public override string ToString() {
		if (connectionA == null) return "Cable, cc A disconnected";
		if (connectionB == null) return "Cable, cc B disconnected";

		return $"Cable connecting {connectionA.Part.GetType().Name} -- {connectionB.Part.GetType().Name}";
	}
}