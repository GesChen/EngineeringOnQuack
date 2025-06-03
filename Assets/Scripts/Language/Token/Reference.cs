using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token {
	public partial class T_Reference : Token {
		public bool Exists;
		public bool IsInstanceVariable;
		public bool IsListItem;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
		public string Name;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
		public T_Data ThisReference;
		public T_Data ParentReference;
		public int ListIndex;

		// most normal case, x.y where x is parent and y is data
		public T_Reference(
			bool exists, 
			bool isInstanceVariable, 
			bool isListItem, 
			string name,
			T_Data thisReference, 
			T_Data parentReference, 
			int listIndex) {

			Exists = exists;
			IsInstanceVariable = isInstanceVariable;
			IsListItem = isListItem;
			
			Name = name;

			ThisReference = thisReference;
			ParentReference = parentReference;
			ListIndex = listIndex;
		}

		public T_Reference Copy() {
			return new(
				Exists,
				IsInstanceVariable,
				IsListItem,
				Name,
				ThisReference,
				ParentReference,
				ListIndex);
		}

		public static T_Reference ExistingGlobalReference(string name, T_Data data)
			=> new(true, false, false, name, data, null, -1);

		public static T_Reference ExistingGlobalReference(T_Data data)
			=> new(true, false, false, "", data, null, -1);

		public static T_Reference NewGlobalReference(string name)
			=> new(false, false, false, name, null, null, -1);

		public static T_Reference ExistingMemberReference(T_Reference parent, T_Data data, string name)
			=> new(true, true, false, name, data, parent.ThisReference, -1);

		public static T_Reference NewMemberReference(T_Reference parent, string name)
			=> new(false, true, false, name, null, parent.ThisReference, -1);

		public static T_Reference ExistingListItemReference(T_Reference container, T_Data data, int index)
			=> new(true, false, true, "", data, container.ThisReference, index);

		public T_Data GetData() {
			if (!Exists)
				return Errors.UnknownName(Name);

			if (IsListItem) {
				if (ParentReference is not Primitive.List parentList)
					return Errors.CannotIndex(ParentReference.Type.Name);

				if (ListIndex < 0 || ListIndex >= parentList.Value.Count)
					return Errors.IndexOutOfRange(ListIndex);

				return parentList.Value[ListIndex];
			}
			return ThisReference;
		}

		public T_Data SetData(Memory memory, T_Data data) {
			if (IsListItem) {
				if (ParentReference is not Primitive.List parentList)
					return Errors.CannotIndex(ParentReference.Type.Name);

				if (ListIndex < 0 || ListIndex >= parentList.Value.Count)
					return Errors.IndexOutOfRange(ListIndex);

				parentList.Value[ListIndex] = data;
			}

			else if (IsInstanceVariable) {
				T_Data trySet = ParentReference.SetThisMember(Name, data);
				if (trySet is Error) return trySet;
			}

			else { // global variable
				T_Data trySet = memory.Set(Name, data); // set the name in the memory where the data is from, might help?
				if (trySet is Error) return trySet;
			}

			Exists = true;
			ThisReference = data; // re reference the new data object
			return ThisReference;
		}

		public override string ToString() {
			return $"#R to {ThisReference.Type.Name} {ThisReference}";
		}
	}
}