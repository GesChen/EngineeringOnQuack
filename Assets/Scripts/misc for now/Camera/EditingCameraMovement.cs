using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditingCameraMovement : MonoBehaviour
{
	[Header("Customization")]
	public float orbitSensitivity	= .4f;
	public float zoomSensitivity	= .001f;
	public float moveSensitivity	= .00015f;
	public float keyboardmoveSpeed	= .1f;
	public float precisionCoef		= .2f;
	[Space]
	public float orbitDrift			= .8f;
	public float zoomDrift			= .1f;
	public float moveDrift			= .8f;
	float moveSmoothness;
	[Space]
	public float initDist			= -5f;
	
	[Header("Focusing")]
	public Vector3 focus;
	Vector3 target;
	Transform targetTransform;
	bool focusing;
	float focusThreshold = .001f;
	public float focusingDrift = .2f;

	private float pitch;
	private float yaw;
	[HideInInspector] public float dist;
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
		controls.Camera.Focus.performed += Focus;
		moveSmoothness = moveDrift;
	}
	void OnEnable()
	{
		controls ??= new InputMaster();
		controls.Enable();
	}
	void OnDisable()
	{
		controls.Disable(); 
	}

	// Update is called once per frame
	float globalSensitivity = 1.0f;
	void Update()
	{
		globalSensitivity = controls.Camera.Precision.IsPressed() ? precisionCoef : 1;
		
		// orbit
		vel *= orbitDrift;
		if (controls.Camera.PerfOrbit.IsPressed())
		{
			Orbit();
			Movement();
		}

		// move todo: sensitivity changes with screen resolution. no good fix. bad fix for now. please fix future me. sorry.
		else if (controls.Camera.Move.IsPressed())
			target += transform.rotation * -controls.Camera.Mouse.ReadValue<Vector2>() * moveSensitivity * Mathf.Abs(dist) * globalSensitivity; // / Screen.width * 1920;

		pitch = (pitch - vel.y) % 360;
		yaw = (yaw + vel.x) % 360;

		Zoom();

		if(focusing && (target - focus).sqrMagnitude < focusThreshold)
		{
			focusing = false;
			moveSmoothness = moveDrift;
		}

		focus = Vector3.Lerp(focus, target, moveSmoothness);// Vector3.SmoothDamp(focus, target, ref smoothTargetVel, focusTime);
		Quaternion r = Quaternion.Euler(pitch, yaw, 0);
		transform.position = r * Vector3.forward * dist + focus;
		transform.rotation = r;
	}
	void Orbit()
	{
		if (sumAxes(v2Abs(lastvel) - v2Abs(vel)) > 0)
			vel = Vector2.Lerp(vel, globalSensitivity * orbitSensitivity * controls.Camera.Mouse.ReadValue<Vector2>(), 1 - orbitDrift);
		else
			vel = globalSensitivity * orbitSensitivity * controls.Camera.Mouse.ReadValue<Vector2>();
		lastvel = vel;
	}
	void Movement()
	{
		Vector3 movement = controls.Camera.KeyboardMovement.ReadValue<Vector3>();
		Vector3 globalMove = transform.rotation * movement;
		target += globalMove * keyboardmoveSpeed;
	}
	void Zoom()
	{
		targetDist += controls.Camera.Zoom.ReadValue<float>() * zoomSensitivity * globalSensitivity;
		dist = Mathf.Lerp(dist, targetDist, zoomDrift);
	}
	void Focus(InputAction.CallbackContext context)
	{
		if (Physics.Raycast(Camera.main.ScreenPointToRay(controls.Camera.MousePos.ReadValue<Vector2>()), out RaycastHit hit)) { 
			if (hit.transform.GetComponent<Part>())
			{
				targetTransform = hit.transform;
				target = targetTransform.position;
				focusing = true;
				moveSmoothness = focusingDrift;
			}
		}
		else
		{
			targetTransform = null;
		}
		
	}
	Vector2 v2Abs(Vector2 v)
	{
		return new(Mathf.Abs(v.x), Mathf.Abs(v.y));
	}
	float sumAxes(Vector3 v)
	{
		return v.x + v.y + v.z;
	}
}