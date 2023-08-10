using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
public class EditingCamera : MonoBehaviour
{
	[Header("Customization")]
	public float orbitSensitivity = .2f;
	public float zoomSensitivity = .001f;
	public float moveSensitivity;
	[Space]
	public float orbitDrift = .8f;
	public float zoomDrift = .1f;
	public float moveDrift = .2f;
	[Space]
	public float initDist;
	
	public Vector3 target;
	private float pitch;
	private float yaw;
	private float dist;
	float targetDist;
	private Vector2 vel;
	Vector2 lastvel;

	InputMaster controls;

	// Start is called before the first frame update
	void Awake()
	{
		//Cursor.lockState = CursorLockMode.Locked;
		//Cursor.visible = false;
		controls = new InputMaster();
		dist = initDist;
		targetDist = initDist;
	}
	void OnEnable()
	{
		if (controls == null)
			controls = new InputMaster();
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
		vel *= orbitDrift;
		if (controls.Camera.PerfOrbit.IsPressed())
		{
			if((v2Abs(lastvel) - v2Abs(vel)).sqrMagnitude > 0)
				vel = Vector2.Lerp(vel,controls.Camera.Mouse.ReadValue<Vector2>() * orbitSensitivity, 1 - orbitDrift);
			else
				vel = controls.Camera.Mouse.ReadValue<Vector2>() * orbitSensitivity;
			lastvel = vel;
		}
		
		// move 
		// todo: sensitivity changes with screen resolution. no good fix. bad fix for now. please fix future me. sorry.
		else if (controls.Camera.Move.IsPressed())
			target += transform.rotation * -controls.Camera.Mouse.ReadValue<Vector2>() * moveSensitivity * Mathf.Abs(dist) / Screen.width * 1920;
		//Debug.Log(vel.ToString() + ' ' + controls.Camera.PerfOrbit.IsPressed().ToString());

		pitch = (pitch - vel.y) % 360;
		yaw = (yaw + vel.x) % 360;

		targetDist += controls.Camera.Zoom.ReadValue<float>() * zoomSensitivity;
		dist = Mathf.Lerp(dist, targetDist, zoomDrift);

		Quaternion r = Quaternion.Euler(pitch, yaw, 0);

		transform.position = r * Vector3.forward * dist + target;																				;
		transform.rotation = r;
	}
	Vector2 v2Abs(Vector2 v)
	{
		return new(Mathf.Abs(v.x), Mathf.Abs(v.y));
	}
}
