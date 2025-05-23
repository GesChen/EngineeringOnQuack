using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class ScriptSaveLoad : MonoBehaviour
{

	// commented out ones are ones that tokenizer likely wont init
	/*public class sData {

	}*/
	public class sReference {

/*		[JsonProperty("E")] public bool Exists; // ?
		public bool IsInstanceVariable;
		public bool IsListItem;

		[JsonProperty("N")] public string Name;
		[JsonProperty("TR")] public sData ThisReference;
*/
		// ----- ThisReference Section (Data representation, no other values, just put it in here --------
		[JsonProperty("PT")] public int PrimitiveType; // 0-number, 1-string

		[JsonProperty("NV")] public double NumberValue;
		[JsonProperty("SV")] public string StringValue;
		//public sData[] ListValue;
		//public Dictionary<sData, sData> DictValue;

		public bool ShouldSerializeNumberValue() => PrimitiveType == 0;
		public bool ShouldSerializeStringValue() => PrimitiveType == 1;

		// ----------------------------------------------------------------------------------------------
/*
		public sData ParentReference;
		public int ListIndex;
*/
	}
	public class sToken {
		/* 0 - operator
		 * 1 - name
		 * 2 - keyword
		 * 3 - reference
		 */
		[JsonProperty("T")] public int Type;
		[JsonProperty("SV")] public string		StringValue; // for op, name, token
		[JsonProperty("RV")] public sReference	ReferenceValue;

		public bool ShouldSerializeStringValue()	=> Type == 0 || Type == 1 || Type == 2;
		public bool ShouldSerializeReferenceValue()	=> Type == 3;
	}
	public class sLine {
		[JsonProperty("RLN")] public int RealLineNumber;
		[JsonProperty("OS")] public string OriginalString; 
		
		[JsonProperty("T")] public int Type; // 0-normal, 1-subsection
		[JsonProperty("L")] public sToken[] Tokens;
		[JsonProperty("S")] public sSection Section;

		public bool ShouldSerializeTokens()		=> Type == 0;
		public bool ShouldSerializeSection()	=> Type == 1;
	}
	public class sSection {
		public sLine[] Lines;
	}
	public class sScript {
		public string Name;
		public string OriginalText;
		public string Version;

		public sSection Contents;
	}

	#region Serialize
	public static string ConvertScriptToString(Script script) {
		string json = ConvertScriptToJson(script, false);
		string zipped = CompressionUtil.GetGzippedBase64(json);
		return zipped;
	}

	public static string ConvertScriptToJson(Script script, bool indent) {
		sScript structFormatted = ConvertScriptToStruct(script);

		string jsonified = JsonConvert.SerializeObject(structFormatted, 
			indent ? Formatting.Indented : Formatting.None);
		return jsonified;
	}

	// consider making these private
	public static sScript ConvertScriptToStruct(Script original) {
		sSection structSection = SectionToStruct(original.Contents);
		
		sScript structScript = new() {
			Name = original.Name,
			Contents = structSection,
			OriginalText = original.OriginalText,
			Version = Config.Language.VERSION
		};

		return structScript;
	}

	public static sSection SectionToStruct(Section original) {
		List<sLine> lines = new();

		foreach(Line oLine in original.Lines) {
			sLine newLine = new() {
				RealLineNumber = oLine.RealLineNumber,
				OriginalString = oLine.OriginalString
			};

			if (oLine.LineType == Line.LineTypeEnum.Line) {
				List<sToken> tokens = new();
				
				foreach (Token oToken in oLine.Tokens) {
					sToken newToken = new();
					if (oToken is Token.Operator op) {
						newToken.Type = 0;
						newToken.StringValue = op.StringValue;
					}
					else if (oToken is Token.Name n) {
						newToken.Type = 1;
						newToken.StringValue = n.Value;
					}
					else if (oToken is Token.Keyword kw) {
						newToken.Type = 2;
						newToken.StringValue = kw.StringValue;
					}
					else if (oToken is Data d) {
						sReference newRef = new();
						if (d is Primitive.Number num) {
							newRef.PrimitiveType = 0;
							newRef.NumberValue = num.Value;
						}
						else if (d is Primitive.String str) {
							newRef.PrimitiveType = 1;
							newRef.StringValue = str.Value;
						};

						newToken.Type = 3;
						newToken.ReferenceValue = newRef;
					}

					tokens.Add(newToken);
				}

				newLine.Type = 0;
				newLine.Tokens = tokens.ToArray();
			}
			else {
				sSection structSubSection = SectionToStruct(oLine.Section);

				newLine.Type = 1;
				newLine.Section = structSubSection;
			}

			lines.Add(newLine);
		}

		return new() {
			Lines = lines.ToArray()
		};
	}
	#endregion

	#region Deserialize

	public static Script ConvertStringToScript(string str) {
		string json = CompressionUtil.DecodeGzippedBase64(str);

		return ConvertJsonToScript(json);
	}

	public static Script ConvertJsonToScript(string json) {
		sScript deserialized = JsonConvert.DeserializeObject<sScript>(json);

		Script script = ConvertStructToScript(deserialized);

		return script;
	}

	public static Script ConvertStructToScript(sScript structed) {
		Section section = StructToSection(structed.Contents);

		Script script = new() {
			Name = structed.Name,
			Contents = section,
			OriginalText = structed.OriginalText,
			Version = structed.Version
		};
		
		return script;
	}

	public static Section StructToSection(sSection structed) {
		List<Line> lines = new();

		foreach (sLine sLine in structed.Lines) {
			Line oLine = new() {
				RealLineNumber = sLine.RealLineNumber,
				OriginalString = sLine.OriginalString
			};

			if (sLine.Type == 0) {
				List<Token> tokens = new();

				foreach (sToken sToken in sLine.Tokens) {
					Token oToken = sToken.Type switch {
						0 => new Token.Operator(sToken.StringValue),
						1 => new Token.Name(sToken.StringValue),
						2 => new Token.Keyword(sToken.StringValue),
						3 => sToken.ReferenceValue.PrimitiveType == 0 ?
								new Primitive.Number(sToken.ReferenceValue.NumberValue) :
								new Primitive.String(sToken.ReferenceValue.StringValue),
						_ => new Token()
					};

					tokens.Add(oToken);
				}

				oLine.LineType = Line.LineTypeEnum.Line;
				oLine.Tokens = tokens;
			}
			else {
				Section subSection = StructToSection(sLine.Section);

				oLine.LineType = Line.LineTypeEnum.Section;
				oLine.Section = subSection;
			}

			lines.Add(oLine);
		}

		return new(lines.ToArray());
	}

	#endregion

	#region Reconstruction (for fun)

	public static string ReconstructString(string str) {
		return ReconstructScript(ConvertStringToScript(str));
	}
	public static string ReconstructJson(string json) {
		return ReconstructScript(ConvertJsonToScript(json));
	}
	public static string ReconstructScript(Script script) {
		return ReconstructStruct(ConvertScriptToStruct(script));
	}
	public static string ReconstructStruct(sScript script) {
		List<string> reconstructedLines = ReconstructSection(script.Contents);

		return string.Join('\n', reconstructedLines);
	}
	public static List<string> ReconstructSection(sSection section) {
		List<string> strings = new();

		foreach (sLine sl in section.Lines) {
			if (sl.Type == 0) {
				string line = "";
				
				for (int s = 0; s < sl.Tokens.Length; s++) {
					sToken st = sl.Tokens[s];

					static bool space(string op) => op switch { "." or "(" or ")" or ":" => false, _ => true };

					// ive seen worse code
					bool addspace =
						(s != sl.Tokens.Length - 1 && sl.Tokens[s + 1].Type == 0 && space(sl.Tokens[s + 1].StringValue)) ||
						(st.Type == 0 && space(st.StringValue));

					line += st.Type switch {
						0 or 1 or 2 => st.StringValue,
						3 => st.ReferenceValue.PrimitiveType == 0 ?
							st.ReferenceValue.NumberValue.ToString() :
							'"' + st.ReferenceValue.StringValue + '"',
						_ => ""
					} + (addspace ? " " : "");
				}

				strings.Add(line);
			}
			else {
				List<string> subSection = ReconstructSection(sl.Section);

				// indent section
				subSection = subSection.Select(s => new string(' ', Config.Language.SpacesPerTab) + s).ToList();

				// add to end
				strings.AddRange(subSection);
			}
		}

		return strings;
	}

	#endregion
}