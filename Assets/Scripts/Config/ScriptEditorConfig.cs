using UnityEngine;

public static partial class Config {
	public static class ScriptEditor {
		public static readonly float	RepeatDelayMs = 500;
		public static readonly float	RepeatRateCPS = 31;
		public static readonly float	CursorBlinkRateMs = 530;
		public static readonly float	MultiClickThresholdMs = 500;

		public static readonly float	MaxClipboardSize = 100;
		public static readonly int		MaxCaretViewRecoverySteps = 100;
		public static readonly int		MaxHistoryLength = 100;
		public static readonly float	NewHistoryPauseThresholdMs = 3000;

		public static class Colors {
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