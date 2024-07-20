using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controls : MonoBehaviour
{
    public static float clickMaxDist = 5;
    public static float clickMaxTime = .1f;

	public static InputMaster inputMaster;

	void Awake()
	{
		inputMaster = new InputMaster();
	}

	public static Controls GetControls()
	{
		return FindObjectOfType<Controls>();
	}
}
