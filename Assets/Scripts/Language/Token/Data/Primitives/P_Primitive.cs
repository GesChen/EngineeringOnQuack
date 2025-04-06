using System.Collections.Generic;

public abstract partial class Primitive : Data {
	protected Primitive(Type type) : base(type) { }
	protected Primitive(Primitive original) : base(original) { }

	public static List<string> TypeNames = new() {
		"Number",
		"String",
		"Bool",
		"List",
		"Dict",
		"Function"
	};

	public partial class Number		: Primitive { }
	public partial class String		: Primitive { }
	public partial class Bool		: Primitive { }
	public partial class List		: Primitive { }
	public partial class Dict		: Primitive { }
	public partial class Function	: Primitive { }
}