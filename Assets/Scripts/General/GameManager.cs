using System;
using UnityEngine;

public class GameManager : Singleton<GameManager> {
	public enum PlayingMode {
		Building,
		Simulating
	}

	public PlayingMode currentPlayMode = PlayingMode.Building;
	protected override void Awake() {
		base.Awake();

		// reset events
		OnStartSimulating = null;
		OnStopSimulating = null;
		OnPlayModeChanged = null;

		Config.Fonts.Reset();
	}
	void Start() {
		UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
		Application.targetFrameRate = Config.FPS_LIMIT;
	}

	void Update() {
	}

	public void StartSimulating() {
		currentPlayMode = PlayingMode.Simulating;

		OnStartSimulating?.Invoke();
		OnPlayModeChanged?.Invoke(currentPlayMode);
	}

	public void StopSimulating() {
		currentPlayMode = PlayingMode.Building;

		OnStopSimulating?.Invoke();
		OnPlayModeChanged?.Invoke(currentPlayMode);
	}

	public event Action OnStartSimulating;
	public event Action OnStopSimulating;

	public delegate void PlayModeChange(PlayingMode curMode);
	public event PlayModeChange OnPlayModeChanged;
}