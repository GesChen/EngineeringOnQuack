using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DynamicLayoutGroup : HorizontalOrVerticalLayoutGroup {
	public bool IsVertical = true;

	public override void CalculateLayoutInputHorizontal() {
		base.CalculateLayoutInputHorizontal();
		CalcAlongAxis(0, IsVertical);
	}

	public override void CalculateLayoutInputVertical() {
		CalcAlongAxis(1, IsVertical);
	}

	public override void SetLayoutHorizontal() {
		SetChildrenAlongAxis(0, IsVertical);
	}

	public override void SetLayoutVertical() {
		SetChildrenAlongAxis(1, IsVertical);
	}

	RectTransform rt;

	protected override void Start() {
		base.Start();
		rt = GetComponent<RectTransform>();
	}
	protected override void Update() {
		base.Update();
		IsVertical = rt.rect.height > rt.rect.width;
	}
}