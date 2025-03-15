using System;
using System.Linq;
using System.Collections.Generic;

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
	public Data RunFunction(Memory memory, Primitive.Function function, Data thisReference, List<Data> args) {
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
		public List<Data> LoopOver;

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
				Evaluator.Output output = Evaluator.Evaluate(line);
				if (output.data is Error) return output.data;
				Flags nFlags = output.flags;

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
				
			}
			else { // run section contents
				state.ExpectingSection = false;

				Data trySection = RunSection(memory, line.Section);
				if (trySection is Error) return trySection;
			}

				//// section/line check before DRNL
				//// check if section token and line contents match
				//if ((pFlags & Flags.ExpectSectionNext) != 0)	  // expecting section
				//	if (line.LineType == Line.LineTypeEnum.Line) // but got line
				//		return Errors.Expected("indented section");
				//else												 // expecting line
				//	if (line.LineType == Line.LineTypeEnum.Section) // but got section
				//		return Errors.Unexpected("indented section");

				//// skip this line and clear the DRNL flag
				//if ((pFlags & Flags.DontRunNextLine) != 0) {
				//	pFlags &= ~Flags.DontRunNextLine; // unset flag
				//	i++;
				//	continue;
				//}

				//if (line.LineType == Line.LineTypeEnum.Line) {
				//	Evaluator.Output evaluateOut = Evaluator.Evaluate(line);
				//	if (evaluateOut.data is Error) return evaluateOut.data;
				//	Flags nFlags = evaluateOut.flags;

				//	if ((nFlags & Flags.ReturnData) != 0)
				//		return evaluateOut.data;

				//	// make sure this else was expected
				//	if ((nFlags & Flags.LineWasElse) != 0) {
				//		// must have had previous if statement otherwise unexpected
				//		if (!((pFlags & Flags.IfSucceeded) != 0 ||
				//			(pFlags & Flags.IfFailed) != 0))
				//			return Errors.Unexpected("else statement");
				//	}

				//	/*if ((flags & Flags.EnterFor) != 0) {
				//		Primitive.List L = evaluateOut.data as Primitive.List;
				//		Primitive.String S = evaluateOut.data as Primitive.String;

				//		if (!(L != null || S != null))
				//			return Errors.CannotUseTypeWithFeature(evaluateOut.data.Type.Name, "for loops");

				//		List<Data> iterateOver = L != null ? L.Value : // turn string into char list
				//			S.Value.Select(c => new Primitive.String(c.ToString()) as Data).ToList();

				//		foreach (Data d in iterateOver) {

				//		}
				//	}*/

				//	// this might not be the right approach cuz some flags need to persist
				//	pFlags = evaluateOut.flags;
				//}
				//else { // section

				//	Data trySection = RunSection(memory, line.Section);
				//	if (trySection is Error) return trySection;
				//}

				i++;
		}

		return Data.Success;
	}
}