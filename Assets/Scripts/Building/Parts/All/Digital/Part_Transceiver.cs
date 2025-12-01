using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part_Transceiver : NonStaticPart {
	public override string PartName => "Transceiver";
	public string TargetOutputName;
	
	public static void Setup() {

		// ----------editing-----------
		Transceiver_UI.RequestOutputs = () => {
			return GetOutputs();
		};

		Transceiver_UI.InitialSelection = InitialSelection;
		Transceiver_UI.OnItemSelected = OnItemSelected;

		Transceiver_UI.OnManageOutputsPressed = EditingOutputManager.Instance.OpenModifyOutputs;

		EditingOutputManager.Instance.OnOutputsChanged = () => {
			OutputsChanged();

			if (Transceiver_UI.OutputSelectionWindow.RealisedWindow.Open)
				UpdateUI();
		};
	}
	static string[] GetOutputs() {
		var transceivers = GetSelectedTransceivers(); // make sure a trans is selected
		if (transceivers.Length == 0)
			return Array.Empty<string>();

		return BuildingManager.Instance.Assembly.Outputs.ToArray();
	}

	static int InitialSelection() {
		var transceivers = GetSelectedTransceivers();
		if (transceivers.Length == 0
			|| !transceivers.All(t => t.TargetOutputName == transceivers[0].TargetOutputName))
			return -1;

		return BuildingManager.Instance.Assembly.Outputs.IndexOf(
			transceivers[0].TargetOutputName);
	}

	static void UpdateUI() {
		Transceiver_UI.UpdateOutputs();
	}

	static void OutputsChanged() {
		var setOut =
			BuildingManager.Instance.Assembly.Outputs.Count == 0
			? null
			: BuildingManager.Instance.Assembly.Outputs[0];

		var transceivers = BuildingManager.Instance.Assembly.Parts
			.Select(p => p.GetComponent<Part_Transceiver>())
			.Where(c => c != null);

		foreach (var t in transceivers) {
			if (string.IsNullOrEmpty(t.TargetOutputName) || setOut == null)
				t.TargetOutputName = setOut;
		}
	}

	static void OnItemSelected(int index) {
		if (index == -1) return; // nothing selected

		var transceivers = GetSelectedTransceivers();
		if (transceivers.Length == 0)
			return;

		foreach (var t in transceivers) {
			var outputs = BuildingManager.Instance.Assembly.Outputs;

			t.TargetOutputName = outputs[index];
		}
	}

	private static Part_Transceiver[] GetSelectedTransceivers() {
		var sel = SelectionManager.Instance.PartSelection;
		var transceivers = sel
			.Select(p => p.GetComponent<Part_Transceiver>())
			.Where(t => t != null).ToArray();

		return transceivers;
	}

	#region language
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
	public override T_Data GetInternalLanguageDataObject() => 
		HF.LoadCached(
			ref m_IDO,
			() => new T_Data(Type_Transceiver).SetThisMember("id", new Primitive.Number(Part.ID))
		);

	public static Action<string, string> OnPrintRequested;
	void Print(int id, string message) {
		if (id != Part.ID) return;

		if (string.IsNullOrEmpty(TargetOutputName)) return;

		OnPrintRequested?.Invoke(TargetOutputName, message);
	}

	#endregion

	#region overrides
	public class CPart : Construct.Part {
		public string Output;

		public override void FinalizeInstantiation(GameObject instantiatedPart) {
			var newTrans = instantiatedPart.GetComponent<Part_Transceiver>();

			newTrans.TargetOutputName = Output;

			PartInternalFunctions.Transceiver.OnPrintCalled += newTrans.Print;
		}
	}
	public override void FinalizeCPartConversion(ref Construct.Part CPart) {
		var trans = new CPart();

		trans.CopyMembers(CPart);
		trans.Output = TargetOutputName;

		CPart = trans;
	}
	public override void FinalizeCPartReconstruction(Construct.Part originalCPart, Part unfinishedPart, Assembly unfinishedAssembly) {
		var cpa = originalCPart as CPart;
		var newtrans = unfinishedPart.GetComponent<Part_Transceiver>();

		newtrans.TargetOutputName = cpa.Output;
	}
	#endregion
}