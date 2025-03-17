using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Interpreter : Part {
	public Evaluator Evaluator;
	public CableConnection MemoryCC;

	// always add a null check manually in the script, data can't hold parts so no error returningn in here
	public Memory GetMemory() {
		if (MemoryCC == null)
			return null;

		if (MemoryCC.Cable.OtherCC(MemoryCC).Part is MemoryPart part)
			return part.component;

		return null;
	}
	public Data TryGetMemory(out Memory memory) {
		memory = GetMemory();

		return memory == null ?
			Errors.MissingOrInvalidConnection("Interpreter", "Memory") :
			Data.Success;
	}

	// need memory so run call can have a copy of it
	public Data RunFunction(
		Memory memory, 
		Primitive.Function function, 
		Data thisReference, 
		List<Data> args) {
		// handle internal functions
		if (function.IsInternalFunction)
			return function.InternalFunction.Invoke(thisReference, args);

		// argument check
		if (function.Parameters.Count != args.Count)
			return Errors.InvalidArgumentCount(function.Name, function.Parameters.Count, args.Count);

		// set args in a copy of memory
		if (memory == null) return Errors.MissingOrInvalidConnection("Memory", "Interpreter");

		Memory memoryCopy = memory.Copy();
		for (int a = 0; a < args.Count; a++) {
			memoryCopy.Set(function.Parameters[a].Value, args[a]);
		}

		// set this
		memoryCopy.Set("this", thisReference);

		// run the script with the memory copy
		Data output = Run(memoryCopy, function.Script);

		return output;
	}

	public Data Run(Memory memory, Script script) {
		return RunSection(memory, script.Contents);
	}

	struct InternalState {
		public bool ExpectingSection;
		public bool SkipNext;
		public bool ExpectingElse;
		public bool IfSuccceded; // if succeeded

		public bool ForLoopNext;
		public Token.Reference LoopOver;

		public bool WhileLoopNext;
	}

	bool CheckFlag(Flags flags, Flags check)
		=> (flags & check) != 0;

	private Data RunSection(Memory memory, Section section) {
		List<Line> lines = section.Lines;
		InternalState state = new();

		int i = 0;
		while (i < lines.Count) {
			Line line = lines[i];
			//Debug.Log($"running {line}");
			
			#region prechecks
			// line type check
			if (line.LineType == Line.LineTypeEnum.Line && state.ExpectingSection)
				return Errors.Expected("indented section");
			if (line.LineType == Line.LineTypeEnum.Section && !state.ExpectingSection)
				return Errors.Unexpected("indented section");

			// skip check
			if (state.SkipNext) {
				state.SkipNext = false;
				state.ExpectingSection = false;
				i++;
				continue;
			}
			#endregion

			if (line.LineType == Line.LineTypeEnum.Line) {

				Line lineCopy = line.DeepCopy();
				Data output = Evaluator.Evaluate(lineCopy, false);
				if (output is Error) return output;
				Flags nFlags = output.Flags;

				#region checks
				if (!state.ExpectingElse && // not expecting else
					CheckFlag(nFlags, Flags.Else)) // is else
					return Errors.Unexpected("else statement");

				#endregion

				// TODO refactor down this fat block of ifs
				if (!CheckFlag(nFlags, Flags.Else))
					state.ExpectingElse = false;

				if (CheckFlag(nFlags, Flags.Else)) { // else
					state.ExpectingSection = true;
					if (state.IfSuccceded) // dont run contents if if succeded
						state.SkipNext = true;

					else { // only run if if failed
						if (!((nFlags & Flags.If) != 0)) { // normal else
							state.ExpectingElse = false;
						}
						else { // else if 
							state.ExpectingElse = true; // TODO: copied code from if, perhaps refactor into separate local method

							if (CheckFlag(nFlags, Flags.Success)) { // sucess
								state.SkipNext = false;
								state.IfSuccceded = true;
							}
							else if (CheckFlag(nFlags, Flags.Fail)) { // fail
								state.SkipNext = true;
								state.IfSuccceded = false;
							}
						}
					}
					// otherwise do nothing
				}
				else if (CheckFlag(nFlags, Flags.If)) { // if
					state.ExpectingElse = true;
					state.ExpectingSection = true;

					if (CheckFlag(nFlags, Flags.Success)) { // sucess
						state.SkipNext = false;
						state.IfSuccceded = true;
					}
					else if (CheckFlag(nFlags, Flags.Fail)) { // fail
						state.SkipNext = true;
						state.IfSuccceded = false;
					}
				}
				else if (CheckFlag(nFlags, Flags.For)) {
					state.ForLoopNext = true;
					state.ExpectingSection = true;
					state.LoopOver = lineCopy.Tokens[1] as Token.Reference;
				}
			}
			else { // run section contents
				state.ExpectingSection = false;

				if (!state.ForLoopNext) { // run once
					Data trySection = RunSection(memory, line.Section);
					if (trySection is Error) return trySection;
				}
				else { // run as for loop
					List<Data> values = (state.LoopOver.ThisReference as Primitive.List).Value;
					Token.Reference iterator = state.LoopOver.Copy(); // keep original ref to iterator

					// iterate over loopover
					foreach (Data item in values) {
						// set the iterator
						Data trySet = memory.Set(iterator, item);
						if (trySet is Error) return trySet;

						// run this section
						Data trySection = RunSection(memory, line.Section);
						if (trySection is Error) return trySection;
					}
				}
			}

			i++;
		}

		return Data.Success;
	}
}