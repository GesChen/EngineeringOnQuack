public interface IContext {
	string Name { get; }
	IContext Parent { get; }
}