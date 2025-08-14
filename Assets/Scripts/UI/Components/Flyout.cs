using Codice.Client.BaseCommands;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Flyout : MonoBehaviour {
	// really exists juist to exist
	// no serialized members either

	[HideInNormalInspector] public bool mouseInRange;
	[HideInNormalInspector] public Flyout openChildFlyout;
	FlyoutTrigger[] childTriggers = new FlyoutTrigger[0];
	RectTransform rt;
	FlyoutTrigger thisTrigger;
	LiveWindow lw;
	bool childOpen;
	
	bool startOverride = false;
	bool overrideProtection = false;

	int lastChildCount = 0;

	void Start() {
		rt = GetComponent<RectTransform>();
		lw = GetComponent<LiveWindow>();
		// this thing doesnt work for shit for some reason
		//LayoutRebuilder.ForceRebuildLayoutImmediate(rt); // hides instantly so must recalculate
		//if (TryGetComponent(out ScaleToContents scale))
			//scale.
		//gameObject.SetActive(false);

	}

	// only updates when visible (active)
	void Update() {

		// hacky workaround to give the ui elements time to load as active objects
		// cuz forcerebuildlayoutimmediate doesnt wanna work for shit for some reason
		// sorry future me
		if (Time.frameCount == Config.UI.Behaviour.MaxFramesForRealization)
			gameObject.SetActive(false);
		if (Time.frameCount <= Config.UI.Behaviour.MaxFramesForRealization) {
			transform.position = new Vector2(-1000, -1000); // somewhere offscreen to load
			return;
		}

		mouseInRange = CheckMouseValidity(Config.UI.Behaviour.FlyoutHoverMargin);

		if (mouseInRange)
			overrideProtection = false;
		if (!mouseInRange && !overrideProtection)
			startOverride = false;

		childOpen = openChildFlyout != null && openChildFlyout.gameObject.activeSelf;
		if (!(mouseInRange || childOpen || startOverride) 
			&& (thisTrigger == null || !thisTrigger.selfHoverTarget.Hovering)) {
			Hide();
		}

		int count = transform.childCount;
		if (count != lastChildCount) {
			GetChildTriggers();
		}
		lastChildCount = count;
	}

	void GetChildTriggers() {
		childTriggers = GetComponentsInChildren<FlyoutTrigger>();
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

	public void Show(FlyoutTrigger trigger, bool horizontal, bool prioritizeRight, bool prioritizeUp) {
		thisTrigger = trigger;

		transform.SetAsLastSibling(); // might change this idk

		lw.Show();
		lw.PlaceAt(trigger.rt, horizontal, prioritizeRight, prioritizeUp);
	}

	public void Show(Vector3 at, bool horizontal, bool prioritizeRight, bool prioritizeUp) {
		gameObject.SetActive(true);
		transform.SetAsLastSibling();

		lw.PlaceAt(at, horizontal, prioritizeRight, prioritizeUp);
	}

	public void Hide() {
		if (openChildFlyout != null)
			openChildFlyout.Hide();

		gameObject.SetActive(false);
	}

	public void HideAllChildFlyouts() {
		foreach(var trigger in childTriggers) {
			trigger.targetFlyout.Hide();
		}
	}
	public void HideAllChildFlyoutsExcept(Transform t) {
		foreach (var trigger in childTriggers) {
			if (trigger.transform != t)
				trigger.targetFlyout.Hide();
		}
	}

	public void OverrideStart() {
		startOverride = true;
		overrideProtection = true;
	}
}