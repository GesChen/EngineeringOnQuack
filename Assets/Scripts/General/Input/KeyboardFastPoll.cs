using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class KeyboardFastPoll : Singleton<KeyboardFastPoll> {

	public float samplingRate = 500;
	private Coroutine pollingCoroutine;
	private static readonly List<Key> pressedKeys = new();
	private static readonly object lockObject = new();

	void Start() {
		StartPolling();
	}

	void LateUpdate() {
		// Access the captured key list
		lock (lockObject) {
			if (pressedKeys.Count > 50)
				pressedKeys.Clear(); // not being sampled
			
			//print(pressedKeys.Count);
			// Clear keys at the end of the frame
		}

		// Ensure coroutine is still running
		if (pollingCoroutine == null) {
			Debug.LogWarning("Polling coroutine stopped. Restarting.");
			StartPolling();
		}
	}

	private void StartPolling() {
		pollingCoroutine = StartCoroutine(SampleKeys());
	}

	IEnumerator SampleKeys() {
		var kb = Keyboard.current;
		var interval = 1f / samplingRate;

		while (true) {
			try {
				if (kb == null) {
					Debug.LogWarning("Keyboard not found.");
					continue;
				}

				foreach (KeyControl kc in kb.allKeys) {
					if (kc.isPressed) {
						lock (lockObject) {
							pressedKeys.Add(kc.keyCode);
						}
					}
				}
			} catch (System.Exception ex) {
				Debug.LogError($"Key sampling error: {ex.Message}");
			}

			yield return new WaitForSecondsRealtime(interval);
		}
	}

	public static List<Key> GetAllPressedKeys() {
		var _ = Instance;

		lock (lockObject) {
			var copy = new List<Key>(pressedKeys);
			pressedKeys.Clear();
			return copy;
		}
	}
}
