using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public static class WorldStateManager {

	public static SaveLoadHelper<WorldState> SaveLoadHelper = new(Config.SaveLoad.WorldConfig);

	// designed to b serializable
	public class WorldState {
		public string Name;

		public Creation.Serializable[] Creations;
		public PlayerState Player;

		public string CurrentContextSerialized;

		internal static readonly JsonSerializerSettings ContextSerializationSettings = new(){
			TypeNameHandling = TypeNameHandling.Auto,
			PreserveReferencesHandling = PreserveReferencesHandling.Objects
			//Formatting = Formatting.Indented
		};

		public struct ContextWrapper {
			public IContext Context;
			public ContextWrapper(IContext context) { Context = context;}
		}

		public IContext GetCurrentContext() {
			var cwrapper = JsonConvert.DeserializeObject<ContextWrapper>
				(CurrentContextSerialized, ContextSerializationSettings);

			return cwrapper.Context;
		}

		public struct PlayerState {
			public TransformData Transform;
			
			public float Yaw;
			public float Pitch;

			public SVector3 Velocity;

			public int CurrentlySittingOnPartID;

			public bool InFirstPerson;
			public float TPDistance;
		}
	}

	public static WorldState GetCurrentWorldState() => new() {
		Name = PlayingManager.Instance.CurrentWorldName,
		Creations = OperatingManager.Instance.Creations.Select(c => c.ConvertToSerializable()).ToArray(),
		Player = GetCurrentPlayerState(),
		CurrentContextSerialized = JsonConvert.SerializeObject(
			new WorldState.ContextWrapper(ContextManager.Current),
			WorldState.ContextSerializationSettings)
	};

	public static WorldState.PlayerState GetCurrentPlayerState() {
		PlayerController player = PlayingManager.Instance.Player;

		return new() {
			Transform = (TransformData)player.transform,
			Yaw = player.yaw,
			Pitch = player.pitch,
			Velocity = player.rb.velocity,
			CurrentlySittingOnPartID =
				player.CurrentlySittingOn == null
				? -1
				: player.CurrentlySittingOn.Part.ID,
			InFirstPerson = player.Camera.FirstPerson,
			TPDistance = player.Camera.tpDistance
		};
	}

	// this is all gonna change soon 

	public static void SaveCurrentWSToFile() {
		var worldstate = GetCurrentWorldState();

		SaveLoadHelper.Save(worldstate, worldstate.Name, new object[] { worldstate.Creations.Length });
	}

	public static void LoadWorldState(string name) {
		var ws = SaveLoadHelper.Load(name);

		ReconstructWorldState(ws);
	}

	// prolly gonna b laggy. might consider turning into a coroutine
	public static void ReconstructWorldState(WorldState state) {
		// destroy all creations
		foreach (var creation in OperatingManager.Instance.Creations)
			OperatingManager.Instance.DestroyCreation(creation);

		// remake all creations
		foreach (var cs in state.Creations) {
			Assembler.Instance.ReconstructCreation(cs, out var created);
			OperatingManager.Instance.Creations.Add(created);
		}

		// restore player state
		var player = PlayingManager.Instance.Player;
		state.Player.Transform.ApplyToTransform(player.transform);
		player.yaw = state.Player.Yaw;
		player.pitch = state.Player.Pitch;
		
		player.rb.velocity = state.Player.Velocity;

		player.Unsit();
		if (state.Player.CurrentlySittingOnPartID != -1)
			player.CurrentlySittingOn = 
				OperatingManager.Instance.FindPartInWorld(state.Player.CurrentlySittingOnPartID)
				.GetNSP<Part_Seat>();

		player.Camera.FirstPerson = state.Player.InFirstPerson;
		player.Camera.tpDistance = state.Player.TPDistance;

		// restore context
		var context = state.GetCurrentContext();
		ContextManager.ForceEnterContext(context);
	}
}