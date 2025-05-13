using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all components, probably shouldn't be used by itself.
/// </summary>
public class PUI_Component {
	public class Config {
		public string Text;
		public bool ShowText;
		public float FontSizeOverride;

		public string Description;

		public Sprite Icon;
		public bool ShowIcon;
		public float IconSizeOverride;

		public class Layout {
			public enum AlignmentTypes { Left, Center, Right }
			public AlignmentTypes Alignment;

			public enum FitTypes { FitContent, FitContainer }
			public FitTypes Fit;

			public Layout(AlignmentTypes alignment, FitTypes fit) {
				Alignment = alignment;
				Fit = fit;
			}

			public Layout(int alignment, int fit) {
				Alignment = (AlignmentTypes)alignment;
				Fit = (FitTypes)fit;
			}

			public static Layout Default = new(AlignmentTypes.Left, FitTypes.FitContent);
		}

		public Layout CurrentLayout;
		
		public Config(
			string text = null,
			bool showText = true,
			float fontSizeOverride = -1,
			string description = null,
			Sprite icon = null,
			bool showIcon = true,
			float iconSizeOverride = -1,
			Layout layout = null
			) {

			Text					= text;
			ShowText				= showText;
			if (text == null) ShowText = false;
			FontSizeOverride		= fontSizeOverride;
			Description				= description;
			Icon					= icon;
			ShowIcon				= showIcon;
			if (icon == null) ShowIcon = false;
			IconSizeOverride		= iconSizeOverride;
			CurrentLayout			= layout;
			if (layout == null) CurrentLayout = Layout.Default;
		}
	}

	public Config Configuration;

	protected PUI_Component(Config configuration) {
		Configuration = configuration;
	}
}