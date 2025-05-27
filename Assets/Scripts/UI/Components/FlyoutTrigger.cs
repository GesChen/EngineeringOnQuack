using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlyoutTrigger : MonoBehaviour {
	public HoverTarget selfHoverTarget;
	public Flyout targetFlyout;

	public Image openIndicator;
	public Sprite openSprite;
	public Sprite closedSprite;

	// potentially null until it gets realised
	[HideInNormalInspector] public CWindow targetCWindow;
	[HideInInspector] public RectTransform rt;

	bool parentIsFlyout;
	Flyout parentFlyout;

	bool open;

	void Start() {
		rt = GetComponent<RectTransform>();

		selfHoverTarget.OnHoverStateChange += HoverStateChange;

		openIndicator.sprite = closedSprite;
	}

	void Update() {
		if (parentFlyout == null && Time.frameCount < Config.UI.Behaviour.MaxFramesForRealization) {
			parentFlyout = GetComponentInParent<Flyout>();
			parentIsFlyout = parentFlyout != null;
		}

		CheckRealization();

		open = targetFlyout.gameObject.activeSelf;
		if (openIndicator != null) {
			openIndicator.sprite = open ? openSprite : closedSprite;
		}
	}

	void CheckRealization() {
		if (targetFlyout != null) return;

		if (targetCWindow == null && targetFlyout == null) {
			Debug.LogError("Missing target CWindow or Flyout");
			return;
		}

		// try to retrieve the target flyout component or make it
		if (targetCWindow.RealisedWindow != null) {
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
				Debug.LogError("Target CWindow still not created!");
				return;
			}
		}
	}

	void HoverStateChange(bool state) {
		if (targetFlyout == null) {
			Debug.LogError("Flyout window not realised or isn't set");
			return;
		}

		if (state) {
			
			// hide all other flyouts of siblings
			foreach (Transform child in rt.parent) {
				if (child != transform) {
					if (child.TryGetComponent<FlyoutTrigger>(out var trigger)) {
						trigger.targetFlyout.Hide();
					}
				}
			}
			
			targetFlyout.Show(this);
		} else {

			if (!targetFlyout.mouseInRange) // and the mouse isnt hovering on the target flyout now
				targetFlyout.Hide();
		}

		if (parentIsFlyout) {
			parentFlyout.childFlyout = targetFlyout;
			parentFlyout.childOpen = open;
		}
	}
}