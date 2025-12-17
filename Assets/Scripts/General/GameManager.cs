using System;
using UnityEngine;

public class GameManager : Singleton<GameManager> {
	public Transform MainPartsContainer;
	public Transform CreationsContainer;

	public Action<string> WM_LoadCollection;
	public Action BM_ClearEditing;
	public Action BM_TryLoadAssembly;
	public Action OM_DestroyCreation;
	public Action OM_AssembleFromEditing;
	public Action OM_Assemble;
	public Action OM_BeginOperating;
	public Action OM_SetCurrentAsLoadTarget;
	public Action SM_ResetState;

	// guaranteed to run before everything else thankfully
	protected override void Awake() {
		base.Awake();

		Config.Fonts.Reset();
	}

	void Start() {
		UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
		Application.targetFrameRate = Config.FPS_LIMIT;

		// manually enter playing to start for test
		WM_LoadCollection("playing");
		// context is handled by CO

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	// someone should call this to begin 
	// store a desired assembly in BM before calling
	// or it will make a new one
	public void BeginEditing() {
		if (ContextManager.CurrentlyInContext<Contexts.Operating>()) {
			OM_SetCurrentAsLoadTarget();

			OM_DestroyCreation();
		}

		SM_ResetState();

		BM_TryLoadAssembly();

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		WM_LoadCollection("editing");
		ContextManager.EnterContext<Contexts.Editing>();
	}

	public void ReturnToPlaying(bool destroyAssemblyIfOperating = false) {
		if (ContextManager.CurrentlyInContext<Contexts.Editing>()) {
			BM_ClearEditing();
		} else {
			if (destroyAssemblyIfOperating)
				OM_DestroyCreation();
		}

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		WM_LoadCollection("playing");
		ContextManager.EnterContextStrict<Contexts.Playing>();
	}

	// put it here bc
	// 1. might add extra processing
	// 2. consolidate all into gm
	// OM expects to assemble to be set before calling this btw
	public void AssembleFromEditing() {
		BM_ClearEditing();

		OM_AssembleFromEditing();
	}

	public void Operate() {
		OM_BeginOperating();

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;

		WM_LoadCollection("operating");
		ContextManager.EnterContext<Contexts.Operating>();
	}
}