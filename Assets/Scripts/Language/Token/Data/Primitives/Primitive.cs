public abstract partial class Primitive : Data
{
	protected Primitive(Type type) : base(type) {
	}

	public partial class Number		: Primitive { }
	public partial class String		: Primitive { }
	public partial class Bool		: Primitive { }
	public partial class List		: Primitive { }
	public partial class Dict		: Primitive { }
	public partial class Function	: Primitive { }
	public partial class Error		: Primitive { }
}
