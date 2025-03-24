using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Primitive;

public class Data : Token {
#pragma warning disable CS0108
	public string Name;
#pragma warning restore CS0108
	public Type Type;
	public Memory Memory;
	public Flags Flags = Flags.None;

	public static Memory currentUseMemory;

	// constructors
	public Data(string name, Type type, Memory memory, Flags flags) {
		Name				= name;
		Type				= type;
		Memory				= memory;
		Flags				= flags;
	}
	public Data(Type type) {
		Type				= type;
		Memory				= new(currentUseMemory?.InterpreterCC);
		Flags				= Flags.None;
	}
	public Data(Data original) { // copy constructor
		Name				= original.Name;
		Type				= original.Type;
		Memory				= original.Memory.Copy();
		Flags				= original.Flags;
	}

	// statics
	public static Data Success = new Bool(true);
	public static Data Fail = new Bool(false);

	#region methods
	public virtual Data Copy() {
		return new(this); // call copy constructor
	}
	public Data SetFlags(Flags flags) {
		Flags = flags;
		return this;
	}
	public Data ClearFlags() {
		Flags = Flags.None;
		return this;
	}
	public Data CopyWithFlags(Flags flags) {
		return new Data(this).SetFlags(flags);
	}

	public virtual Data GetMember(string name) {
		// instance variables with same name as methods override same name in memory
		Data get = Memory.Get(name);
		if (get is not Error)
			return get;

		return Type.Snapshot.Get(name);
	}

	public Data SetThisMember(string name, Data data) {
		return SetMember(this, name, data);
	}

	public static Data SetMember(Data thisReference, string name, Data data) {
		if (thisReference is Primitive)
			return Errors.CannotSetMemberOfBuiltin(name);
		
		thisReference.Memory.Set(name, data);
		return data;
	}

	#region Casting
		// self cast
	public Data Cast(Type toType) {
		return CastFromTo(this, toType);
	}

	// cast any two types
	public static Data CastFromTo(Data fromValue, Type toType) {
		string FTN = fromValue.Type.Name;	// FromTypeName
		string TTN = toType.Name;			// ToTypeName

		char FTNC = FTN[0];	// FromTypeNameChar(0)
		char TTNC = TTN[0]; // ToTypeNameChar(0)

		if (FTNC == TTNC) // no casting needed!
			return fromValue;

		// have to be primitives, no cast (from or to function) or (from dict)
		if (!Primitive.TypeNames.Contains(FTN) || !Primitive.TypeNames.Contains(TTN))
			return Errors.InvalidCast(FTN, TTN);
		if (FTNC == 'F' || TTNC == 'D' || TTNC == 'F')
			return Errors.InvalidCast(FTN, TTN);

		return FTNC switch {
			'N' => NumberCast	(fromValue as Number,	TTNC, TTN),
			'S' => StringCast	(fromValue as String,	TTNC, TTN),
			'B' => BoolCast		(fromValue as Bool,		TTNC, TTN),
			'L' => ListCast		(fromValue as List,		TTNC, TTN),
			'D' => DictCast		(fromValue as Dict,		TTNC, TTN),
			_ => Errors.InvalidCast(FTN, TTN),
		};
	}

	private static Data NumberCast(Number value, char toc, string to) {
		double v = value.Value;
		switch (toc) {
			case 'S' : return Number.tostring(value, new());
			case 'B' : return new Bool(v != 0);
			case 'L' : return new List(new List<Data>() { value });
		}
		return Errors.InvalidCast("Number", to);
	}
	private static Data StringCast(String value, char toc, string to) {
		string v = value.Value;
		switch (toc) {
			case 'N':
				if (double.TryParse(v, out double val)) return new Number(val);
				return Errors.CannotParseValueAs("String", "Number");
			case 'B': return new Bool(v != "");
			case 'L': return new List(new List<Data>() { value });
		}
		return Errors.InvalidCast("String", to);
	}
	private static Data BoolCast(Bool value, char toc, string to) {
		bool v = value.Value;
		switch (toc) {
			case 'N': return new Number(v ? 1 : 0);
			case 'S': return Bool.tostring(value, new());
			case 'L': return new List(new List<Data>() { value });
		}
		return Errors.InvalidCast("Bool", to);
	}
	private static Data ListCast(List value, char toc, string to) {
		List<Data> v = value.Value;
		switch (toc) {
			case 'N': return new Number(v.Count == 0 ? 1 : 0);
			case 'S': return List.tostring(value, new());
			case 'B': return new Bool(v.Count != 0);
			case 'D': return List.todict(value, new());
		}
		return Errors.InvalidCast("List", to);
	}
	private static Data DictCast(Dict value, char toc, string to) {
		Dictionary<Data, Data> v = value.Value;
		switch (toc) {
			case 'N': return new Number(v.Count == 0 ? 1 : 0);
			case 'S': return Dict.tostring(value, new());
			case 'B': return new Bool(v.Count != 0);
			case 'L': return Dict.tolist(value, new());
		}
		return Errors.InvalidCast("Dict", to);
	}
	#endregion

	public override string ToString() {
		return $"[DATA DEFAULT TS] Type {Type.Name} object \"{Name}\"";
	}

	#endregion
}
