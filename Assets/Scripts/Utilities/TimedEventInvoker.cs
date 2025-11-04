using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Reflection;

public class TimedEventInvoker : MonoBehaviour {
	public struct TimedEvent {
		public Timing Timing;
		public TimedEventCall Action;

		public TimedEvent(Timing timing, TimedEventCall action) {
			Timing = timing;
			Action = action;
		}
	}

	private List<TimedEvent> m_customEvents;
	public List<TimedEvent> CustomEvents {
		get {
			if (m_customEvents == null || m_customEvents.Count == 0) {
				throw new("CustomEvents not assigned and trying to access!");
			}
			return m_customEvents;
		}
		set {
			m_customEvents = value;
			CustomAwake();
		}
	}

	public delegate void TimedEventCall(TimedEventInvoker source);
	public enum Timing {
		Awake,
		Start,
		Update,
		Close
	}

	void CallEvents(Timing timing) {
		if (CustomEvents == null || CustomEvents.Count == 0) return;

		var timedAction = CustomEvents?.Where(ce => ce.Timing == timing).Select(ce => ce.Action);

		foreach (var a in timedAction) {
			a?.Invoke(this);
		}
	}

	// must be called once the script is made and customevents is assigned
	// normal awake would be called before customevents are assigned
	// for now i'm just binding it to the setter of customevents might break 
	// shit tho idk
	void CustomAwake() { CallEvents(Timing.Awake); }

	void Start() { CallEvents(Timing.Start); }
	void Update() { CallEvents(Timing.Update); }
	public void Close() { CallEvents(Timing.Close); }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TimedEventInvoker))]
public class TimedEventInvokerEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();

		var invoker = (TimedEventInvoker)target;
		var field = typeof(TimedEventInvoker).GetField("m_customEvents", BindingFlags.NonPublic | BindingFlags.Instance);

		if (field?.GetValue(invoker) is not IEnumerable events) {
			EditorGUILayout.HelpBox("No CustomEvents assigned.", MessageType.Info);
			return;
		}

		EditorGUILayout.LabelField("Serialized Custom Events Debug", EditorStyles.boldLabel);

		foreach (var e in events) {
			var timingField = e.GetType().GetField("Timing");
			var actionField = e.GetType().GetField("Action");

			var timing = timingField?.GetValue(e);
			var action = actionField?.GetValue(e) as System.Delegate;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(timing != null ? timing.ToString() : "Unknown Timing", GUILayout.Width(80));

			if (action != null) {
				var target = action.Target;
				if (target != null) {
					EditorGUILayout.LabelField($"Target: {target}", GUILayout.ExpandWidth(true));
				} else {
					EditorGUILayout.LabelField($"Method: {action.Method.Name}", GUILayout.ExpandWidth(true));
				}
			} else {
				EditorGUILayout.LabelField("Null Action", GUILayout.ExpandWidth(true));
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}
#endif