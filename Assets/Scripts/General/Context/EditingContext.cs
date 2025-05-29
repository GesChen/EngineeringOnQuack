public class EditingContext : IContext {
	public string Name => "EditingContext";
	public IContext Parent { get; }
	public EditingContext() { Parent = null; }
}


public class SceneSelection : IContext {
	public string Name => "SceneSelection";
	public IContext Parent { get; }
	public SceneSelection(EditingContext parent = null) {
		Parent = parent ?? throw new System.ArgumentNullException(nameof(parent));
	}
}

public class NoSelection : IContext {
	public string Name => "NoSelection";
	public IContext Parent { get; }
	public NoSelection(SceneSelection parent = null) {
		Parent = parent ?? throw new System.ArgumentNullException(nameof(parent));
	}
}

public class SingleSelection : IContext {
	public string Name => "SingleSelection";
	public IContext Parent { get; }
	public SingleSelection(SceneSelection parent = null) {
		Parent = parent ?? throw new System.ArgumentNullException(nameof(parent));
	}
}

public class MultipleSelection : IContext {
	public string Name => "MultipleSelection";
	public IContext Parent { get; }
	public MultipleSelection(SceneSelection parent = null) {
		Parent = parent ?? throw new System.ArgumentNullException(nameof(parent));
	}
}


public class UISelection : IContext {
	public string Name => "UISelection";
	public IContext Parent { get; }
	public UISelection(EditingContext parent = null) {
		Parent = parent ?? throw new System.ArgumentNullException(nameof(parent));
	}
}
