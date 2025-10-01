using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Flyout : MonoBehaviour {
	// really exists juist to exist
	// no serialized members either
	[HideInNormalInspector] public bool mouseInRange;
	[HideInNormalInspector] public FlyoutTrigger sourceTrigger;
	RectTransform rt;
	LiveWindow lw;
	
	CWindow[] childFlyouts;
	FlyoutTrigger[] childTriggers;

	void Start() {
		rt = GetComponent<RectTransform>();
		lw = GetComponent<LiveWindow>();
		// this thing doesnt work for shit for some reason
		//LayoutRebuilder.ForceRebuildLayoutImmediate(rt); // hides instantly so must recalculate
		//if (TryGetComponent(out ScaleToContents scale))
			//scale.
		//gameObject.SetActive(false);

		FindChildren();
	}

	// only updates when visible (active)
	void Update() {

		// hacky workaround to give the ui elements time to load as active objects
		// cuz forcerebuildlayoutimmediate doesnt wanna work for shit for some reason
		// sorry future me
		if (Time.frameCount - lw.Source.CreationFrame == Config.UI.Behaviour.MaxFramesForRealization)
			gameObject.SetActive(false);
		if (Time.frameCount - lw.Source.CreationFrame <= Config.UI.Behaviour.MaxFramesForRealization) {
			//transform.position = new Vector2(-1000, -1000); // somewhere offscreen to load
			return;
		}


		mouseInRange = CheckMouseValidity(Config.UI.Behaviour.FlyoutHoverMargin);
		bool anyChildOpen = childFlyouts.Any(f => f.RealisedWindow.gameObject.activeInHierarchy);
		bool triggerOpening = 
			sourceTrigger != null 
			&& sourceTrigger.selfHoverTarget.Hovering;

		if (!(mouseInRange 
			|| anyChildOpen 
			|| triggerOpening))
			Hide();
	}

	void FindChildren() {
		childTriggers = GetComponentsInChildren<FlyoutTrigger>();
		childFlyouts = childTriggers.Select(t => t.targetCWindow).ToArray();
	}

	public bool CheckMouseValidity(float margin) {
		Vector2 mousePos = Conatrols.Mouse.Position;

		Vector3[] corners = new Vector3[4];
		rt.GetWorldCorners(corners);
		Vector2 min = corners[0];
		Vector2 max = corners[2];

		// expanding by margin achieves same effect
		min -= margin * Vector2.one;
		max += margin * Vector2.one;

		return mousePos.x < max.x && mousePos.y < max.y && mousePos.x > min.x && mousePos.y > min.y;
	}

	public void Show(FlyoutTrigger trigger, int targetEdge, bool alignment) {
		transform.SetAsLastSibling(); // might change this idk

		lw.Show();
		lw.PlaceAt(trigger.rt, targetEdge, alignment);
	}

	public void Show(Vector3 at, int targetEdge, bool alignment) {
		gameObject.SetActive(true);
		transform.SetAsLastSibling();

		lw.PlaceAt(at, targetEdge, alignment);
	}

	public void Hide() {
		foreach (var child in childTriggers)
			child.targetFlyout.Hide();

		gameObject.SetActive(false);
	}
}