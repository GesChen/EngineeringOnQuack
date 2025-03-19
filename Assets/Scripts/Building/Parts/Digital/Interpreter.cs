//#define DEBUGMODE

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.U2D;

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
		List<Data> args,
		int depth = 0) {
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
		Data output = Run(memoryCopy, function.Script, depth + 1); // increase depth on function call

		return output;
	}

	public Data Run(Memory memory, Script script, int depth = 0) {
		if (script == null)
			return Errors.NoScriptLoaded();

		if (depth > LanguageConfig.RecursionDepthLimit) // check recursion depth
			return Errors.RecursionLimitReached();
		
		return RunSection(memory, script.Contents, depth);
	}

	class InternalState {
		public bool ExpectingSection;
		public bool SkipNext;
		public bool ExpectingElse;
		public bool IfSuccceded; // if succeeded

		public bool ForLoopNext;
		public Token.Reference LoopOver;

		public bool ReturnToWhileStatement;
		public int WhileLoops;

		public bool TryNext;
		public bool TryErrored;
	}

	bool CheckFlag(Flags flags, Flags check)
		=> (flags & check) != 0;

	private Data RunSection(Memory memory, Section section, int depth = 0) {
		List<Line> lines = section.Lines;
		InternalState state = new();

		int i = 0;
		while (i < lines.Count) {
			Line line = lines[i];

			if (LanguageConfig.DEBUG) Debug.Log($"running {line} {i}");

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
				Data output = Evaluator.Evaluate(lineCopy, false, depth);
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
						if (!CheckFlag(nFlags, Flags.If)) { // normal else
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
				else if (CheckFlag(nFlags, Flags.While)) {
					if (state.WhileLoops > LanguageConfig.MaxWhileLoopIters)
						return Errors.WhileLoopLimitReached();

					state.ExpectingSection = true;

					if (!(lineCopy.Tokens[1] is Token.Reference condition &&
						condition.ThisReference is Primitive.Bool b))
						return Errors.BadSyntaxFor("while loop");

					state.SkipNext = !b.Value;
					state.ReturnToWhileStatement = b.Value;

					if (!b.Value) // reset at the last 
						state.WhileLoops = 0;
				}
				else if (CheckFlag(nFlags, Flags.Break)) {
					return output; // has break flag
				}
				else if (CheckFlag(nFlags, Flags.Continue)) {
					return output; // has continue flag
				}
				else if (CheckFlag(nFlags, Flags.Pass)) {
					continue; // do noything
				}
				else if (CheckFlag(nFlags, Flags.Return)) {
					return output; // has return flag
				}
				else if (CheckFlag(nFlags, Flags.Try)) {
					state.ExpectingSection = true;
					state.TryNext = true;
				}
				else if (CheckFlag(nFlags, Flags.Except)) {
					state.ExpectingSection = true;
					if (state.TryErrored) {
						state.SkipNext = false;
					}
					else {
						state.SkipNext = true; // skip if try didnt error
					}
				}
				else if (CheckFlag(nFlags, Flags.Finally)) {
					state.ExpectingSection = true;
				}
			}
			else { // run section contents
				state.ExpectingSection = false;

				if (state.ForLoopNext) {// run as for loop
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
						Flags sFlags = trySection.Flags;

						if (CheckFlag(sFlags, Flags.Break)) {
							break;
						}
						else if (CheckFlag(sFlags, Flags.Return)) {
							return trySection; // propogates return flag
						}
					}
				}
				else {  // run once
					Data trySection = RunSection(memory, line.Section);
					if (trySection is Error) return trySection;

					if (CheckFlag(trySection.Flags, Flags.Break)) {
						if (state.ReturnToWhileStatement) // break if at loop level
							state.ReturnToWhileStatement = false;
						else // propogate if not at loop level
							return trySection;
					}

					if (state.ReturnToWhileStatement) {
						state.WhileLoops++; // prevent inf loops
						i -= 2;
					}
				}
			}

			i++;
		}

		return Data.Success;
	}
}