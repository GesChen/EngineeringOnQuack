using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : Data {
	public partial class Function : Primitive {
		public static Function Default = new(new(), new(new())); // "yeah ik what that code does"

		// functions dont have methods or instance vars
		public static Type InternalType = new("Function", new Dictionary<string, Data>());

		// user defined function code
		public List<Name> Parameters;
		public Section Script; 

		public Function(List<Name> parameters, Section script) : base(InternalType) { // user defined function constructor
			Parameters = parameters;
			Script = script;
		}

		public override string ToString() {
			return $"Function object \"{Name}\"";
		}

		// internal function code below
		public delegate Data InternalFunctionDelegate(Data thisReference, List<Data> args);
		public bool IsInternalFunction = false;
		public InternalFunctionDelegate InternalFunction;
	
		public Function(InternalFunctionDelegate internalFunction) : base(InternalType) {
			IsInternalFunction = true;
			InternalFunction = internalFunction;
		}
	}
}