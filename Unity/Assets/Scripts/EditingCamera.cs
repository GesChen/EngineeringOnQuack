using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EditingCamera : MonoBehaviour
{
	
	public float orbitSensitivity;
	public float drift;

	public Vector3 target;
	private float pitch;
	private float yaw;
	private float dist;
	private Vector2 vel;

	InputMaster controls;

	// Start is called before the first frame update
	void Awake()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		controls = new InputMaster();
	}
	void OnEnable()
	{
		controls.Enable();
	}
	void OnDisable()
	{
		controls.Disable(); 
	}

	// Update is called once per frame
	void Update()
	{
		// orbit
		vel *= drift;
		if (controls.Camera.PerfOrbit.IsPressed())
			vel = controls.Camera.OrbitVel.ReadValue<Vector2>();
		pitch = pitch + vel.y % 360;
		yaw = yaw + vel.x % 360;


	}
}
