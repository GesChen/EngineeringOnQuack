using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextManager : Singleton<ContextManager> {
	private IContext _current;

	public IContext Current => _current;

	public void EnterContext(IContext context) {
		_current = context;
	}

	public void ExitContext() {
		_current = _current?.Parent;
	}

	public bool IsInContext<T>() where T : IContext {
		return IsInContext<T>(_current);
	}
	bool IsInContext<T>(IContext context) {
		while (context != null) {
			if (context is T) return true;
			context = context.Parent;
		}
		return false;
	}

}