using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Caret : MonoBehaviour {
	public Vector2 head;
	public Vector2 tail;

	static float DefaultRepeatDelayMs = 1000;
	static float DefaultRepeatRateCPS = 31;
	static float DefaultCursorBlinkRateMs = 530;

	public void SetText(List<string> lines) {

	}

	void Update() {
		UpdateKeyHeldTimes();
		List<Key> presses = GetRepeats().Union(Controls.AllPressedThisFrame).ToList();
		

		Debug.Log(string.Join(", ", presses));
	}

	// these two can be optimized away into one timer sort of thing if you want
	Dictionary<Key, float> KeyHeldTimes = new();
	Dictionary<Key, float> KeyLastRepeatTime = new();
	void UpdateKeyHeldTimes() {
		foreach (Key k in Controls.AllPressedThisFrame) {
			KeyHeldTimes.Add(k, Time.time);
			KeyLastRepeatTime.Add(k, Time.time);
		}
		foreach(Key k in Controls.AllReleasedThisFrame) {
			KeyHeldTimes.Remove(k);
			KeyLastRepeatTime.Remove(k);
		}
	}

	List<Key> GetRepeats() {
		List<Key> keys = new();
		foreach(KeyValuePair<Key, float> keytime in KeyHeldTimes) {
			if (Time.time - keytime.Value > DefaultRepeatDelayMs / 1000 && // long enough held
				Time.time - KeyLastRepeatTime[keytime.Key] > 1f / DefaultRepeatRateCPS) { // long enough since last repeat
				keys.Add(keytime.Key);
				KeyLastRepeatTime[keytime.Key] = Time.time;
			}
		}

		return keys;
	}

	void HandleModifiers(List<Key> pressedKeys) {
		if (pressedKeys.Contains(Key.LeftCtrl) || pressedKeys.Contains(Key.RightCtrl)) {

		}
		if (pressedKeys.Contains(Key.LeftAlt) || )
	}
}
