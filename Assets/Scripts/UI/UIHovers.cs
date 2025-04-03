using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHovers : MonoBehaviour
{
	#region singleton
	private static UIHovers _instance;
	public static UIHovers Instance { get { return _instance; } }
	void Awake() { UpdateSingleton(); }
	private void OnEnable() { UpdateSingleton(); }
	void UpdateSingleton() {
		if (_instance != null && _instance != this) {
			Destroy(this);
		}
		else {
			_instance = this;
		}
	}
	#endregion

	public static List<Transform> hovers = new();
	public List<Transform> hoversDebug;
	private GraphicRaycaster graphicRaycaster;
	private PointerEventData pointerEventData;
	private EventSystem eventSystem;

	public static bool Check(Transform t) {
		if (Instance == null) {
			Canvas canvas = Config.UI.MainCanvas;
			if (canvas == null) {
				Debug.LogError("No main canvas set");
				return false;
			}
			UIHovers newHovers = canvas.gameObject.AddComponent<UIHovers>();
			newHovers.UpdateSingleton();
		}

		return hovers.Contains(t);
	}
	public static bool Check(RectTransform rt) {
		return Check(rt.transform);
	}

	void Start() {
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

		List<RaycastResult> results = new();
		graphicRaycaster.Raycast(pointerEventData, results);

		if (results.Count > 0) {
			hovers.Clear();
			foreach (var result in results)
				hovers.Add(result.gameObject.transform);
		}
	}
}