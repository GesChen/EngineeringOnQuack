using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ContextManager {
	static ContextManager() {
		OnContextChanged = null;
	}

	private static IContext _current;

	public static IContext Current => _current;

	static void Changed() { OnContextChanged?.Invoke(_current); }
	public static event Action<IContext> OnContextChanged;

	public static void ForceEnterContext(IContext context) {
		_current = context;

		Changed();
	}

	public static C EnterContext<C>() where C : IContext {
		if (IsInContext(out C instance)) {
			return instance;
		}
		
		// ????
		_current = RerouteContextTo<C>(_current);
		
		Changed();

		return (C)_current;
	}

	// the funky thing that i dont know what to call
	// this is the worst algorithm i have ever come up with
	// this code is absolute dogshit just saying
	// giving good speed tho ~.02ms or a bit hiehger
	public static IContext RerouteContextTo<C>(IContext cur) where C : IContext {
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
		IContext targetInstance = Activator.CreateInstance<C>();
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

	public static bool IsInContext<C>(out C instance) where C : IContext {
		bool isIn = IsInContext(_current, out C tcon);
		instance = tcon;
		return isIn;
	}

	static bool IsInContext<C>(IContext context, out C instance) {
		instance = default;

		while (context != null) {
			if (context is C tcon) {
				instance = tcon;
				return true;
			}
			context = context.Parent;
		}
		return false;
	}

	/// <summary>
	/// Checks if a context is another ie. if the context
	/// argument is a descendant of the C context
	/// </summary>
	/// <typeparam name="C">Check if context is from this</typeparam>
	/// <param name="context">Context to check</param>
	public static bool IsContextOrDescendant(IContext context, IContext checkAgainst) {
		Type type = checkAgainst.GetType();

		while (context != null) {
			if (context.GetType() == type) return true;

			context = context.Parent;
		}

		return false;
	}

	/// <summary>
	/// Is A or B directly related in any way? ie one is the descendant of the other
	/// </summary>
	public static bool AnyDirectRelation(IContext A, IContext B) {
		return IsContextOrDescendant(A, B) || IsContextOrDescendant(B, A);
	}

	public static bool RelatedWithoutMain(IContext A, IContext B) {
		static List<IContext> buildAncestry(IContext of) {
			List<IContext> A = new();
			while (of != null) {
				if (of is not Contexts.Main)
					A.Add(of);
				
				of = of.Parent;
			}
			return A;
		}

		var ancestryA = buildAncestry(A);
		var ancestryB = buildAncestry(B);

		var setB = new HashSet<IContext>(ancestryB); // O(m)
		return ancestryA.Any(x => setB.Contains(x));
	}
}