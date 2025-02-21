using System.Collections;
using System.Collections.Generic;

public partial class Primitive : Data {
	public partial class Function : Primitive {
		// functions dont have methods or instance vars
		public static Type InternalType = new("Function", new Dictionary<string, Data>()); 

		public Section Script; // user defined function code

		public Function(Section script) : base(InternalType) { // user defined function constructor
			Script = script;
		}

		// internal function code below
		public delegate Data InternalFunctionDelegate(List<Data> args);
		public bool IsInternalFunction = false;
		public InternalFunctionDelegate InternalFunction;
	
		public Function(InternalFunctionDelegate internalFunction) : base(InternalType) {
			IsInternalFunction = true;
			InternalFunction = internalFunction;
		}
	}
}