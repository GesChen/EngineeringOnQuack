using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Interpreter {
	public Evaluator Evaluator;
	public Memory Memory;


	// need memory so run call can have a copy of it
	public Data RunFunction(
		Memory memory,
		Primitive.Function function,
		Data thisReference,
		List<Data> args,
		int depth = 0) {

		if (LanguageConfig.DEBUG) HF.WarnColor($"Running function {function.Name}", Color.yellow);

		// handle different function types
		switch (function.FunctionType) {
			case Primitive.Function.FunctionTypeEnum.Internal:
				return function.InternalFunction.Invoke(thisReference, args);

			case Primitive.Function.FunctionTypeEnum.Constructor:
				if (LanguageConfig.DEBUG) HF.WarnColor($"Constructing {function.TypeFor.Name}", Color.yellow);

				Data newObject = new(function.TypeFor) {
					Memory = new(memory.Interpreter, "object memory")
				};
				Memory originalUse = Data.currentUseMemory;
				Data.currentUseMemory = newObject.Memory;

				// DONT INFINITE RECURSE!!!
				function.FunctionType = Primitive.Function.FunctionTypeEnum.UserDefined;

				Data runConstructor = RunFunction(
					newObject.Memory,
					function,
					newObject,
					args,
					depth + 1);
				function.FunctionType = Primitive.Function.FunctionTypeEnum.Constructor;
				Data.currentUseMemory = originalUse;

				if (runConstructor is Error)
					return runConstructor;

				if (LanguageConfig.DEBUG) HF.WarnColor($"Constructed new {function.TypeFor.Name} object: {newObject}", Color.yellow);

				return newObject;
		}
		// argument check
		if (function.Parameters.Length != args.Count)
			return Errors.InvalidArgumentCount(function.Name, function.Parameters.Length, args.Count);

		// set args in a copy of memory
		if (memory == null) return Errors.MissingOrInvalidConnection("Memory", "Interpreter");

		Data trySet;
		Memory memoryCopy = memory.Copy();

		for (int a = 0; a < args.Count; a++) {
			trySet = memoryCopy.Set(function.Parameters[a], args[a]);
			if (trySet is Error) return trySet;
		}

		// set this
		trySet = memoryCopy.Set("this", thisReference);
		if (trySet is Error) return trySet;

		// run the script with the memory copy
		Data output = RunSection(memoryCopy, function.Script, depth + 1); // increase depth on function call

		return output;
	}

	public Data Run(Memory memory, Script script, int depth = 0) {
		if (script == null)
			return Errors.NoScriptLoaded();

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

		public string NewName;

		public bool MakeFunction;
		public string[] NewFuncParams;

		public bool MakeClass;
	}

	bool CheckFlag(Flags flags, Flags check)
		=> (flags & check) != 0;

	private Data RunSection(Memory memory, Section section, int depth) {
		if (depth > LanguageConfig.RecursionDepthLimit) // check recursion depth
			return Errors.RecursionLimitReached();

		if (LanguageConfig.DEBUG) HF.WarnColor($"--Running {section}", Color.yellow);

		Line[] lines = section.Lines;
		InternalState state = new();

		int i = 0;
		while (i < lines.Length) {
			Line line = lines[i];

			if (LanguageConfig.DEBUG) HF.WarnColor($"{depth}.{i} running {line} ", Color.cyan);

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
				Data output = Evaluator.Evaluate(lineCopy, memory, false, depth);
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
					continue; // do nothing
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

				else if (CheckFlag(nFlags, Flags.MakeFunction)) {
					List<Data> formattedOutput = (output as Primitive.List).Value;

					string name = (formattedOutput[0] as Primitive.String).Value;

					string[] newparams =
						(formattedOutput[1] as Primitive.List).Value
						.Select(d => (d as Primitive.String).Value)
						.ToArray();

					state.ExpectingSection = true;
					state.MakeFunction = true;
					state.NewName = name;
					state.NewFuncParams = newparams;
				}
				else if (CheckFlag(nFlags, Flags.MakeInline)) {
					List<Data> formattedOutput = (output as Primitive.List).Value;
					
					string name = (formattedOutput[0] as Primitive.String).Value;

					string[] newparams =
						(formattedOutput[1] as Primitive.List).Value
						.Select(d => (d as Primitive.String).Value)
						.ToArray();

					int functionDefStartIndex = (int)(formattedOutput[2] as Primitive.Number).Value;
					Token[] functionDef = line.Tokens.Skip(functionDefStartIndex).ToArray();

					memory.Set(name, new Primitive.Function(name, newparams, functionDef));
				}
				else if (CheckFlag(nFlags, Flags.MakeClass)) {
					string name = (output as Primitive.String).Value;

					state.ExpectingSection = true;
					state.MakeClass = true;
					state.NewName = name;
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
						Data trySection = RunSection(memory, line.Section, depth);
						if (trySection is Error) return trySection;
						Flags sFlags = trySection.Flags;

						if (CheckFlag(sFlags, Flags.Break)) {
							break;
						}
						else if (CheckFlag(sFlags, Flags.Return)) {
							return trySection; // propogates return flag
						}
					}

					state.ForLoopNext = false;
				}
				else if (state.MakeFunction) {
					Primitive.Function newFunction = new(state.NewName, state.NewFuncParams, line.Section);
					Data trySet = memory.Set(state.NewName, newFunction);
					if (trySet is Error) return trySet;

					state.MakeFunction = false;
				}
				else if (state.MakeClass) {
					if (LanguageConfig.DEBUG) HF.WarnColor($"Constructing class {state.NewName}", Color.yellow);

					Memory originallyUsing = Data.currentUseMemory;
					Memory classMemory = new (memory.Interpreter, "class init memory");
					Data.currentUseMemory = classMemory;

					Data trySection = RunSection(classMemory, line.Section, depth);

					Type newType = new(state.NewName, classMemory);
					
					// check if constructor was defined when it ran
					if (classMemory.Get(state.NewName) is Primitive.Function tryGetConstructor) {
						Primitive.Function constructor = new(
							state.NewName,
							tryGetConstructor.Parameters,
							tryGetConstructor.Script,
							newType);
						memory.Set(state.NewName, constructor); // save it 
					}

					Data makeNewType = memory.NewType(newType);
					if (LanguageConfig.DEBUG) HF.WarnColor($"Made new class {state.NewName} with memory\n{classMemory.MemoryDump()}", Color.yellow);

					Data.currentUseMemory = originallyUsing;

					state.MakeClass = false;
				}
				else { // run once
					Data trySection = RunSection(memory, line.Section, depth);
					if (trySection is Error) return trySection;

					if (CheckFlag(trySection.Flags, Flags.Break)) {
						if (state.ReturnToWhileStatement) // break if at loop level
							state.ReturnToWhileStatement = false;
						else // propogate if not at loop level
							return trySection;
					}

					if (CheckFlag(trySection.Flags, Flags.Continue)) { // propogate continue until original loop catches
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