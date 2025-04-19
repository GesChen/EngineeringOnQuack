using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayingMode
{
	Building,
	Simulating
}

public class GameManager : MonoBehaviour
{
	#region singleton
	private static GameManager _instance;
	public static GameManager Instance { get { return _instance; } }
	void Awake() { UpdateSingleton(); }
	private void OnEnable() { UpdateSingleton(); }
	void UpdateSingleton()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(this);
		}
		else
		{
			_instance = this;
		}
	}
	#endregion

	public PlayingMode currentPlayMode = PlayingMode.Building;

	void Update()
	{
		Application.targetFrameRate = Config.FPS_LIMIT;
	}

	public void StartSimulating()
	{
		currentPlayMode = PlayingMode.Simulating;
	}

	public void StopSimulating()
	{
		currentPlayMode = PlayingMode.Building;
	}
}
