using System;
using System.IO;

public static partial class Config {
	public static readonly int FPS_LIMIT = 120;
	public static readonly int MAX_RECURSION_DEPTH = 128;
	public static readonly string tempfilesaveloc = "C:\\";
	

	public static class Locations {
		public static readonly string IconsFolder		= "Icons/";
		public static readonly string MaterialsFolder	= "Materials/";
		
		public static readonly string PartsFolder		= "Parts/";
		public static readonly string BasePartsFolder		= PartsFolder + "Base/";
		public static readonly string ProcessingPartsFolder	= PartsFolder + "Processing/";
		public static readonly string TemplatePartsFolder	= PartsFolder + "Templates/";

		public static string SavingLocation =>
			HF.GuaranteePath(
				Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"EOQ")
				);
		public static string AssembliesLocation =>
			HF.GuaranteePath(
				Path.Combine(SavingLocation, "Assemblies")
				);
	}
}