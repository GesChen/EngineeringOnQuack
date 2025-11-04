using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Primitive;

public class T_Data : Token {
	public string Name;
	public Type Type;
	public Memory Memory; // instance data
	public Flags Flags = Flags.None;

	public static Memory currentUseMemory;

	// constructors
	public T_Data(string name, Type type, Memory memory, Flags flags) {
		Name				= name;
		Type				= type;
		Memory				= memory;
		Flags				= flags;
	}
	public T_Data(Type type) {
		Type				= type;
		Memory				= new(currentUseMemory?.Interpreter, "data's memory");
		Flags				= Flags.None;
	}
	public T_Data(T_Data original) { // copy constructor
		Name				= original.Name;
		Type				= original.Type;
		Memory				= original.Memory.Copy();
		Flags				= original.Flags;
	}

	// statics
	public static T_Data Success = new Bool(true);
	public static T_Data Fail = new Bool(false);

	#region methods
	public virtual T_Data Copy() {
		return new(this); // call copy constructor
	}
	public T_Data SetFlags(Flags flags) {
		Flags = flags;
		return this;
	}
	public T_Data ClearFlags() {
		Flags = Flags.None;
		return this;
	}
	public T_Data CopyWithFlags(Flags flags) {
		return Copy().SetFlags(flags);
	}

	public virtual T_Data GetMember(string name) {
		// instance variables with same name as methods override same name in memory
		T_Data get = Memory.Get(name, true);
		if (get is not Error)
			return get;

		return Type.Snapshot.Get(name, true);
	}

	public T_Data SetThisMember(string name, T_Data data) {
		return SetMember(this, name, data);
	}

	public static T_Data SetMember(T_Data thisReference, string name, T_Data data) {
		if (thisReference is Primitive)
			return Errors.CannotSetMemberOfBuiltin(name);
		
		thisReference.Memory.Set(name, data, true);
		return thisReference;
	}

	#region Casting
		// self cast
	public T_Data Cast(Type toType) {
		return CastFromTo(this, toType);
	}

	// cast any two types
	public static T_Data CastFromTo(T_Data fromValue, Type toType) {
		string FTN = fromValue.Type.Name;	// FromTypeName
		string TTN = toType.Name;			// ToTypeName

		char FTNC = FTN[0];	// FromTypeNameChar(0)
		char TTNC = TTN[0]; // ToTypeNameChar(0)

		if (FTNC == TTNC) // no casting needed!
			return fromValue;

		// cannot cast to non primitive
		if (!Primitive.TypeNamesHS.Contains(TTN))
			return Errors.InvalidCast(FTN, TTN);

		// no cast (from or to function) or (from dict)
		if (FTNC == 'F' || TTNC == 'D' || TTNC == 'F')
			return Errors.InvalidCast(FTN, TTN);

		// otherwise we chill
		// just find the custom function or
		var castFuncName = TTNC switch {
			'N' => "num",
			'S' => "str",
			'B' => "bool",
			'L' => "list",
			'D' => "dict",
			_ => "wtf"
		};
		if (castFuncName == "wtf") return Errors.BadCode();

		var tryGetCastFunc = fromValue.GetMember(castFuncName);
		if (tryGetCastFunc is Error) {
			// some primitives just cant cast to others
			if (Primitive.TypeNamesHS.Contains(FTN))
				return Errors.InvalidCast(FTN, TTN);

			// tostring global override
			if (TTNC == 'S') {
				// taking a page from python again
				return new String($"<{FTN} object>"); // wo mnemory address
			}

			// user defined, give reason
			return Errors.InvalidCast(FTN, TTN, $"Type {fromValue.Type.Name} does not contain the required method \"{castFuncName}\"");
		}
		if (tryGetCastFunc is not Primitive.Function castfunc)
			return Errors.InvalidCast(FTN, TTN, $"Member \"{castFuncName}\" from type {fromValue.Type.Name} must be a method to cast");

		var tryCast =
			fromValue.Memory.Interpreter.RunFunction(
				fromValue.Memory,
				castfunc,
				fromValue,
				new()
			);
		if (tryCast is Error) return tryCast;

		// if the function doesnt return the desired type then error
		if (tryCast.Type != toType)
			return Errors.CastMethodWrongType(
				fromValue.Type.Name,
				castFuncName,
				tryCast.Type.Name,
				toType.Name);

		return tryCast;
	}

	#endregion

	public override string ToString() {
		return $"Object \"{Name}\" of type {Type.Name}";
	}

	#endregion
}