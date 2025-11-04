using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part_Transceiver : NonStaticPart {
	public override string PartName => "Transceiver";
	public Output TargetOutput;
	
	public static void Setup() {
		Transceiver_UI.RequestOutputs = () => {
			OutputsChanged();
			return GetOutputs();
		};

		Transceiver_UI.InitialSelection = InitialSelection;
		Transceiver_UI.OnItemSelected = OnItemSelected;

		SelectionManager.Instance.OnSelectionChanged += () => {
			if (Transceiver_UI.OutputSelectionWindow.RealisedWindow.Open) {
				OutputsChanged();
				UpdateUI();
			}
		};

		OutputManager.Instance.OnOutputsChanged = () => {
			if (!Transceiver_UI.OutputSelectionWindow.RealisedWindow.gameObject.activeSelf)
				return;

			OutputsChanged();
			UpdateUI();
		};

		GameManager.Instance.OnStartSimulating += OutputsChanged;
	}
	static string[] GetOutputs() {
		var transceivers = GetSelectedTransceivers();
		if (transceivers.Length == 0)
			return Array.Empty<string>();

		var outs = BuildingManager.Instance.Assembly.Outputs;

		// still cant figure out why i added the i when its just the index
		// might remove it later for speed
		return outs.Select(o => o.Name).ToArray();
	}

	static int InitialSelection() {
		var transceivers = GetSelectedTransceivers();
		if (transceivers.Length == 0
			|| !transceivers.All(t => t.TargetOutput == transceivers[0].TargetOutput))
			return -1;

		return BuildingManager.Instance.Assembly.Outputs.IndexOf(
			transceivers[0].TargetOutput);
	}

	static void UpdateUI() {
		Transceiver_UI.UpdateOutputs();
	}

	static void OutputsChanged() {
		var setOut =
			BuildingManager.Instance.Assembly.Outputs.Count == 0
			? null
			: BuildingManager.Instance.Assembly.Outputs[0];

		var transceivers = GetSelectedTransceivers();
		foreach (var t in transceivers) {
			t.TargetOutput ??= setOut;
		}
	}

	static void OnItemSelected(int index) {
		if (index == -1) return; // nothing selected

		var transceivers = GetSelectedTransceivers();
		if (transceivers.Length == 0)
			return;

		foreach (var t in transceivers) {
			var outputs = BuildingManager.Instance.Assembly.Outputs;

			t.TargetOutput = outputs[index];
		}
	}

	private static Part_Transceiver[] GetSelectedTransceivers() {
		var sel = SelectionManager.Instance.PartSelection;
		var transceivers = sel
			.Select(p => p.GetComponent<Part_Transceiver>())
			.Where(t => t != null).ToArray();

		return transceivers;
	}

	public override void FinalizeInstantiation(GameObject instantiatedPart) {
		var newTrans = instantiatedPart.GetComponent<Part_Transceiver>();

		newTrans.TargetOutput = TargetOutput;

		PartInternalFunctions.Transceiver.OnPrintCalled += PrintInternal;
	}

	public static Type Type_Transceiver = new(
		"Transceiver",
		new Memory(
			new Dictionary<string, T_Data>(){
				{ "print", new Primitive.Function("print", PartInternalFunctions.Transceiver.print) }
			},
			new Dictionary<string, Type>(),
			"Transceiver Type Snapshot"
			)
		);

	private T_Data m_IDO;
	public T_Data InternalDataObject =>
		HF.LoadCached(
			ref m_IDO,
			() => new T_Data(Type_Transceiver).SetThisMember("id", new Primitive.Number(Part.ID))
		);

	public override T_Data InternalLanguageDataObject() => InternalDataObject;

	public override void HandleCommand(string command, object[] args) {
		if (command == "print") {
			Print(args);
			return;
		}

		Debug.LogError(UnknownCommand(command));
	}

	void PrintInternal(int partID, string message) {
		if (partID != Part.ID) return;

		Print(new[] { message });
	}

	void Print(object[] args) {
		if (args.Length != 1) {
			Debug.LogError(BadArgumentCount("print", 1, args.Length));
			return;
		}

		//Debug.Log(args[0]?.ToString() ?? "null");
		TargetOutput?.Print(args[0].ToString()); // should already be a string obj
	}
}