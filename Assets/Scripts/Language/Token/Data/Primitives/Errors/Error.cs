using System.Collections.Generic;

public partial class Error : Primitive {
	public static Error Default = new("");

	// defines internal type with name and memory
	public static Type InternalType = new("Error", new Dictionary<string, T_Data>() {
	});

	public string Value; // internal value

	public Error(string value) : base(InternalType) { // default constructor
		Value = value;
	}

	public override string ToString() {
		return $"Error: {Value}";
	}

	// methods
}