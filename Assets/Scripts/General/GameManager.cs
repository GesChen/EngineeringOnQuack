using UnityEngine;

public enum PlayingMode {
	Building,
	Simulating
}

public class GameManager : Singleton<GameManager> {
	public PlayingMode currentPlayMode = PlayingMode.Building;

	void Start() {
		UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
	}

	void Update() {
		Application.targetFrameRate = Config.FPS_LIMIT;
	}

	public void StartSimulating() {
		currentPlayMode = PlayingMode.Simulating;
	}

	public void StopSimulating() {
		currentPlayMode = PlayingMode.Building;
	}
}