using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interpreter : MonoBehaviour
{
	public Memory Memory;

	//public Data RunFunction(Primitive.Function function, List<Data> args)
	//{
	//	if (function.IsInternalFunction)
	//		return function.InternalFunction.Invoke(args);

	//	Memory snapshot = Memory.Copy();


	//}

	public Data Run(Section script)
	{
		return new();
	}
}