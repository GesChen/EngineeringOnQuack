using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class ScriptSaveLoad : MonoBehaviour
{

	// commented out ones are ones that
	// tokenizer likely wont init
	public struct sData {
		public int PrimitiveType; // 0-number, 1-string

		public double NumberValue;
		public string StringValue;
		//public sData[] ListValue;
		//public Dictionary<sData, sData> DictValue;
	}
	public struct sReference {
		public bool Exists; // ?
		//public bool IsInstanceVariable;
		//public bool IsListItem;

		public string Name;
		public sData ThisReference;
		//public sData ParentReference;
		//public int ListIndex;
	}
	public struct sToken {
		/* 0 - operator
		 * 1 - name
		 * 2 - keyword
		 * 3 - reference
		 */
		public int Type;
		// TODO: figure out a better system for this idfk
		public Token.Operator.Ops OperatorValue;
		public string NameValue;
		public Token.Keyword.Kws KeywordValue;
		public sReference ReferenceValue;
	}
	public struct sLine {
		public int RealLineNumber;
		public string OriginalString; 
		
		public int Type; // 0-normal, 1-subsection
		public sToken[] Tokens;
		public sSection Section;
	}
	public struct sSection {
		public sLine[] Lines;
	}
	public struct sScript {
		public string Name;
		public sSection Contents;
		public string OriginalText;
	}

	public static string 

	public static sScript ConvertScriptToStruct(Script original) {
		sSection structSection = SectionToStruct(original.Contents);

		sScript structScript = new() {
			Name = original.Name,
			Contents = structSection,
			OriginalText = original.OriginalText
		};

		return structScript;
	}

	private static sSection SectionToStruct(Section original) {
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
						newToken.OperatorValue = op.Value;
					}
					else if (oToken is Token.Name n) {
						newToken.Type = 1;
						newToken.NameValue = n.Value;
					}
					else if (oToken is Token.Keyword kw) {
						newToken.Type = 2;
						newToken.KeywordValue = kw.Value;
					}
					else if (oToken is Token.Reference r) {
						sData newData = new();
						if (r.ThisReference is Primitive.Number num) {
							newData.PrimitiveType = 0;
							newData.NumberValue = num.Value;
						}
						else if (r.ThisReference is Primitive.String str) {
							newData.PrimitiveType = 1;
							newData.StringValue = str.Value;
						};

						newToken.Type = 3;
						newToken.ReferenceValue = new() {
							Exists = r.Exists,
							Name = r.Name,
							ThisReference = newData
						};
					}

					tokens.Add(newToken);
				}

				newLine.Type = 0;
				newLine.RealLineNumber = oLine.RealLineNumber;
				newLine.OriginalString = oLine.OriginalString;
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
}