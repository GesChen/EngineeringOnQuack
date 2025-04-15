using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// this probably should merge with controls
public class KeyboardTextInput : MonoBehaviour {

	public List<Key> presses;
	void Update() {
		UpdateKeyHeldTimes();
		presses = GetRepeats().Union(Controls.AllPressedThisFrame).ToList();
	}

	// these two can be optimized away into one timer sort of thing if you want
	Dictionary<Key, float> KeyHeldTimes = new();
	Dictionary<Key, float> KeyLastRepeatTime = new();
	void UpdateKeyHeldTimes() {
		foreach (Key k in Controls.AllPressedThisFrame) {
			KeyHeldTimes.Add(k, Time.time);
			KeyLastRepeatTime.Add(k, Time.time);
		}
		foreach (Key k in Controls.AllReleasedThisFrame) {
			KeyHeldTimes.Remove(k);
			KeyLastRepeatTime.Remove(k);
		}
	}

	List<Key> GetRepeats() {
		List<Key> keys = new();
		foreach (KeyValuePair<Key, float> keytime in KeyHeldTimes) {
			if (Time.time - keytime.Value > SEConfig.RepeatDelayMs / 1000 && // long enough held
				Time.time - KeyLastRepeatTime[keytime.Key] > 1f / SEConfig.RepeatRateCPS) { // long enough since last repeat
				keys.Add(keytime.Key);
				KeyLastRepeatTime[keytime.Key] = Time.time;
			}
		}

		return keys;
	}
}