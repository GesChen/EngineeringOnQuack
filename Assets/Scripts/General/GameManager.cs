using System;
using UnityEngine;

public class GameManager : Singleton<GameManager> {
	protected override void Awake() {
		base.Awake();

		// reset events
		OnStartSimulating = null;
		OnStopSimulating = null;

		Config.Fonts.Reset();
	}

	void Start() {
		UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
		Application.targetFrameRate = Config.FPS_LIMIT;
	}

	public void StartSimulating() {
		OnStartSimulating?.Invoke();
	}

	public void StopSimulating() {
		OnStopSimulating?.Invoke();
	}

	// should hopefully call its awake and nulling before everything else
	public event Action OnStartSimulating; 
	public event Action OnStopSimulating;
}