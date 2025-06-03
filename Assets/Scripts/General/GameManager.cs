using System;
using UnityEngine;

public enum PlayingMode {
	Building,
	Simulating
}

public class GameManager : Singleton<GameManager> {
	public PlayingMode currentPlayMode = PlayingMode.Building;
	protected override void Awake() {
		base.Awake();

		// reset events
		OnStartSimulating = null;
		OnStopSimulating = null;
		OnPlayModeChange = null;
	}
	void Start() {
		UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
	}

	void Update() {
		Application.targetFrameRate = Config.FPS_LIMIT;
	}

	public void StartSimulating() {
		currentPlayMode = PlayingMode.Simulating;

		OnStartSimulating?.Invoke();
		OnPlayModeChange?.Invoke(currentPlayMode);
	}

	public void StopSimulating() {
		currentPlayMode = PlayingMode.Building;

		OnStopSimulating?.Invoke();
		OnPlayModeChange?.Invoke(currentPlayMode);
	}

	public event Action OnStartSimulating;
	public event Action OnStopSimulating;

	public delegate void PlayModeChange(PlayingMode curMode);
	public event PlayModeChange OnPlayModeChange;
}