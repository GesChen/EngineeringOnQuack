using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InternalInputType : T_Data {
	public InternalInputType() : base(InternalType) {

	}

	public static Type InternalType = new("Input", new Dictionary<string, T_Data>(){
		{ "keydown",		new Primitive.Function("keydown",		keydown) },
		{ "allkeysdown",	new Primitive.Function("allkeysdown",	allkeysdown) },
		{ "mousepos",		new Primitive.Function("mousepos",		mousepos) },
		{ "mousedelta",		new Primitive.Function("mousedelta",	mousedelta) },
		{ "mousedown",		new Primitive.Function("mousedown",		mousedown) },
		{ "mousescroll",	new Primitive.Function("mousescroll",	mousescroll) }
	});

	// so the funcs can know if or if not to give output
	public static Func<int> RequestCurrentlyOperatingID;

	public static T_Data keydown(T_Data _, List<T_Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("keydown", 1, args.Count);

		if (InternalFunctions.CurrentCaller.CreationID != RequestCurrentlyOperatingID())
			return new Primitive.Bool(false);

		string cast = (args[0].Cast(Primitive.String.InternalType) as Primitive.String).Value;

		var key = Conatrols.Keyboard.NameToKey(cast);
		if (key == Key.None) return Errors.BadArgument("keydown", 0, "Invalid key name");

		bool down = Conatrols.Keyboard.Pressed.Contains(key);
		return new Primitive.Bool(down);
	}
	
	public static T_Data allkeysdown(T_Data _, List<T_Data> args) {
		if (args.Count != 0) return Errors.InvalidArgumentCount("allkeysdown", 0, args.Count);

		if (InternalFunctions.CurrentCaller.CreationID != RequestCurrentlyOperatingID())
			return new Primitive.List();

		var keysdown = Conatrols.Keyboard.Pressed;
		string[] keynames = keysdown.Select(k => Conatrols.Keyboard.KeyToName(k)).ToArray();
		List<T_Data> datas = keynames.Select(kn => (T_Data)new Primitive.String(kn)).ToList();

		return new Primitive.List(datas);
	}

	// returns a list for now 
	public static T_Data mousepos(T_Data _, List<T_Data> args) {
		if (args.Count != 0) return Errors.InvalidArgumentCount("mousepos", 0, args.Count);

		if (InternalFunctions.CurrentCaller.CreationID != RequestCurrentlyOperatingID())
			return new Primitive.List(new List<T_Data> { new Primitive.Number(0), new Primitive.Number(0) });

		var pos = Conatrols.Mouse.Position;

		return new Primitive.List(new List<T_Data>() 
			{ new Primitive.Number(pos.x), new Primitive.Number(pos.y) });
	}

	public static T_Data mousedelta(T_Data _, List<T_Data> args) {
		if (args.Count != 0) return Errors.InvalidArgumentCount("mousedelta", 0, args.Count);

		if (InternalFunctions.CurrentCaller.CreationID != RequestCurrentlyOperatingID())
			return new Primitive.List(new List<T_Data> { new Primitive.Number(0), new Primitive.Number(0) });

		var delta = Conatrols.Mouse.Delta;

		return new Primitive.List(new List<T_Data>
			{ new Primitive.Number(delta.x), new Primitive.Number(delta.y) });
	}

	public static T_Data mousedown(T_Data _, List<T_Data> args) {
		if (args.Count != 1) return Errors.InvalidArgumentCount("mousedown", 1, args.Count);

		if (InternalFunctions.CurrentCaller.CreationID != RequestCurrentlyOperatingID())
			return new Primitive.Bool(false);

		int button;

		var tryCastString = args[0].Cast(Primitive.String.InternalType);
		var tryCastNumber = args[0].Cast(Primitive.Number.InternalType);
		if (tryCastString is Primitive.String str) {
			button = str.Value.ToLowerInvariant() switch {
				"left" => 0,
				"right" => 1,
				"middle" => 2,
				"back" => 3,
				"forward" => 4,
				_ => -1
			};

			if (button == -1) return Errors.BadArgument("mousedown", 0, $"\"{str}\" is not a valid mouse button");
		} else 
		if (tryCastNumber is Primitive.Number number) {
			double val = number.Value;

			// is int and in range
			if (val != Math.Floor(val) || val < 0 || val > 4)
				return Errors.BadArgument("mousedown", 0, $"\"{val}\" is not a valid mouse button");

			button = (int)val;
		} else {
			return Errors.BadArgument("mousedown", 0, "Invalid mouse button");
		}

		bool pressed = button switch {
			0 => Conatrols.Mouse.Left.Pressed,
			1 => Conatrols.Mouse.Right.Pressed,
			2 => Conatrols.Mouse.Middle.Pressed,
			3 => Conatrols.Mouse.Back.Pressed,
			4 => Conatrols.Mouse.Forward.Pressed,
			_ => false
		};

		return new Primitive.Bool(pressed);
	}

	public static T_Data mousescroll(T_Data _, List<T_Data> args) {
		if (args.Count != 0) return Errors.InvalidArgumentCount("mousescroll", 0, args.Count);

		if (InternalFunctions.CurrentCaller.CreationID != RequestCurrentlyOperatingID())
			return new Primitive.List(new List<T_Data> { new Primitive.Number(0), new Primitive.Number(0) });

		var scroll = Conatrols.Mouse.Scroll;

		return new Primitive.List(new List<T_Data>()
			{ new Primitive.Number(scroll.x), new Primitive.Number(scroll.y) });
	}
}