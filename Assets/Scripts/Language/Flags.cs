using System;

[Flags]
public enum Flags {
	None			= 1 << 0,
	Success			= None			<< 1,
	Fail			= Success		<< 1,
	If				= Fail			<< 1,
	Else			= If			<< 1,
	For				= Else			<< 1,
	While			= For			<< 1,
	Break			= While			<< 1,
	Continue		= Break			<< 1,
	Pass			= Continue		<< 1,
	Return			= Pass			<< 1,
	Try				= Return		<< 1,
	Except			= Try			<< 1,
	Finally			= Except		<< 1,
	Raise			= Finally		<< 1,

	MakeFunction	= Raise			<< 1,
	MakeInline		= MakeFunction	<< 1,
	MakeClass		= MakeInline	<< 1,
}