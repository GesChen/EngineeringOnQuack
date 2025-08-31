using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HoverTarget))]
public class FlyoutTrigger : MonoBehaviour {
	public Flyout targetFlyout;
	public int openTargetEdge;
	public bool openAlignment;

	public Image openIndicator;
	public Sprite openSprite;
	public Sprite closedSprite;

	DateTime create;

	[HideInNormalInspector] public HoverTarget selfHoverTarget;

	// potentially null until it gets realised
	[HideInNormalInspector] public CWindow targetCWindow;
	[HideInNormalInspector] public RectTransform rt;

	bool open;

	void Start() {
		rt = GetComponent<RectTransform>();

		selfHoverTarget = GetComponent<HoverTarget>();
		selfHoverTarget.OnHoverStateChange += HoverStateChange;

		if (openIndicator != null)
			openIndicator.sprite = closedSprite;

		create = DateTime.Now;

	}

	void Update() {
		CheckRealization();
		if (targetFlyout == null) return; // give it time

		open = targetFlyout.gameObject.activeSelf;

		if (openIndicator != null)
			openIndicator.sprite = open ? openSprite : closedSprite;
	}

	void CheckRealization() {
		if (targetFlyout != null) return;

		if (targetCWindow == null) {
			Debug.LogError("No target CWindow assigned! in " + transform.GetPath());
			return;
		}

		/*try {
			var _ = targetCWindow.RealisedWindow;
		} catch {
			Debug.Log($"fetch {create}");
		}*/

		// try to retrieve the target flyout component or make it
		if (targetCWindow.GetRealisedOrNull() != null) {
			var window = targetCWindow.RealisedWindow.gameObject;
			// other triggers may have already created a component
			if (window.TryGetComponent(out Flyout flyoutInstance)) {
				targetFlyout = flyoutInstance;
			} else {
				targetFlyout = window.AddComponent<Flyout>();
				// component needs no setup
			}
		} else { 
			if (Time.frameCount > Config.UI.Behaviour.MaxFramesForRealization) {
				Debug.LogError("Target CWindow still not realised!");
				return;
			}
		}
	}

	void HoverStateChange(bool state) {
		if (targetFlyout == null) {
			Debug.LogError("Flyout window not realised or isn't set in "+transform.GetPath());
			return;
		}

		if (state) {
			targetFlyout.sourceTrigger = this;
		
			targetFlyout.Show(this, openTargetEdge, openAlignment);
		} else {

			if (!targetFlyout.mouseInRange) // and the mouse isnt hovering on the target flyout now
				targetFlyout.Hide();
		}
	}
}