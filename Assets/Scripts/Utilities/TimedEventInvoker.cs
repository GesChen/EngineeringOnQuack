using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimedEventInvoker : MonoBehaviour {
	public struct TimedEvent {
		public TimedEventCall Action;
		public Timing Timing;
		public TimedEvent(TimedEventCall action, Timing timing) {
			Action = action;
			Timing = timing;
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
		Update
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
}