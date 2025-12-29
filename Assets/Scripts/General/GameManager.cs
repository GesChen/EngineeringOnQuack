using System;
using UnityEngine;

public class GameManager : Singleton<GameManager> {
	public bool Paused;

	public Transform MainPartsContainer;
	public Transform CreationsContainer;

	public Action<string> WM_LoadCollection;
	public Action WM_Pause;
	public Action WM_UnPause;
	public Action BM_ClearEditing;
	public Action BM_TryLoadAssembly;
	public Action OM_DestroyCreation;
	public Action OM_AssembleFromEditing;
	public Action OM_Assemble;
	public Action OM_BeginOperating;
	public Action OM_SetCurrentAsLoadTarget;
	public Action SelM_ResetState;
	public Action PC_AutoSit;
	public Action PC_Unsit;

	public Action WorldUpdated;

	public bool CursorEnabled;

	// guaranteed to run before everything else thankfully
	protected override void Awake() {
		base.Awake();

		Config.Fonts.Reset();

		WorldUpdated = null;
	}

	void Start() {
		UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
		Application.targetFrameRate = Config.FPS_LIMIT;

		// manually enter playing to start for test
		WM_LoadCollection("playing");
		// context is handled by CO

		DisableCursor();
	}

	void Update() {
		HandleCursor();
	}

	#region cursor
	void HandleCursor() {
		bool over = ContextManager.GetCurrent<Contexts.Main>().OverUI;

		// idk when to check
		if (Conatrols.Mouse.Left.PressedThisFrame) {
			if (over) {
				ShowCursor();
			} else {
				if (!CursorEnabled) HideCursor();
			}
		}
	}
	void EnableCursor() {
		CursorEnabled = true;

		ShowCursor();
	}
	public void ShowCursor() {
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}
	void DisableCursor() {
		CursorEnabled = false;

		HideCursor();
	}
	public void HideCursor() {
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
	#endregion

	// someone should call this to begin 
	// store a desired assembly in BM before calling
	// or it will make a new one
	public void BeginEditing() {
		if (ContextManager.CurrentlyInContext<Contexts.Operating>()) {
			OM_SetCurrentAsLoadTarget();

			OM_DestroyCreation();

			PC_Unsit();

			WorldUpdated();
		}

		SelM_ResetState();

		BM_TryLoadAssembly();

		EnableCursor();

		WM_LoadCollection("editing");
		ContextManager.EnterContext<Contexts.Editing>();
	}

	public void ReturnToPlaying(bool destroyAssemblyIfOperating = false) {
		if (ContextManager.CurrentlyInContext<Contexts.Editing>()) {
			BM_ClearEditing();
		} else {
			PC_Unsit();

			if (destroyAssemblyIfOperating) {
				OM_DestroyCreation();
				WorldUpdated();
			}
		}

		DisableCursor();

		WM_LoadCollection("playing");
		ContextManager.EnterContext<Contexts.Playing>(true);
	}

	// put it here bc
	// 1. might add extra processing
	// 2. consolidate all into gm
	// OM expects to assemble to be set before calling this btw
	public void AssembleFromEditing() {
		BM_ClearEditing();

		OM_AssembleFromEditing();

		WorldUpdated();
	}

	public void Operate(bool autoSit = true) {
		OM_BeginOperating();

		EnableCursor();

		if (autoSit)
			PC_AutoSit();

		WM_LoadCollection("operating");
		ContextManager.EnterContext<Contexts.Operating>();
	}

	bool cursorStateBeforePause;
	public void Pause() {
		Paused = true;

		cursorStateBeforePause = CursorEnabled;

		EnableCursor();

		WM_Pause();
	}

	public void UnPause() {
		Paused = false;

		if (!cursorStateBeforePause)
			DisableCursor();

		WM_UnPause();
	}
}