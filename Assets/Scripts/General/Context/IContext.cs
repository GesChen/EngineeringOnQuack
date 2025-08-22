using System;
public interface IContext {
	// can't figure out good way to do this, get it to prevent using the setter
	IContext Parent { get; set; } // just do NOT use the setter, always use setparent
	Type ParentType { get; }
	// always include an empty parameterless constructor if another exists
	void SetParent(IContext parent) {
		if (!ParentType.IsInstanceOfType(parent))
			throw new ArgumentException($"TemplateContext parent must be {ParentType.Name}");
		Parent = parent;
	}
}