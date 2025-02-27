using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Token {
	public partial class Reference : Token {
		public bool Exists;
		public bool IsInstanceVariable;
		public bool IsListItem;

#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
		public string Name;
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
		public Data ThisReference;
		public Data ParentReference;
		public int ListIndex;

		// most normal case, x.y where x is parent and y is data
		public Reference(
			bool exists, 
			bool isInstanceVariable, 
			bool isListItem, 
			string name,
			Data thisReference, 
			Data parentReference, 
			int listIndex) {

			Exists = exists;
			IsInstanceVariable = isInstanceVariable;
			IsListItem = isListItem;
			
			Name = name;

			ThisReference = thisReference;
			ParentReference = parentReference;
			ListIndex = listIndex;
		}

		public static Reference ExistingGlobalReference(string name, Data data)
			=> new(true, false, false, name, data, null, -1);

		public static Reference NewGlobalReference(string name)
			=> new(false, false, false, name, null, null, -1);

		public static Reference ExistingMemberReference(Reference parent, Data data, string name)
			=> new(true, true, false, name, data, parent.ThisReference, -1);

		public static Reference NewMemberReference(Reference parent, string name)
			=> new(false, true, false, name, null, parent.ThisReference, -1);

		public static Reference ExistingListItemReference(Reference container, Data data, int index)
			=> new(true, false, true, "", data, container.ThisReference, index);

		public Data GetData() {
			if (!Exists)
				return Errors.UnknownVariable(Name);

			if (IsListItem) {
				if (ParentReference is not Primitive.List parentList)
					return Errors.CannotIndex(ParentReference.Type.Name);

				if (ListIndex < 0 || ListIndex >= parentList.Value.Count)
					return Errors.IndexOutOfRange(ListIndex);

				return parentList.Value[ListIndex];
			}
			return ThisReference;
		}

		public Data SetData(Data data) {
			if (IsListItem) {
				if (ParentReference is not Primitive.List parentList)
					return Errors.CannotIndex(ParentReference.Type.Name);

				if (ListIndex < 0 || ListIndex >= parentList.Value.Count)
					return Errors.IndexOutOfRange(ListIndex);

				parentList.Value[ListIndex] = data;
			}

			else if (IsInstanceVariable) {
				ParentReference.SetThisMember(Name, data);
			}

			else { // global variable
				data.Memory.Set(Name, data); // set the name in the memory where the data is from, might help?
			}

			ThisReference = data; // re reference the new data object
			return ThisReference;
		}

	}
}