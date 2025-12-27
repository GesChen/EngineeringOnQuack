using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayingManager : Singleton<PlayingManager>{
	public PlayerController Player;

	public string CurrentWorldName; // idk where to put it 

	void Start() {
		UpdateSeatsList();
		GameManager.Instance.WorldUpdated += UpdateSeatsList;

		SubscribeToShortcuts();
	}

	void SubscribeToShortcuts() {
		Conatrols.IM.Playing_Game.Edit.Subscribe<Contexts.Playing>(Edit, true);
		Conatrols.IM.Playing_Game.SaveWorld.Subscribe<Contexts.Playing>(TrySaveWorld, true);
		Conatrols.IM.Playing_Game.LoadWorld.Subscribe<Contexts.Playing>(TryLoadWorld, true);
	}

	void Edit() {
		GameManager.Instance.BeginEditing();
	}

	void Update() {

		// needed cuz some of these get pretty resource intensive
		if (Time.frameCount % Config.Player.Behaviour.LiveUIUpdateRate == 0) {
			if (ContextManager.CurrentlyInContext<Contexts.Playing>()
				&& !ContextManager.CurrentlyInContext<Contexts.Editing>())
				UpdateTargetedSeat();

			if (ContextManager.CurrentlyInContextStrict<Contexts.Playing>())
				UpdateTargetedCreation();
		}

		if (!ContextManager.CurrentlyInContextStrict<Contexts.Playing>()) return;

	}

	void UpdateSeatsList() {
		AllSeats = GameManager.Instance.CreationsContainer.GetComponentsInChildren<Part_Seat>();
	}

	Part_Seat[] AllSeats;
	internal Part_Seat TargetedSeat;
	void UpdateTargetedSeat() {
		if (ContextManager.GetCurrent<Contexts.Playing>().Sitting) {
			TargetedSeat = null;
			return;
		}

		// check for seats
		float seatDistSquared = Config.Player.Behaviour.SitDistance * Config.Player.Behaviour.SitDistance;

		// find most center one in vision aka highest dot
		float bestDot = -1;
		Part_Seat bestSeat = null;
		foreach (var seat in AllSeats) {
			float sqrdist = (seat.transform.position - Player.transform.position).sqrMagnitude;
			if (sqrdist < seatDistSquared) {
				// angle relative to view
				float dot = Vector3.Dot(
					Player.Camera.Camera.transform.forward,
					(seat.transform.position - Player.Camera.Camera.transform.position).normalized
					);

				if (dot > bestDot) {
					bestSeat = seat;
					bestDot = dot;
				}
			}
		}

		TargetedSeat = bestSeat;
	}

	internal Creation TargetedCreation;
	void UpdateTargetedCreation() {
		float targetDistSquared = Config.Player.Behaviour.CreationTargetDistance * Config.Player.Behaviour.CreationTargetDistance;

		float bestDot = -1;
		Creation bestCreation = null;
		foreach (var creation in OperatingManager.Instance.Creations) {

			// crucial to use the COM not the creation pos cuz that never changes

			Vector3 com = creation.GetCenterOfMassApprox();

			float sqrdist = (com - Player.transform.position).sqrMagnitude;
			if (sqrdist < targetDistSquared) {
				// angle relative to view
				float dot = Vector3.Dot(
					Player.Camera.Camera.transform.forward,
					(com - Player.Camera.Camera.transform.position).normalized
					);

				if (dot > bestDot) {
					bestCreation = creation;
					bestDot = dot;
				}
			}
		}

		TargetedCreation = bestCreation;
	}

	// temporaries till we get a proper full system
	void TrySaveWorld() {
		FileExplorer.CreateNewFE(
			Config.SaveLoad.WorldConfig.SaveLocation,
			new(
				FileExplorer.Type.SaveFile,
				new[] { Config.SaveLoad.WorldConfig.SaveExtension },
				FileExplorer.MetadataGetters.GetBytes,
				"Save",
				SaveWorld,
				5,
				"New World" + Config.SaveLoad.WorldConfig.SaveExtension,
				9
				)
			);

		GameManager.Instance.ShowCursor();
	}

	void SaveWorld(string path) {
		CurrentWorldName = HF.Depath(path);

		WorldStateManager.SaveCurrentWSToFile();
	}

	void TryLoadWorld() {
		FileExplorer.CreateNewFE(
			Config.SaveLoad.WorldConfig.SaveLocation,
			new(
				FileExplorer.Type.OpenFile,
				new string[] { Config.SaveLoad.WorldConfig.SaveExtension },
				FileExplorer.MetadataGetters.GetBytes,
				"Load",
				LoadWorld,
				5
			)
		);

		GameManager.Instance.ShowCursor();
	}

	void LoadWorld(string path) {
		WorldStateManager.LoadWorldState(HF.Depath(path));
	}
}