using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HoverTarget : MonoBehaviour {
	public bool Hovering { get; private set; }
	private bool m_Hovering = false;

	public bool AlreadyHasButton = false;

	public Config.UI.ColorBlock Colors;

	public delegate void HoverStateChangeEvent(bool state);
	public event HoverStateChangeEvent OnHoverStateChange;

	bool lastHovering = false;
	Image im;

	void Awake() {
		OnHoverStateChange = null; // reset, may get glitchy
	}

	void OnDisable() {
		if (!AlreadyHasButton)
			im.color = Colors.NormalColor;
	}

	void Start() {
		im = GetComponent<Image>();
		AlreadyHasButton = GetComponent<Button>() != null;

		if (!AlreadyHasButton)
			im.color = Colors.NormalColor;
	}

	void Update() {
		m_Hovering = UIHovers.CheckFirstIgnoringChildren(transform);
		Hovering = m_Hovering;

		if (m_Hovering != lastHovering) {
			OnHoverStateChange?.Invoke(m_Hovering);

			if (im != null && !AlreadyHasButton) {
				Color targetColor = m_Hovering ? Colors.HoverColor : Colors.NormalColor;
				StartCoroutine(LerpColor(im, targetColor, Colors.FadeDuration));
			}
		}

		lastHovering = m_Hovering;
	}

	IEnumerator LerpColor(Image image, Color targetColor, float duration) {
		Color startColor = image.color;
		float elapsed = 0f;

		while (elapsed < duration) {
			elapsed += Time.unscaledDeltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			image.color = Color.Lerp(startColor, targetColor, t);
			yield return null;
		}

		image.color = targetColor;
	}
}