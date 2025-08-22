public class Script {
	public string Name;
	public Section Contents;
	public string OriginalText;
	public string Version;

	public Script(string name, Section contents, string originalText, string version) {
		Name = name;
		Contents = contents;
		OriginalText = originalText;
		Version = version;
	}
	public Script(Section contents, string originalText) {
		Name = "";
		Contents = contents;
		OriginalText = originalText;
	}
	public Script() {
		Name = "";
		Contents = new();
		OriginalText = "";
		Version = Config.Language.VERSION;
	}

	public override string ToString() {
		return $"Script \"{Name}\": \n{Contents}";
	}
}