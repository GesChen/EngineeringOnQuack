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
			yield return new WaitForSecondsRealtime(interval);

			if (kb == null) {
				Debug.LogWarning("Keyboard not found.");
				continue;
			}

			foreach (KeyControl kc in kb.allKeys) {
				// dont know why need the null check but kc is null sometimes
				if (kc != null && kc.isPressed) {
					lock (lockObject) {
						pressedKeys.Add(kc.keyCode);
					}
				}
			}
		}
	}

	// only let conatrols use it 
	// >1 call will break shit
	internal static List<Key> GetAllPressedKeys() {
		var _ = Instance;

		lock (lockObject) {
			//return pressedKeys;

			// not sure why it used to do this
			// it works better this way though?
			var copy = new List<Key>(pressedKeys);
			pressedKeys.Clear();
			return copy;
		}
	}
}
