using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

public class TransformData {
	public SVector3 position;
	public SVector4 rotation;
	public SVector3 localScale;

	public static explicit operator TransformData(Transform transform) => new() {
		position = transform.position,
		rotation = transform.rotation,
		localScale = transform.localScale
	};

	public Vector3 GetLocalScale() {
		return localScale;
	}

	public void ApplyToTransform(Transform transform) {
		transform.SetPositionAndRotation(position, rotation);
		transform.localScale = localScale;
	}
}