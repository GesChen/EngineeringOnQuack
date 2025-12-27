using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Config;

public class PlayingUIManager : Singleton<PlayingUIManager> {

	void LateUpdate() {
		if (!ContextManager.CurrentlyInContextStrict<Contexts.Playing>()) return;

		UpdateSitIndicator();
		UpdateCreationBox();
	}

	void UpdateSitIndicator() {
		if (PlayingManager.Instance.TargetedSeat == null
			|| Vector3.Dot( // seat's behind the camera. really fucking long winded way to write it.
				// def a better way im not doing too lazy sorry
				PlayingManager.Instance.TargetedSeat.transform.position
				- PlayingManager.Instance.Player.Camera.Camera.transform.position,
				PlayingManager.Instance.Player.Camera.Camera.transform.forward) < 0) {
			PlayingMainUI.SitControl.RealisedWindow.Hide();
			return;
		}

		Vector2 seatScreenPos = 
			PlayingManager.Instance.Player.Camera.Camera.WorldToScreenPoint(
				PlayingManager.Instance.TargetedSeat.transform.position);

		PlayingMainUI.SitControl.RealisedWindow.Show();
		PlayingMainUI.SitControl.RealisedWindow.SetWorldCorner(seatScreenPos, 4);
	}

	void UpdateCreationBox() {
		if (PlayingManager.Instance.TargetedCreation == null) {
			PlayingMainUI.CreationBox.RealisedWindow.Hide();
			PlayingMainUI.OperateControl.RealisedWindow.Hide();
			return;
		}

		PlayingMainUI.CreationBox.RealisedWindow.Show();
		PlayingMainUI.OperateControl.RealisedWindow.Show();

		PlayingMainUI.ConfigureCB(
			PlayingManager.Instance.TargetedCreation.transform,
			PlayingManager.Instance.Player.Camera.Camera, 
			PlayingManager.Instance.TargetedCreation.Construct.Name);

	}
}