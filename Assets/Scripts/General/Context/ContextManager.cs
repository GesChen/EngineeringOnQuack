using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Codice.CM.Common.CmCallContext;

public class ContextManager {
	private static IContext _current;

	public static IContext Current => _current;

	public static void EnterContext(IContext context) {
		_current = context;
	}

	public static void EnterContext<T>() where T : IContext {
		if (IsInContext<T>()) return;

		// ????
		_current = RerouteContextTo<T>(_current);
	}

	// the funky thing that i dont know what to call
	// this is the worst algorithm i have ever come up with
	// this code is absolute dogshit just saying
	// giving good speed tho ~.02ms or a bit hiehger
	public static IContext RerouteContextTo<T>(IContext cur) where T : IContext {
		// build the context ancestry for current
		List<IContext> curAncestry = new();
		List<Type> typeAncestry = new();
		IContext traverse = cur;
		while (traverse != null) {
			curAncestry.Add(traverse);
			typeAncestry.Add(traverse.GetType());

			traverse = traverse.Parent;
		}

		Dictionary<Type, IContext> ancestryMap = new();
		for (int i = 0; i < typeAncestry.Count; i++) {
			ancestryMap[typeAncestry[i]] = curAncestry[i];
		}

		// keep making parents of target until the parent of one is inside a's ancestry
		IContext targetInstance = Activator.CreateInstance<T>();
		IContext seek = targetInstance;

		List<IContext> newContexts = new();
		while (seek != null) {
			newContexts.Insert(0, seek);

			Type parentType = seek.ParentType;
			if (ancestryMap.TryGetValue(parentType, out IContext parent)) {
				// found match
				// relink the new ones
				foreach (var nc in newContexts) {
					nc.SetParent(parent);
					parent = nc;
				}

				break;
			}

			seek = (IContext)Activator.CreateInstance(parentType);
		}

		return targetInstance;
	}

	public static void ExitContext() {
		_current = _current?.Parent;
	}

	public static bool IsInContext<T>() where T : IContext {
		return IsInContext<T>(_current);
	}

	static bool IsInContext<T>(IContext context) {
		while (context != null) {
			if (context is T) return true;
			context = context.Parent;
		}
		return false;
	}
}