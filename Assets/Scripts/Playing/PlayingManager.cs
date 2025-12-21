using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayingManager : Singleton<PlayingManager>{
	public PlayerController Player;

	void Start() {
		UpdateSeatsList();
		GameManager.Instance.WorldUpdated += UpdateSeatsList;

		SubscribeToShortcuts();
	}

	void SubscribeToShortcuts() {
		Conatrols.IM.Playing_Player.Edit.Subscribe<Contexts.Playing>(Edit, true);
	}


	void Edit() {
		GameManager.Instance.BeginEditing();
	}

	void Update() {
		if (ContextManager.CurrentlyInContext<Contexts.Playing>()
			&& !ContextManager.CurrentlyInContext<Contexts.Editing>())
			CheckIndicators();

		if (!ContextManager.CurrentlyInContextStrict<Contexts.Playing>()) return;

	}

	void UpdateSeatsList() {
		AllSeats = GameManager.Instance.CreationsContainer.GetComponentsInChildren<Part_Seat>();
	}

	Part_Seat[] AllSeats;
	internal Part_Seat TargetedSeat;
	void CheckIndicators() {
		if (ContextManager.GetCurrent<Contexts.Playing>().Sitting) {
			TargetedSeat = null;
			PlayingMainUI.SitIndicator.RealisedWindow.SetState(false);
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

		if (bestSeat == null) {
			PlayingMainUI.SitIndicator.RealisedWindow.SetState(false);
			return;
		}

		PlayingMainUI.SitIndicator.RealisedWindow.SetState(true);

		Vector2 seatScreenPos = Player.Camera.Camera.WorldToScreenPoint(bestSeat.transform.position);
		PlayingMainUI.SitIndicator.RealisedWindow.PlaceAt(seatScreenPos, 0, false);
	}
}