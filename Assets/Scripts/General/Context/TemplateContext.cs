using System;

public class TemplateContext : IContext {
	public string Name => "TemplateContext";
	public IContext Parent { get; set; }
	public Type ParentType => typeof(TemplateContext);
	public TemplateContext(IContext parent) => ((IContext)this).SetParent(parent);
	public TemplateContext() { }
}