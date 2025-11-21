using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TMPAutoScaler : MonoBehaviour {
	public TextMeshProUGUI tmp;
	public Vector2 Padding;
	RectTransform rect;

	void Awake() {
		if (tmp == null)
			tmp = GetComponent<TextMeshProUGUI>();

		rect = GetComponent<RectTransform>();
	}

	void LateUpdate() {
		Vector2 size = tmp.GetPreferredValues(tmp.text);
		rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + Padding.x);
		rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y + Padding.y);
	}
}
