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
		new Memory(
			new Dictionary<string, T_Data>() {
				{ "toggle",	new Primitive.Function("toggle", PartInternalFunctions.LED.toggle) },
				{ "set",	new Primitive.Function("set", PartInternalFunctions.LED.set) }
			},
			new Dictionary<string, Type>(),
			"LED Type Snapshot"
			)
		);
	T_Data m_IDO;
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

	public override void FinalizeInstantiation(GameObject instantiatedPart) {
		var newLED = instantiatedPart.GetComponent<Part_LED>();

		PartInternalFunctions.LED.OnToggleCalled += newLED.IF_Toggle;
		PartInternalFunctions.LED.OnSetCalled += newLED.IF_Set;
	}
}