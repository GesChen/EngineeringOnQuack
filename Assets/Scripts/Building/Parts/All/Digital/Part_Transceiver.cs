using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part_Transceiver : NonStaticPart {
	public override string PartName => "Transceiver";

	public override void HandleCommand(string command, object[] args) {
		if (command == "print") {
			if (args.Length != 1) {
				Debug.LogError(BadArgumentCount(command, 1, args.Length));
				return;
			}

			Debug.Log(args[0]?.ToString() ?? "null");
			return;
		}

		Debug.LogError(UnknownCommand(command));
	}
}