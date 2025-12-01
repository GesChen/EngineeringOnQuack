using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Construct {
	public class SVector3 {
		public float x, y, z;
		public SVector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
		public static implicit operator Vector3(SVector3 other) =>
			new(other.x, other.y, other.z);
		public static implicit operator SVector3(Vector3 other) =>
			new(other.x, other.y, other.z);
		public static implicit operator Color(SVector3 other) =>
			new(other.x, other.y, other.z);
		public static implicit operator SVector3(Color other) =>
			new(other.r, other.g, other.b);
		public override bool Equals(object obj) {
			if (ReferenceEquals(this, obj)) return true;
			if (obj is not SVector3 v) return false;
			return v.x == x && v.y == y && v.z == z;
		}
		public override int GetHashCode() => HashCode.Combine(x, y, z);
	}
	public class SVector4 {
		public float x, y, z, w;
		public SVector4(float X, float Y, float Z, float W) { x = X; y = Y; z = Z; w = W; }
		public static implicit operator Quaternion(SVector4 other) =>
			new(other.x, other.y, other.z, other.w);
		public static implicit operator SVector4(Quaternion other) =>
			new(other.x, other.y, other.z, other.w);

		public override bool Equals(object obj) {
			if (ReferenceEquals(this, obj)) return true;
			if (obj is not SVector4 v) return false;
			return v.x == x && v.y == y && v.z == z && v.w == w;
		}
		public override int GetHashCode() => HashCode.Combine(x, y, z, w);
	}
	public class Part {
		public int basePartID;
		public int id;
		public SVector3 position;
		public SVector4 rotation;
		public SVector3 scale;

		public SVector3 color;
		public int compositionID;

		public void CopyMembers(Part other) {
			basePartID = other.basePartID;
			id = other.id;
			position = other.position;
			rotation = other.rotation;
			scale = other.scale;

			color = other.color;
			compositionID = other.compositionID;
		}

		public BasePart GetBasePart() =>
			AllParts.BaseParts[basePartID];

		/// <summary>
		/// <para>For Assembling</para>
		/// Copy over ALL fields to the instantiated object
		/// </summary>
		public virtual void FinalizeInstantiation(GameObject instantiatedPart) { }

		public Vector3 TransformPoint(Vector3 p) {
			Vector3 sp = new(p.x * scale.x, p.y * scale.y, p.z * scale.z);
			Vector3 rp = (Quaternion)rotation * sp;
			return rp + position;
		}

		public void TransformPoints(Vector3[] points) {
			Matrix4x4 m = Matrix4x4.TRS(position, rotation, scale);
			for (int i = 0; i < points.Length; i++)
				points[i] = m.MultiplyPoint3x4(points[i]);
		}
	}
	public class Group {
		public List<int> PartIDs;

		public static explicit operator Group(PartGroup other) => new() {
			PartIDs = other.Parts.Select(p => p.ID).ToList(),
		};
	}

	public string Name;
	public List<Part> Parts;
	public List<Group> Groups;
	public BuildingClipboard Clipboard; // should serialize just fine
	public List<string> Outputs;
}