using UnityEngine;
using TMPro;

public static partial class Config {
	public static class ScriptEditor {
		public static readonly TMP_FontAsset Font = Fonts.Consolas;
		public static readonly float FontSize = 20;

		public static readonly int	CursorBlinkRateMs = 530;
		public static readonly int	MultiClickThresholdMs = 500;

		public static readonly int	MaxClipboardSize = 100;
		public static readonly int	MaxCaretViewRecoverySteps = 100;
		public static readonly int	MaxHistoryLength = 100;
		public static readonly int	NewHistoryPauseThresholdMs = 1500;

		public static readonly float NumberDefaultWidth = 20;
		public static readonly float NumberToContentSpace = 20;
		public static readonly float NumberExtraWidth = 20;
		public static readonly float ContentExtraWidth = 100;

		public static readonly int XCursorScreenMarginChars = 2;
		public static readonly int YCursorScreenMarginLines = 2;

		public static readonly Color SelectionColor	= new Color(242, 58, 58, 39) / 255f;

		public static class SyntaxColors {
			public static readonly Color Keyword	= new Color(206	, 23	, 23	) / 255f;
			public static readonly Color Function	= new Color(230	, 121	, 255	) / 255f;
			public static readonly Color Variable	= new Color(157	, 220	, 253	) / 255f;
			public static readonly Color Unknown	= new Color(255	, 255	, 255	) / 255f;
			public static readonly Color Symbol		= new Color(255	, 255	, 255	) / 255f;
			public static readonly Color Literal	= new Color(19	, 223	, 19	) / 255f;
			public static readonly Color Type		= new Color(242	, 175	, 22	) / 255f;
			public static readonly Color Comment	= new Color(101	, 101	, 101	) / 255f;
		}
	}
}