using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class PlayingMainUI {
	static readonly float BoxCornersSize = 50;
	static readonly float CreationNameBackgroundOpacity = .4f;
	static readonly float CBContentSize = 30; // the text under and controls

	// might turn ts generic but for now we will have ts
	public static CWindow SitControl;

	public static void SetSC() {
		SitControl = new PControl(
			"Sit",
			'e'
			).ToWindow();

		return;
		SitControl = new CWindow() {
			Name = "Sit Indicator",
			Config = new() {
				Resizable = false,
				Movable = false,
				Size = CWindow.Configuration.FixedSize(new(50, 50)),
				Closable = false
			},
			Items = new WindowItem[] {
				WindowItem.NewText(
					new PComponents.Text(
						"E",
						TextAlignmentOptions.Center
						),
					WindowItem.LayoutConfig.FillLayout
					)
			}
		};
	}

	static TextMeshProUGUI CBNameBox;

	public static CWindow CreationBox;
	public static void SetCB() {
		CreationBox = new CWindow() {
			Name = "Creation Box",
			Config = new() { // first time im actually using ts
				Resizable = false,
				Movable = false,
				Color = new(0, 0, 0, 0),
				Outline = (0, new(0, 0, 0, 0)),
				Closable = false
			},
			Items = new WindowItem[] {
				WindowItem.NewImage(
					new PComponents.Image("Icons/Playing/BoxBL"),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.BottomLeft),
						BoxCornersSize * Vector2.one)
				),
				WindowItem.NewImage(
					new PComponents.Image("Icons/Playing/BoxTL"),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.TopLeft),
						BoxCornersSize * Vector2.one)
				),
				WindowItem.NewImage(
					new PComponents.Image("Icons/Playing/BoxTR"),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.TopRight),
						BoxCornersSize * Vector2.one)
				),
				WindowItem.NewImage(
					new PComponents.Image("Icons/Playing/BoxBR"),
					WindowItem.LayoutConfig.FixedLayout(
						UIPosition.AnchoredAt(UIPosition.BottomRight),
						BoxCornersSize * Vector2.one)
				),
				WindowItem.NewText( // creation name
					new PComponents.Text(
						"Name",
						TextAlignmentOptions.TopLeft,
						fontSize: CBContentSize
					).OnRealised<PComponents.Text, TextMeshProUGUI>(c => CBNameBox = c),
					WindowItem.LayoutConfig.Custom(
						new(0, 200), // anything reasonably large
						new(
							new(0, 0),
							new(.5f, 0),
							new(0, 1),
							new(0, 0)
						)
					)
				)
			}
		};
	}

	public static CWindow OperateControl;
	public static void SetOC() {
		OperateControl = new PControl(
			"Operate",
			'q'
			).ToWindow();
	}

	public static void ConfigureCB(Transform target, Camera camera, string creationName) {
		List<Vector3> targetVertsWS = new();

		// gcic to include self
		foreach (var t in target.GetComponentsInChildren<Transform>()) {
			if (t.TryGetComponent<MeshFilter>(out var mf)) {
				var verts = mf.sharedMesh.vertices; // need the copy
				t.TransformPoints(verts);
				targetVertsWS.AddRange(verts);
			}
		}

		Vector3 camPos = camera.transform.position;
		Vector3 camFwd = camera.transform.forward;

		// optimize later
		Vector2[] vertsSS = targetVertsWS
			.Where(v => Vector3.Dot(v - camPos, camFwd) > 0) // only in front 
			.Select(v =>
			(Vector2)camera.WorldToScreenPoint(v)).ToArray();

		// possible if all behind camera
		if (vertsSS.Length == 0) return;

		Vector2 min = new(vertsSS.Select(v => v.x).Min(), vertsSS.Select(v => v.y).Min());
		Vector2 max = new(vertsSS.Select(v => v.x).Max(), vertsSS.Select(v => v.y).Max());

		Vector2 center = (min + max) / 2;
		Vector2 size = max - min;

		CreationBox.RealisedWindow.rt.position = center;
		CreationBox.RealisedWindow.rt.sizeDelta = size;

		Color bgcol = Config.UI.Visual.BackgroundColor;
		bgcol.a = CreationNameBackgroundOpacity;

		CBNameBox.text = $"<mark={bgcol.ToHex()}>{creationName}</mark>";

		OperateControl.RealisedWindow.SetWorldCorner(
			(Vector3)center + new Vector3(size.x, -size.y) / 2,
			2
		);
	}


	public static CWindow[] Windows => new CWindow[] {
		SitControl,
		CreationBox,
		OperateControl
	};

	public static void Set() {
		SetSC();
		SetCB();
		SetOC();
	}
}