using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PartInternalFunctions {
	public static void ClearSubscriptions() {
		CPU.ClearSubs();
		Transceiver.ClearSubs();
	}

	public static class CPU {
		internal static void ClearSubs() {
			OnPortCalled = null;
		}

		public static event Func<int, int, T_Data> OnPortCalled;
		public static T_Data port(T_Data _, List<T_Data> args) {
			if (args.Count != 1) 
				return Errors.InvalidArgumentCount("port", 1, args.Count);

			if (args[0] is not Primitive.Number num) 
				return Errors.InvalidArgumentType("port", 0, "Number", args[0].Type.Name);

			double idDouble = num.Value;
			if (idDouble != (int)idDouble)
				return Errors.InvalidArgumentType("port", 0, "whole number", "decimal");

			int port = (int)idDouble;
			int intID = args[0].Memory.Interpreter.ID;

			foreach (var handler in OnPortCalled
				.GetInvocationList().Cast<Func<int, int, T_Data>>()) {

				var call = handler?.Invoke(intID, port);
				if (call != null) return call;
			}

			return Errors.BadCode();
		}
	}

	public static class Transceiver {
		internal static void ClearSubs() {
			OnPrintCalled = null;
		}
		public static event Action<int, string> OnPrintCalled;
		public static T_Data print(T_Data thisRef, List<T_Data> args) {
			if (args.Count != 1) return Errors.InvalidArgumentCount("print", 1, args.Count);

			T_Data tryCast = args[0].Cast(Primitive.String.InternalType);
			if (tryCast is Error) return tryCast;

			string message = (tryCast as Primitive.String).Value;

			// tr will be a type_transceiver object
			var tryGetID = thisRef.GetMember("id");
			if (tryGetID is Error) return tryGetID;

			if (tryGetID is not Primitive.Number id
				|| id.Value != (int)id.Value) return Errors.BadCode();

			OnPrintCalled?.Invoke((int)id.Value, message);

			return T_Data.Success;
		}
	}
}