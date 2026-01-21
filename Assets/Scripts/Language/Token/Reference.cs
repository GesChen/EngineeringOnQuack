using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token {
	public partial class T_Reference : Token {
		public bool Exists = true;
		public string Name = "";

		public enum ReferenceType {
			Global,
			Instance,
			ListItems, // a[0,2,3] -> listitems ref
			DictItems // same as list but for a dict
		}
		public ReferenceType Type = ReferenceType.Global;

		// p_list when type is listitems or dictitems
		T_Data m_thisReference = null;
		public T_Data ThisReference {
			get {
				if (!Exists)
					return Errors.UnknownName(Name);

				if (Type == ReferenceType.DictItems) {
					// check for errors this is a terrible system 
					// im ngl
					// when getting the data all values must exist
					// fine when setting
					if (m_thisReference is not Primitive.List list)
						return Errors.BadCode();

					foreach (var item in list.Value) {
						if (item is Error)
							return item; // its the error right? so we can just return it
					}

					if (list.Value.Count == 1)
						return list.Value[0];
				}else 
				if (Type == ReferenceType.ListItems) {
					if (m_thisReference is not Primitive.List list)
						return Errors.BadCode();

					if (list.Value.Count == 1)
						return list.Value[0];
				}

				return m_thisReference;
			}
			set {
				m_thisReference = value;
			}
		}
		
		public T_Data ParentReference = null;

		// used for both list and dict, save some memory
		public T_Data[] KeyIndices = null;

		public T_Reference() { }

		public T_Reference Copy() => new() {
			Exists = Exists,
			Name = Name,
			Type = Type,
			m_thisReference = m_thisReference,
			ParentReference = ParentReference,
			KeyIndices = KeyIndices
		};

		public static T_Reference ExistingGlobalReference(T_Data data) => new() {
			Exists = true,
			Type = ReferenceType.Global,
			m_thisReference = data
		};

		public bool IsLiteral =>
			Name == ""
			&& (Type == ReferenceType.Global || Type == ReferenceType.Instance);

		public T_Data SetData(Memory globalMemory, T_Data data) {
			switch (Type) {
				case ReferenceType.Global: { // global variable
					T_Data trySet = globalMemory.Set(Name, data); // set the name in the memory where the data is from, might help?
					if (trySet is Error) return trySet;
					break;
				}

				case ReferenceType.Instance: {
					T_Data trySet = ParentReference.SetThisMember(Name, data);
					if (trySet is Error) return trySet;
					break;
				}

				case ReferenceType.ListItems: {
					if (ParentReference is not Primitive.List parentList)
						return Errors.CannotIndex(ParentReference.Type.Name);

					foreach (var idxData in KeyIndices) {
						if (idxData is not Primitive.Number num) 
							return Errors.BadCode();

						int index = (int)num.Value;

						// double check
						if (index < 0 || index >= parentList.Value.Count)
							return Errors.IndexOutOfRange(index);

						parentList.Value[index] = data;
					}

					break;
				}

				case ReferenceType.DictItems: {
					if (ParentReference is not Primitive.Dict parentDict)
						return Errors.CannotIndex(ParentReference.Type.Name);

					foreach (var key in KeyIndices) {
						Primitive.Dict.set(parentDict, new() {
							key,
							data
						});
					}
					break;
				}
			}

			Exists = true;
			m_thisReference = data; // re reference the new data object
			return m_thisReference;
		}

		public override string ToString() {
			return $"#R to {m_thisReference.Type.Name} {m_thisReference}";
		}
	}
}