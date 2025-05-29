public class TemplateContext : IContext {
	public string Name => "Template Context";
	public IContext Parent { get; }

	public TemplateContext(IContext parent = null) {
		Parent = parent;
	}
}