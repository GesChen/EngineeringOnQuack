using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayingManager : Singleton<PlayingManager>{
	void Start() {
		SubscribeToShortcuts();
	}

	void SubscribeToShortcuts() {
		Conatrols.IM.Playing_Player.Edit.Subscribe<Contexts.Playing>(Edit, true);
	}

	void Edit() {
		GameManager.Instance.BeginEditing();
	}
}