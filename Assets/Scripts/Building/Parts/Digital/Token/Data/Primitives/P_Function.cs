using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : Data {
	public partial class Function : Primitive {
		public static Function Default = new(new(), new()); // "yeah ik what that code does"

		// functions dont have methods or instance vars
		public static Type InternalType = new("Function", new Dictionary<string, Data>());

		public enum FunctionTypeEnum {
			UserDefined,
			UserDefinedInline,
			Internal
		}
		public FunctionTypeEnum FunctionType;

		#region user defined
		public List<Name> Parameters;
		public Script Script; // normal
		public List<Token> InlineDefinition;

		// normal function
		public Function(List<Name> parameters, Script script) : base(InternalType) {
			FunctionType = FunctionTypeEnum.UserDefined;
			Parameters = parameters;
			Script = script;
		}

		// inline function
		public Function(List<Name> parameters, List<Token> definition) : base(InternalType) {
			FunctionType = FunctionTypeEnum.UserDefinedInline;
			Parameters = parameters;
			InlineDefinition = definition;
		}
		#endregion

		#region internal
		public delegate Data InternalFunctionDelegate(Data thisReference, List<Data> args);
		public InternalFunctionDelegate InternalFunction;
	
		public Function(InternalFunctionDelegate internalFunction) : base(InternalType) {
			FunctionType = FunctionTypeEnum.Internal;
			InternalFunction = internalFunction;
		}
		#endregion

		public override string ToString() {
			return $"Function object \"{Name}\"";
		}
	}
}