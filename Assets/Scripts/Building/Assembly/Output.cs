using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SimulatingMainUI.TopBar.Outputs;

// a single output channel. not sure what to put in here
// but its at this point just a wrapper class
// for a name of an output
// more specific output handling and ui code later to do i guess
public class Output {
	public string Name;
	public bool Visible; // might rename to enabled but visible is more clear

	private OutputWindow Window;

	public void SetWindow(OutputWindow window) {
		Window = window;
	}

	public void Print(string message) {
		// handle formatting in here
		float t = Time.time; // Time since start in seconds

		int minutes = (int)(t / 60);
		int seconds = (int)(t % 60);
		int milliseconds = (int)((t * 1000) % 1000);

		string timestamp = string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);

		message = $"[{timestamp}] {message}";

		Window.AddLine(message);
	}
}