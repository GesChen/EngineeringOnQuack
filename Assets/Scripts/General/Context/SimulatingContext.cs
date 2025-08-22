using System;

namespace Contexts {
	public class Simulating : IContext {
		public IContext Parent { get; set; }
		public Type ParentType => typeof(Main);
		public Simulating(IContext parent) => ((IContext)this).SetParent(parent);
		public Simulating() { }
	}
}