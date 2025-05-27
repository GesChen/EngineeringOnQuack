using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Flyout : MonoBehaviour {
	// really exists juist to exist
	// no serialized members either

	[HideInNormalInspector] public bool mouseInRange;
	[HideInNormalInspector] public Flyout childFlyout;
	[HideInNormalInspector] public bool childOpen;
	RectTransform rt;
	Canvas canvas;
	FlyoutTrigger thisTrigger;

	void Start() {
		rt = GetComponent<RectTransform>();
		LayoutRebuilder.ForceRebuildLayoutImmediate(rt); // hides instantly so must recalculate
		canvas = GetComponentInParent<Canvas>();

		gameObject.SetActive(false);
	}

	// only updates when visible (active)
	void Update() {
		mouseInRange = CheckMouseValidity(Config.UI.Behaviour.FlyoutHoverMargin);

		if (!mouseInRange && !childOpen && !thisTrigger.selfHoverTarget.Hovering) {
			Hide();
		}
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

	public void Show(FlyoutTrigger trigger) {
		thisTrigger = trigger;

		gameObject.SetActive(true);
		transform.SetAsLastSibling();

		Vector3[] corners = new Vector3[4];
		trigger.rt.GetWorldCorners(corners);

		// check if would fit at right
		Vector2 triggerTopRight = (Vector2)corners[2] +
				Config.UI.Behaviour.FlyoutDistance * Vector2.right;

		bool wouldFit = triggerTopRight.x + rt.rect.width < canvas.renderingDisplaySize.x;

		if (wouldFit) { // place top left at top right
			SetWorldCorner(rt, triggerTopRight, 1);
		} else {
			// place top right corner of dropdown at top left
			Vector2 triggerTopLeft = (Vector2)corners[1] +
				-Config.UI.Behaviour.FlyoutDistance * Vector2.right;

			SetWorldCorner(rt, triggerTopLeft, 2);
		}
	}

	// 0-BL 1-TL 2-TR 3-BR
	public void SetWorldCorner(RectTransform rect, Vector3 targetWorldPosition, int corner) {
		Vector3[] worldCorners = new Vector3[4];
		rect.GetWorldCorners(worldCorners);

		Vector3 currentCornerPos = worldCorners[corner];

		Vector3 offset = targetWorldPosition - currentCornerPos;

		rect.position += offset;
	}

	public void Hide() {
		if (childFlyout != null)
			childFlyout.Hide();

		gameObject.SetActive(false);
	}
}