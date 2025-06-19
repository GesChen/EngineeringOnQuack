using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HoverTarget))]
public class FlyoutTrigger : MonoBehaviour {
	public Flyout targetFlyout;
	public bool openHorizontally;
	public bool openPrioritizingTopRight;

	public Image openIndicator;
	public Sprite openSprite;
	public Sprite closedSprite;

	[HideInNormalInspector] public HoverTarget selfHoverTarget;

	// potentially null until it gets realised
	[HideInNormalInspector] public CWindow targetCWindow;
	[HideInNormalInspector] public RectTransform rt;

	bool parentIsFlyout;
	Flyout parentFlyout;

	bool open;

	void Start() {
		rt = GetComponent<RectTransform>();

		selfHoverTarget = GetComponent<HoverTarget>();
		selfHoverTarget.OnHoverStateChange += HoverStateChange;

		if (openIndicator != null)
			openIndicator.sprite = closedSprite;
	}

	void Update() {
		if (parentFlyout == null && Time.frameCount < Config.UI.Behaviour.MaxFramesForRealization) {
			parentFlyout = GetComponentInParent<Flyout>();
			parentIsFlyout = parentFlyout != null;
		}

		CheckRealization();
		if (targetFlyout == null) return; // give it time

		open = targetFlyout.gameObject.activeSelf;

		if (openIndicator != null)
			openIndicator.sprite = open ? openSprite : closedSprite;
	}

	void CheckRealization() {
		if (targetFlyout != null) return;

		if (targetCWindow == null) {
			Debug.LogError("No target CWindow assigned! " + transform.GetPath());
			return;
		}
		if (targetFlyout == null) {
			Debug.LogError("Target flyout not created/is null! " + transform.GetPath());
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
			targetFlyout.Show(this, openHorizontally, openPrioritizingTopRight);
		} else {

			if (!targetFlyout.mouseInRange) // and the mouse isnt hovering on the target flyout now
				targetFlyout.Hide();
		}

		if (parentIsFlyout) {
			parentFlyout.openChildFlyout = targetFlyout;
		}
	}
}