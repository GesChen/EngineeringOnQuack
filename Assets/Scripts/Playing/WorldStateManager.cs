using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public static class WorldStateManager {

	public static SaveLoadHelper<WorldState> SaveLoadHelper = new(Config.SaveLoad.WorldConfig);

	public static string CurrentWorldName = null;

	// designed to b serializable
	public class WorldState {
		public string Name;

		public Creation.Serializable[] Creations;
		public PlayerState Player;

		public string CurrentContextSerialized;
		public string CurrentWindowCollection;
		public bool CursorEnabled;

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

		// store playerstate separately for multiplayer
		public struct PlayerState {
			public TransformData Transform;
			
			public float Yaw;
			public float Pitch;

			public SVector3 Velocity;

			public int CurrentlySittingOnPartID;
			public int CurrentlyOperatingCreationID;

			public bool InFirstPerson;
			public float TPDistance;
		}
	}
		
	public static WorldState GetCurrentWorldState() => new() {
		Name = CurrentWorldName,
		Creations = OperatingManager.Instance.Creations.Select(c => c.ConvertToSerializable()).ToArray(),
		Player = GetCurrentPlayerState(),
		CurrentContextSerialized = SerializeContext(ContextManager.Current),
		CurrentWindowCollection = WindowManager.Instance.currentlyLoadedCollection,
		CursorEnabled = GameManager.Instance.CursorEnabled
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
			CurrentlyOperatingCreationID = 
				OperatingManager.Instance.CurrentlyOperating == null
				? -1
				: OperatingManager.Instance.CurrentlyOperating.ID,
			InFirstPerson = player.Camera.FirstPerson,
			TPDistance = player.Camera.tpDistance
		};
	}

	static string SerializeContext(IContext context) =>
		JsonConvert.SerializeObject(
			new WorldState.ContextWrapper(context),
			WorldState.ContextSerializationSettings);

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

		// restore operating
		if (state.Player.CurrentlyOperatingCreationID != -1) {
			OperatingManager.Instance.CurrentlyOperating =
				OperatingManager.Instance.FindCreation(state.Player.CurrentlyOperatingCreationID);
		}

		// restore sit
		player.Unsit(); // has null check
		if (state.Player.CurrentlySittingOnPartID != -1) {
			player.CurrentlySittingOn =
				OperatingManager.Instance.FindPartInWorld(state.Player.CurrentlySittingOnPartID)
				.GetNSP<Part_Seat>();
			
			player.SetupSit();
		}

		player.Camera.FirstPerson = state.Player.InFirstPerson;
		player.Camera.tpDistance = state.Player.TPDistance;

		// restore context
		var context = state.GetCurrentContext();
		ContextManager.ForceEnterContext(context);

		// restore ui 
		WindowManager.Instance.RealiseCollection(state.CurrentWindowCollection);

		// restore cursor
		GameManager.Instance.CursorEnabled = state.CursorEnabled;

		GameManager.Instance.WorldUpdated();
	}

	static readonly IContext NewWorldContext = new Contexts.Playing() {
		Parent = new Contexts.Main(){
			Parent = null,
			OverUI = false
		},
		Sitting = false
	};

	static readonly WorldState NewWorldWS = new(){
		Name = null,
		Creations = new Creation.Serializable[0],
		Player = new() {
			Transform = new() {
				position = new(0, 1, 0),
				rotation = new(0, 0, 0, 1),
				localScale = new(1, 1, 1)
			},
			Yaw = 0,
			Pitch = 0,
			Velocity = new(0, 0, 0),
			CurrentlySittingOnPartID = -1,
			CurrentlyOperatingCreationID = -1,
			InFirstPerson = true,
			TPDistance = 0
		},
		CurrentContextSerialized = SerializeContext(NewWorldContext),
		CurrentWindowCollection = "playing",
		CursorEnabled = false
	};

	public static void NewWorld() {
		ReconstructWorldState(NewWorldWS);
	}
}