public class Script
{
	public string Name;
	public Section Contents;
	public string OriginalText;

	public Script(string name, Section contents, string originalText) {
		Name = name;
		Contents = contents;
		OriginalText = originalText;
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
	}

	public override string ToString() {
		return $"Script \"{Name}\": \n{Contents}";
	}
}