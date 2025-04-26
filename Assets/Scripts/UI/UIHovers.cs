using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHovers : MonoBehaviour {
	#region singleton
	private static UIHovers _instance;
	public static UIHovers Instance { get { return _instance; } }
	void Awake() { UpdateSingleton(); }
	private void OnEnable() { UpdateSingleton(); }
	void UpdateSingleton() {
		if (_instance != null && _instance != this) {
			Destroy(this);
		} else {
			_instance = this;
		}
	}
	#endregion

	[HideInNormalInspector] public Canvas detectionCanvas;

	public static List<Transform> hovers = new();
	public static List<RaycastResult> results = new();
	public List<Transform> hoversDebug;
	private GraphicRaycaster graphicRaycaster;
	private PointerEventData pointerEventData;
	private EventSystem eventSystem;

	public static bool CheckIgnoreOrder(Transform t) {
		return hovers.Contains(t);
	}

	public static bool CheckStrictlyFirst(Transform t) {
		if (hovers.Count == 0) return false;

		return hovers[0] == t;
	}

	// thx chatgpt i was too lazy to do this myself
	public static bool CheckFirstAllowing(Transform t, params Transform[] allowedInFront) {
		if (hovers.Count == 0) return false;

		foreach (var hover in hovers) {
			if (hover == t)
				return true;

			if (!allowedInFront.Any(t => hover.IsChildOf(t)))
				return false;
		}

		return false; // t wasn't found at all
	}

	void Start() {
		detectionCanvas = GetComponent<Canvas>();
		graphicRaycaster = GetComponent<GraphicRaycaster>();
		eventSystem = FindObjectOfType<EventSystem>();
	}

	void Update() {
		CheckUIRaycast();

		hoversDebug = hovers;
	}
	void CheckUIRaycast() {
		pointerEventData = new(eventSystem) {
			position = Input.mousePosition
		};

		results.Clear();
		graphicRaycaster.Raycast(pointerEventData, results);

		if (results.Count > 0) {
			hovers = results.Select(r => r.gameObject.transform).ToList();
		} else {
			hovers.Clear();
		}
	}
}