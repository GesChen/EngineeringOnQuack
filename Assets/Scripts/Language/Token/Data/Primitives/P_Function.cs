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
			Constructor,
			Internal
		}
		public FunctionTypeEnum FunctionType;

		#region user defined
		public string[] Parameters;
		public Section Script ; // normal
		public Token[] InlineDefinition;

		// normal function
		public Function(string name, string[] parameters, Section script) : base(InternalType) {
			Name = name;
			FunctionType = FunctionTypeEnum.UserDefined;
			Parameters = parameters;
			Script = script;
		}

		// inline function
		public Function(string name, string[] parameters, Token[] definition) : base(InternalType) {
			Name = name;
			FunctionType = FunctionTypeEnum.UserDefinedInline;
			Parameters = parameters;
			InlineDefinition = definition;
		}
		#endregion

		#region
		public Type TypeFor;
		public Function(string name, string[] parameters, Section script, Type typeFor) : base(InternalType) {
			Name = name;
			Parameters = parameters;
			Script = script;
			TypeFor = typeFor;
			FunctionType = FunctionTypeEnum.Constructor;
		}
		#endregion

		#region internal
		public delegate Data InternalFunctionDelegate(Data thisReference, List<Data> args);
		public InternalFunctionDelegate InternalFunction;
	
		public Function(string name, InternalFunctionDelegate internalFunction) : base(InternalType) {
			Name = name;
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