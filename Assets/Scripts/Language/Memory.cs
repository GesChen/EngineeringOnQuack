using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Memory
{
	public Dictionary<string, Data> Data;
	public Dictionary<string, Type> Types;
	public Section Script;

	public Memory(Dictionary<string, Data> data, Dictionary<string, Type> types, Section script)
	{
		Data = data;
		Types = types;
		Script = script;
	}
	public Memory()
	{
		Data = new();
		Types = new();
		Script = null;
	}

	public Memory Copy()
	{
		return new(
			new Dictionary<string, Data>(Data),
			new Dictionary<string, Type>(Types),
			new(Script.Lines)
			);
	}



	//public Token.Data Get(List<Token> location)
	//{

	//}

	public void Set(List<Token> location, Data data)
	{

	}
}
