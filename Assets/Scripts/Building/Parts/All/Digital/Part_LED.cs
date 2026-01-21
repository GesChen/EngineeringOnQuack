using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part_LED : NonStaticPart {
	public override string PartName => "LED";

	public bool On = false;
	public Color Color = Color.red;
	public float Intensity = 1f;

	public MeshRenderer glowRenderer;
	public Material glowMat;

	private new void Awake() {
		base.Awake();

		glowMat = glowRenderer.material;  
	}

	void Set(bool state) {
		On = state;

		UpdateMaterial();
	}

	void UpdateMaterial() {
		//glowMat.SetFloat("_EmissiveColorMode", On ? 1f : 0f);

		glowMat.SetColor("_BaseColor", On ? Color : Color.black);
		glowMat.SetColor("_EmissiveColor", (On ? Color : Color.black) * Intensity);
		//glowMat.SetColor("_EmissiveColorLDR", On ? Color : Color.black);
		//glowMat.SetFloat("_EmissiveIntensity", On ? Intensity : 0);

		glowRenderer.material = glowMat; // refresh
	}

	public static Type Type_LED = new(
		"LED",
		new Dictionary<string, T_Data>() {
			{ "toggle",	new Primitive.Function("toggle", PartInternalFunctions.LED.toggle) },
			{ "set",	new Primitive.Function("set", PartInternalFunctions.LED.set) }
		}
	);

	public T_Data m_IDO;
	public override T_Data GetInternalLanguageDataObject() =>
		HF.LoadCached(
			ref m_IDO,
			() => new T_Data(Type_LED).SetThisMember("id", new Primitive.Number(Part.ID))
		);

	void IF_Toggle(int id) {
		if (id != Part.ID) return;

		Set(!On);
	}

	void IF_Set(int id, bool state) {
		if (id != Part.ID) return;

		Set(state);
	}

	public override void OnPartCreation() {
		Part.dontUpdateMaterialsFor = new[] { glowRenderer.transform };
	}

	public class CPart : Construct.Part {
		public SVector3 Color;
		public float Intensity;

		public override void FinalizeInstantiation(GameObject instantiatedPart, GameObject creation) {
			var newLED = instantiatedPart.GetComponent<Part_LED>();

			newLED.Color = Color;
			newLED.Intensity = Intensity;

			PartInternalFunctions.LED.OnToggleCalled += newLED.IF_Toggle;
			PartInternalFunctions.LED.OnSetCalled += newLED.IF_Set;
		}
	}

	public override void FinalizeCPartConversion(ref Construct.Part CPart) {
		var led = new CPart();

		led.CopyMembers(CPart);
		led.Color = Color;
		led.Intensity = Intensity;

		CPart = led;
	}

	public override void FinalizeCPartReconstruction(Construct.Part originalCPart, Part unfinishedPart, Assembly unfinishedAssembly) {
		var cpa = originalCPart as CPart;
		var newled = unfinishedPart.GetComponent<Part_LED>();

		newled.Color = cpa.Color;
		newled.Intensity = cpa.Intensity;
	}
}