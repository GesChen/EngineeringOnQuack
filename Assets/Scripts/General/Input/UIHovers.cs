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
	
	static void PerformCheck() {
		if (_instance == null) {
			throw new("UIHovers singleton missing!");
		}
		if (_instance.canvases.Count == 0)
			Debug.LogWarning("UIHovers canvases list is empty");
	}
	#endregion

	public List<Canvas> canvases; // Assign in Inspector or pass programmatically
	private List<GraphicRaycaster> graphicRaycasters = new();

	public static List<Transform> hovers = new();
	public static List<RaycastResult> results = new();
	public List<Transform> hoversDebug;
	private GraphicRaycaster graphicRaycaster;
	private PointerEventData pointerEventData;
	private EventSystem eventSystem;

	public static bool CheckIgnoreOrder(Transform t) {
		PerformCheck();
		return hovers.Contains(t);
	}

	public static bool CheckStrictlyFirst(Transform t) {
		PerformCheck();
		if (hovers.Count == 0) return false;

		return hovers[0] == t;
	}

	// thx chatgpt i was too lazy to do this myself
	public static bool CheckFirstAllowing(Transform t, params Transform[] allowedInFront) {
		PerformCheck();
		if (hovers.Count == 0) return false;

		foreach (var hover in hovers) {
			if (hover == t)
				return true;

			if (!allowedInFront.Any(t => hover.IsChildOf(t)))
				return false;
		}

		return false; // t wasn't found at all
	}

	// this kinda tedious work small change is a good use of ai
	void Start() {
		// Cache all raycasters
		graphicRaycasters = canvases
			.Select(c => c.GetComponent<GraphicRaycaster>())
			.Where(gr => gr != null)
			.ToList();

		eventSystem = EventSystem.current;
	}

	void Update() {
		CheckUIRaycast();
		hoversDebug = new List<Transform>(hovers);
	}

	void CheckUIRaycast() {
		pointerEventData = new PointerEventData(eventSystem) {
			position = Input.mousePosition
		};

		results.Clear();

		foreach (var raycaster in graphicRaycasters) {
			List<RaycastResult> tempResults = new();
			raycaster.Raycast(pointerEventData, tempResults);
			results.AddRange(tempResults);
		}

		if (results.Count > 0) {
			hovers = results.Select(r => r.gameObject.transform).ToList();
		} else {
			hovers.Clear();
		}
	}
}