using UnityEngine;

/// <summary>
/// Robust MonoBehaviour singleton base class.
/// Usage: inherit your MonoBehaviour from Singleton<T>.
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
	private static T _instance;
	private static readonly object _lock = new();

	/*static Singleton() {
		_instance = null;
	}*/

	public static T Instance {
		get {
			/*if (_applicationIsQuitting) {
				Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Returning null.");
				return null;
			}*/

			lock (_lock) {
				if (_instance == null) {
					// Look for existing instance.
					_instance = FindObjectOfType<T>();

					if (_instance == null) {
						Debug.LogError($"[Singleton] No instance of {typeof(T)} found in scene.");
					}
				}

				return _instance;
			}
		}
	}

	public static bool InstanceExists => _instance != null;

	//private static bool _applicationIsQuitting = false;

	protected virtual void Awake() {
		lock (_lock) {
			if (_instance != null && _instance != this) {
				// Safely destroy duplicates depending on play mode or editor
				if (Application.isPlaying)
					Destroy(gameObject);
				else
					DestroyImmediate(gameObject);
				return;
			}

			_instance = this as T;
			//_applicationIsQuitting = false;

			// Optional: Persist singleton across scenes
			if (ShouldPersist()) {
				DontDestroyOnLoad(gameObject);
			}
		}
	}

	/// <summary>
	/// Override to control persistence behavior (default: false).
	/// </summary>
	protected virtual bool ShouldPersist() => false;

	protected virtual void OnDestroy() {
		if (_instance == this) {
			_instance = null;
			//_applicationIsQuitting = true;
		}
	}
	// ask chatgpt again about this idfk
/*
#if UNITY_EDITOR
	// Clear instance on domain reload / script recompilation in Editor to avoid stale references
	[UnityEditor.InitializeOnLoadMethod]
	protected static void EditorInitialize() {
		_instance = null;
		_applicationIsQuitting = false;
	}
#endif

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void RuntimeInitialize() {
		Debug.Log("reset");
		_instance = null;
		_applicationIsQuitting = false;
	}*/
}
