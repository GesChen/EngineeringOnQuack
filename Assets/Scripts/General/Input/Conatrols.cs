using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Linq;

// if the misspelling makes it into final, this is why:
// every time i do con- then intellisense always pulls up some bullshit other conttroller
// garbage so i added an a so itd be the first result or close to
// so i dont have to keep JDFLKJLK:DSLK:JFL stupid thing

public class Conatrols : MonoBehaviour {
	public static float clickMaxDist = 5;
	public static float clickMaxTime = .1f;

	public static InputMaster IM;
	Keyboard CurrentKeyboard;
	Mouse CurrentMouse;

	void Awake() {
		IM = new();

		CurrentKeyboard = new();
		CurrentMouse = new();
		CurrentMouse.Initialize();
	}
	void OnEnable() {
		IM ??= new InputMaster();
		IM.Enable();
	}
	void OnDisable() {
		IM.Disable();
	}

	void Update() {
		CurrentMouse.Update();
		CurrentKeyboard.Update();
	}


	#region Shortcut Methods
	/// <summary>
	/// Repeating
	/// </summary>
	public static bool IsUsed(Key key) => Keyboard.Presses.Contains(key);
	/// <summary>
	/// Non-Repeating
	/// </summary>
	public static bool IsPressed(Key key) => Keyboard.Pressed.Contains(key);
	#endregion

	public class Mouse {
		public static Vector2 Position;
		public static Vector2 LastPos;
		public static Vector2 Delta;
		public static Vector2 Scroll;

		public static MouseButton Left;
		public static MouseButton Middle;
		public static MouseButton Right;

		// add more when needed like the side ones

		public void Initialize() {
			Left = new(IM.Mouse.Left);
			Middle = new(IM.Mouse.Middle);
			Right = new(IM.Mouse.Right);
		}

		public void Update() {
			LastPos = Position;
			Position = IM.Mouse.Position.ReadValue<Vector2>();
			Delta = IM.Mouse.Delta.ReadValue<Vector2>();
			Scroll = IM.Mouse.Scroll.ReadValue<Vector2>();

			Left.Update();
			Middle.Update();
			Right.Update();
		}

		public class MouseButton {
			public InputAction Binding;
			public bool Pressed;
			public bool PressedThisFrame;
			public bool ReleasedThisFrame;

			public MouseButton(InputAction binding) { Binding = binding; }

			public void Update() {
				Pressed = Binding.IsPressed();
				PressedThisFrame = Binding.WasPressedThisFrame();
				ReleasedThisFrame = Binding.WasReleasedThisFrame();
			}
		}
	}

	public class Keyboard {

		/// <summary>
		/// Repeating, holds repeat + down on frame
		/// </summary>
		public static List<Key> Presses;
		public void Update() {
			UpdateKeyboard();
			UpdateKeyHeldTimes();

			Presses = GetRepeats().Union(PressedThisFrame).ToList();

			Modifiers.Update();
		}

		public static List<Key> LastPressed = new();
		/// <summary>
		/// Non repeating, holding stays held
		/// </summary>
		public static List<Key> Pressed; // held ones stay held, presses repeats held
		public static List<Key> PressedThisFrame;
		public static List<Key> ReleasedThisFrame;
		public void UpdateKeyboard() {
			Pressed = KeyboardFastPoll.GetAllPressedKeys();

			// might be kinda slow but idk
			PressedThisFrame = Pressed.Except(LastPressed).ToList();
			ReleasedThisFrame = LastPressed.Except(Pressed).ToList();

			LastPressed = Pressed;
		}

		// do this later if list contains ends up being too slow
		void UpdateHashSets() {

		}

		// dw about speed, its doing like .05ms so 20k fps \_("/)_/
		/*
		List<Key> GetAllPressedKeys() {
			List<Key> pressed = new();

			var kb = UnityEngine.InputSystem.Keyboard.current;
			foreach (KeyControl kc in kb.allKeys) {
				if (kc.isPressed) pressed.Add(kc.keyCode);
				if (kc.keyCode == Key.Enter) 
					print($"chedck ent");
			}
			print(pressed.Count);

			return pressed;
		}
*/
		// these two can be optimized away into one timer sort of thing if you want
		readonly Dictionary<Key, float> KeyHeldTimes = new();
		readonly Dictionary<Key, float> KeyLastRepeatTime = new();
		void UpdateKeyHeldTimes() {
			foreach (Key k in PressedThisFrame) {
				KeyHeldTimes.Add(k, Time.time);
				KeyLastRepeatTime.Add(k, Time.time);
			}
			foreach (Key k in ReleasedThisFrame) {
				KeyHeldTimes.Remove(k);
				KeyLastRepeatTime.Remove(k);
			}
		}

		List<Key> GetRepeats() {
			List<Key> keys = new();
			foreach (KeyValuePair<Key, float> keytime in KeyHeldTimes) {
				if (Time.time - keytime.Value > Config.ScriptEditor.RepeatDelayMs / 1000f && // long enough held
					Time.time - KeyLastRepeatTime[keytime.Key] > 1f / Config.ScriptEditor.RepeatRateCPS) { // long enough since last repeat
					keys.Add(keytime.Key);
					KeyLastRepeatTime[keytime.Key] = Time.time;
				}
			}

			return keys;
		}
	
		public static class Modifiers {
			public static bool Ctrl { get; private set; }
			public static bool Shift { get; private set; }
			public static bool Alt { get; private set; }
			public static bool Any { get; private set; }

			public static void Update() {
				// controls.keyboard.modifiers.alt is shorter still
				Ctrl = Pressed.Contains(Key.LeftCtrl) || Pressed.Contains(Key.RightCtrl);
				Shift = Pressed.Contains(Key.LeftShift) || Pressed.Contains(Key.RightShift);
				Alt = Pressed.Contains(Key.LeftAlt) || Pressed.Contains(Key.RightAlt);
				Any = Ctrl || Shift || Alt;
			}
		}

		public static class All {
			public static readonly HashSet<Key> Modifiers = new() {
				Key.LeftCtrl,
				Key.RightCtrl,
				Key.LeftAlt,
				Key.RightAlt,
				Key.LeftShift,
				Key.RightShift
			};

			public static readonly HashSet<Key> Alphabetical = new() {
				Key.A,
				Key.B,
				Key.C,
				Key.D,
				Key.E,
				Key.F,
				Key.G,
				Key.H,
				Key.I,
				Key.J,
				Key.K,
				Key.L,
				Key.M,
				Key.N,
				Key.O,
				Key.P,
				Key.Q,
				Key.R,
				Key.S,
				Key.T,
				Key.U,
				Key.V,
				Key.W,
				Key.X,
				Key.Y,
				Key.Z
			};

			public static readonly HashSet<Key> Numerical = new() {
				Key.Digit1,
				Key.Digit2,
				Key.Digit3,
				Key.Digit4,
				Key.Digit5,
				Key.Digit6,
				Key.Digit7,
				Key.Digit8,
				Key.Digit9,
				Key.Digit0
			};

			public static readonly HashSet<Key> NumericalNumpad = new() {
				Key.Numpad1,
				Key.Numpad2,
				Key.Numpad3,
				Key.Numpad4,
				Key.Numpad5,
				Key.Numpad6,
				Key.Numpad7,
				Key.Numpad8,
				Key.Numpad9,
				Key.Numpad0
			};

			public static readonly HashSet<Key> Symbolic = new() {
				Key.Backquote,
				Key.Minus,
				Key.Equals,
				Key.LeftBracket,
				Key.RightBracket,
				Key.Backslash,
				Key.Semicolon,
				Key.Quote,
				Key.Comma,
				Key.Period,
				Key.Slash
			};

			// all keys used in actual text editing (includes enter 
			public static readonly HashSet<Key> TextKeys =
						Alphabetical
				.Concat(Numerical)
				.Concat(NumericalNumpad)
				.Concat(Symbolic)
				.Concat(new Key[] { 
						Key.Space,
						Key.Backspace,
						Key.Delete,
						Key.Enter,
						Key.Tab})
				.ToHashSet();

			// could also just try to find a key in the keycharmapping dict but whatever
			public static readonly HashSet<Key> CharacterKeys =
				Alphabetical
				.Concat(Numerical)
				.Concat(NumericalNumpad)
				.Concat(Symbolic)
				.Concat(new Key[] { Key.Space })
				.ToHashSet();

			public static readonly Dictionary<Key, char> KeyCharMapping = new() {
				{ Key.Backquote,	'`' },
				{ Key.Digit1,		'1' },
				{ Key.Digit2,		'2' },
				{ Key.Digit3,		'3' },
				{ Key.Digit4,		'4' },
				{ Key.Digit5,		'5' },
				{ Key.Digit6,		'6' },
				{ Key.Digit7,		'7' },
				{ Key.Digit8,		'8' },
				{ Key.Digit9,		'9' },
				{ Key.Digit0,		'0' },
				{ Key.Minus,		'-' },
				{ Key.Equals,		'=' },
				{ Key.Q,			'q' },
				{ Key.W,			'w' },
				{ Key.E,			'e' },
				{ Key.R,			'r' },
				{ Key.T,			't' },
				{ Key.Y,			'y' },
				{ Key.U,			'u' },
				{ Key.I,			'i' },
				{ Key.O,			'o' },
				{ Key.P,			'p' },
				{ Key.LeftBracket,	'[' },
				{ Key.RightBracket,	']' },
				{ Key.Backslash,	'\\' },
				{ Key.A,			'a' },
				{ Key.S,			's' },
				{ Key.D,			'd' },
				{ Key.F,			'f' },
				{ Key.G,			'g' },
				{ Key.H,			'h' },
				{ Key.J,			'j' },
				{ Key.K,			'k' },
				{ Key.L,			'l' },
				{ Key.Semicolon,	';' },
				{ Key.Quote,		'\'' },
				{ Key.Z,			'z' },
				{ Key.X,			'x' },
				{ Key.C,			'c' },
				{ Key.V,			'v' },
				{ Key.B,			'b' },
				{ Key.N,			'n' },
				{ Key.M,			'm' },
				{ Key.Comma,		',' },
				{ Key.Period,		'.' },
				{ Key.Slash,		'/' },
				{ Key.Space,		' ' },

				{ Key.Numpad1,		'1'},
				{ Key.Numpad2,		'2'},
				{ Key.Numpad3,		'3'},
				{ Key.Numpad4,		'4'},
				{ Key.Numpad5,		'5'},
				{ Key.Numpad6,		'6'},
				{ Key.Numpad7,		'7'},
				{ Key.Numpad8,		'8'},
				{ Key.Numpad9,		'9'},
				{ Key.Numpad0,		'0'}
			};

			public static readonly Dictionary<Key, char> KeyShiftedMapping = new() {
				{ Key.Backquote,	'~' },
				{ Key.Digit1,		'!' },
				{ Key.Digit2,		'@' },
				{ Key.Digit3,		'#' },
				{ Key.Digit4,		'$' },
				{ Key.Digit5,		'%' },
				{ Key.Digit6,		'^' },
				{ Key.Digit7,		'&' },
				{ Key.Digit8,		'*' },
				{ Key.Digit9,		'(' },
				{ Key.Digit0,		')' },
				{ Key.Minus,		'_' },
				{ Key.Equals,		'+' },
				{ Key.Q,			'Q' },
				{ Key.W,			'W' },
				{ Key.E,			'E' },
				{ Key.R,			'R' },
				{ Key.T,			'T' },
				{ Key.Y,			'Y' },
				{ Key.U,			'U' },
				{ Key.I,			'I' },
				{ Key.O,			'O' },
				{ Key.P,			'P' },
				{ Key.LeftBracket,	'{' },
				{ Key.RightBracket,	'}' },
				{ Key.Backslash,	'|' },
				{ Key.A,			'A' },
				{ Key.S,			'S' },
				{ Key.D,			'D' },
				{ Key.F,			'F' },
				{ Key.G,			'G' },
				{ Key.H,			'H' },
				{ Key.J,			'J' },
				{ Key.K,			'K' },
				{ Key.L,			'L' },
				{ Key.Semicolon,	':' },
				{ Key.Quote,		'"' },
				{ Key.Z,			'Z' },
				{ Key.X,			'X' },
				{ Key.C,			'C' },
				{ Key.V,			'V' },
				{ Key.B,			'B' },
				{ Key.N,			'N' },
				{ Key.M,			'M' },
				{ Key.Comma,		'<' },
				{ Key.Period,		'>' },
				{ Key.Slash,		'?' },
				{ Key.Space,		' ' },

				{ Key.Numpad1,		'1'}, // yeah this works ig idk
				{ Key.Numpad2,		'2'},
				{ Key.Numpad3,		'3'},
				{ Key.Numpad4,		'4'},
				{ Key.Numpad5,		'5'},
				{ Key.Numpad6,		'6'},
				{ Key.Numpad7,		'7'},
				{ Key.Numpad8,		'8'},
				{ Key.Numpad9,		'9'},
				{ Key.Numpad0,		'0'}
			};

/* dont have to solve this problem
			public enum Keys {
				Escape,
				F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
				Delete,
				Backquote,
				D1, D2, D3, D4, D5, D6, D7, D8, D9, D0,
				Minus, 
				Equals, 
				Backspace,
				A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
				Tab,
				CapsLock,
				Leftbracket, Rightbracket,
				Backslash,
				Semicolon,
				Quote,
				Enter,
				Comma,
				Period,
				Slash,
				Shift, Alt, Ctrl, Meta,
				Contextmenu,
				Space,

				LeftArrow, RightArrow, UpArrow, DownArrow,
				
				PrintScreen,
				ScrollLock,
				Pause,

				Insert,
				Home,
				PageUp, 
				End,
				PageDown, 

				NumLock,
				NEnter, NDivide, NMultiply, NPlus, NMinus, NPeriod, NEquals,
				N0, N1, N2, N3, N4, N5, N6, N7, N8, N9,
			}
*/
		}
	}
}