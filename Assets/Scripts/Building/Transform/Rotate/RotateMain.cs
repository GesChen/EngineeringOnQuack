using UnityEngine;

public class RotateMain : MonoBehaviour
{
	public TransformTools main;
	public RotateAxis X;
	public RotateAxis Y;
	public RotateAxis Z;
	public RotateView View;
	void Update()
	{
		X.enabled = main.rotating;
		Y.enabled = main.rotating;
		Z.enabled = main.rotating;
		View.enabled = main.rotating;

		transform.localScale = main.rotating ? Vector3.one : Vector3.zero;
	}
}
