using System.Collections;
using System.Collections.Generic;

public abstract partial class Primitive : Data {
	public partial class Function : Primitive {
		public static Function Default = new();

		// functions dont have methods or instance vars
		public static Type InternalType = new("Function", new Dictionary<string, Data>());

		public enum FunctionTypeEnum {
			UserDefined,
			UserDefinedInline,
			Internal
		}
		public FunctionTypeEnum FunctionType;

		#region user defined
		public string[] Parameters;
		public Section Script ; // normal
		public Token[] InlineDefinition;

		// normal function
		public Function(string[] parameters, Section script) : base(InternalType) {
			FunctionType = FunctionTypeEnum.UserDefined;
			Parameters = parameters;
			Script = script;
		}

		// inline function
		public Function(string[] parameters, Token[] definition) : base(InternalType) {
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

		public Function(Function original) : base(original) { // copy constructor
			FunctionType		= original.FunctionType;
			Parameters			= original.Parameters;
			Script				= original.Script;
			InlineDefinition	= original.InlineDefinition;
			InternalFunction	= original.InternalFunction;
		}
		public Function() : base(InternalType) {
			// todo?
		}
		
		public override string ToString() {
			return $"Function object";
		}

		public override Data Copy() {
			return new Function(this);
		}
	}
}