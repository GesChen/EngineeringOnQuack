using System;
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

	private Data RunSection(Memory memory, Section section) {
		List<Line> lines = section.Lines;

		int flags = 0;
		int i = 0;
		while (i < lines.Count) {
			Line line = lines[i];

			if (flags & Flags.ExpectSectionNext != 0 &&
				line.LineType != Line.LineTypeEnum.Section)
				return Errors.Expected("Section", "");

			if (flags & Flags.DontRunNextLine != 0) {
				// unset flag
				continue;
			}

			if (line.LineType == Line.LineTypeEnum.Line) {

				Evaluator.Output evaluateOut = Evaluator.Evaluate(flags, line);
				if (evaluateOut.data is Error) return evaluateOut.data;

				if (flags & Flags.ReturnData != 0)
					return evaluateOut.data;
			}
			else {
				Data trySection = RunSection(memory, line.Section);
				if (trySection is Error) return trySection;
			}

			i++;
		}

		return Data.Success;
	}
}