using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// TODO: figure out why it flickers when moving between different options sometimes
public class Description : MonoBehaviour {
	public string Text;
	[HideInNormalInspector] public bool DescriptionShowing = false;
	float mouseOverTime;
	bool lastMouseOver;
	bool lastShowing;
	
	static (RectTransform rt, TextMeshProUGUI text) descriptionInstance;
	static bool descriptionInUse;
	static float closeTime;

	bool userOfDescription;
	bool attemptToBecomeUser;

	void OnDisable() {
		Close();
	}

	void Update() {
		if (descriptionInUse && !userOfDescription) return; // save some resources

		bool over;
		if (descriptionInstance.rt != null)
			over = UIHovers.CheckFirstAllowing(transform, descriptionInstance.rt) || 
				UIHovers.CheckFirstIgnoringChildren(transform);
		else over = UIHovers.CheckFirstIgnoringChildren(transform);

		if (over != lastMouseOver && over) {
			mouseOverTime = Time.time;
		}

		bool hoveredLongEnough = Time.time - mouseOverTime > Config.UI.Behaviour.DescriptionHoverMs / 1000f;
		bool inBetweenTime = Time.time - closeTime < Config.UI.Behaviour.TimeForDescriptionChangeMs / 1000f;
		DescriptionShowing = over && (hoveredLongEnough || inBetweenTime);

		// handle multiple users
		if (attemptToBecomeUser) AttemptUser();
		attemptToBecomeUser = false;
		if (DescriptionShowing != lastShowing) {

			if (DescriptionShowing)
				AttemptUser();
			else
				Close();
		}

		// actually set position
		if (DescriptionShowing && userOfDescription) {
			descriptionInstance.rt.position = Conatrols.Mouse.Position + Config.UI.Description.CursorOffset;
		}

		lastMouseOver = over;
		lastShowing = DescriptionShowing;
	}

	void AttemptUser() {
		// already user
		if (userOfDescription) return;

		if (!descriptionInUse) {
			descriptionInUse = true;
			userOfDescription = true;

			OpenAsUser();
		} else {
			attemptToBecomeUser = true;
		}
	}

	void OpenAsUser() {
		if (descriptionInstance.rt == null) 
			Generate();

		descriptionInstance.text.text = Text;
		descriptionInstance.rt.gameObject.SetActive(true);
		descriptionInstance.rt.SetAsLastSibling();
		LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionInstance.rt); // plz work bru
	}

	void Close() {
		if (userOfDescription)
			CloseAsUser();
	}

	void CloseAsUser() {
		descriptionInUse = false;
		userOfDescription = false;
		
		descriptionInstance.rt.gameObject.SetActive(false);
		closeTime = Time.time;
	}

	void Generate() {
		var newObj = new GameObject("Description");
		newObj.SetActive(false);

		var rect = newObj.AddComponent<RectTransform>();
		var parentCanvas = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
		rect.SetParent(parentCanvas);
		rect.pivot = new(0, 1);

		var image = newObj.AddComponent<Image>();
		image.color = Config.UI.Description.Color;

		var fitter = newObj.AddComponent<ContentSizeFitter>();
		fitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
		fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

		var layout = newObj.AddComponent<VerticalLayoutGroup>();
		int p = Config.UI.Description.Padding;
		layout.padding = new(p, p, p, p);
		layout.childControlWidth		= false;
		layout.childControlHeight		= false;
		layout.childScaleWidth			= false;
		layout.childScaleHeight			= false;
		layout.childForceExpandWidth	= false;
		layout.childScaleHeight			= false;

		var newTextObj = new GameObject("Text");
		
		var newTextRT = newTextObj.AddComponent<RectTransform>();
		newTextRT.SetParent(rect);

		var newText = newTextObj.AddComponent<TextMeshProUGUI>();
		newText.font = Config.UI.Visual.DefaultFont;
		newText.fontSize = Config.UI.Description.FontSize;
		newText.fontWeight = Config.UI.Description.FontWeight;
		newText.verticalAlignment = VerticalAlignmentOptions.Middle;

		var textFitter = newTextObj.AddComponent<ContentSizeFitter>();
		textFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		descriptionInstance.rt = rect;
		descriptionInstance.text = newText;
	}
}