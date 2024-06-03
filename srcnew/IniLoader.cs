using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MMXOnline;

public class IniParser {
	enum Token {
		String,
		Literal,
		Number,
		Header,
		Assigment,
		LineBreak,
	}

	public static Dictionary<string, object> Parse(string fileLocation) {
		string fileText = File.ReadAllText(fileLocation, Encoding.UTF8);
		(Token token, string value)[] data = TokenizeIni(fileText);

		var parsedIni = ParseTokens(data);
		return parsedIni;
	}

	static Dictionary<string, object> ParseTokens((Token token, string value)[] data) {
		Dictionary<string, object> parsedIni = new();
		List<Dictionary<string, object>> levels = new() { parsedIni };

		for (int i = 0; i < data.Length; i++) {
			var currentLevel = levels.Last();
			// Headers.
			if (data[i].token == Token.Header) {
				if (data[i].value.ToLower() != "!end") {
					if (levels.Count > 1) {
						levels.RemoveAt(levels.Count - 1);
					}
					Dictionary<string, object> newLevel = new();
					levels.Last()[data[i].value] = newLevel;
					levels.Add(newLevel);
				} else {
					if (levels.Count == 1) {
						throw new Exception("INI Error: Closed root level.");
					}
					levels.RemoveAt(levels.Count - 1);
				}
				i++;
				if (data[i].token != Token.LineBreak) {
					throw new Exception("INI Error: Unexpected value, expected linebreak.");
				}
			}
			// Variable asigment.
			else if (data[i].token is Token.Literal or Token.String) {
				string key = data[i].value;
				i++;
				if (i >= data.Length || data[i].token == Token.LineBreak) {
					throw new Exception("INI Error: Line ended before variable asigment.");
				}
				if (data[i].token == Token.Assigment) {
					i++; 
					if (data[i].token == Token.Number) {
						currentLevel[key] = Decimal.Parse(
							data[i].value, NumberStyles.Any, CultureInfo.InvariantCulture
						);
					}
					else if (data[i].token is Token.Literal or Token.String) {
						currentLevel[key] = data[i].value;
					}
					else {
						throw new Exception("INI Error: Invalid expression term.");
					}
				}
				else {
					throw new Exception("INI Error: Variable assigment unfinished.");
				}
				i++;
				if (data[i].token != Token.LineBreak) {
					throw new Exception("INI Error: Unexpected value, expected linebreak.");
				}
			}
		}

		return parsedIni;
	}

	static (Token token, string value)[] TokenizeIni(string fileText) {
		fileText = fileText.ReplaceLineEndings("\n");
		fileText = fileText.Replace("\n\n", "\n");
		List<(Token token, string value)> data = new();

		for (int pos = 0; pos < fileText.Length; pos++) {
			// LineBreaks
			if (fileText[pos] == ',' || fileText[pos] == '\n') {
				if (data.Count >= 1 && data[^1].token != Token.LineBreak) {
					data.Add((Token.LineBreak, ""));
				}
			}
			// WhiteSpace
			else if (Char.IsWhiteSpace(fileText[pos]) && fileText[pos] != '\n') {
				while (pos + 1 < fileText.Length &&
					Char.IsWhiteSpace(fileText[pos+1]) && fileText[pos+1] != '\n'
				) {
					pos++;
				}
			}
			// Comments
			else if (fileText[pos] == ';') {
				while (pos + 1 < fileText.Length && fileText[pos+1] != '\n') {
					pos++;
				}
			}
			// Header start
			else if (fileText[pos] == '[') {
				pos++;
				string text = ParseHeader(fileText, ref pos);
				data.Add((Token.Header, text));
			}
			// String start
			else if (fileText[pos] == '"') {
				pos++;
				string text = ParseString(fileText, ref pos);
				data.Add((Token.String, text));
			}
			// Token
			else if (fileText[pos] == '=') {
				data.Add((Token.Assigment, ""));
			}
			// Literals
			else {
				string text = ParseLiteral(fileText, ref pos);
				data.Add(ParseLiteralType(text));
			}
		}
		if (data.Count >= 1 && data[^1].token != Token.LineBreak) {
			data.Add((Token.LineBreak, ""));
		}
		return data.ToArray();
	}

	static string ParseHeader(string fileText, ref int pos) {
		string headerText = "";
		while (fileText[pos] != ']') {
			if (pos == fileText.Length - 1) {
				throw new Exception("INI parse error: Header was not closed before EOF");
			}
			if (fileText[pos] == '\n') {
				throw new Exception("INI parse error: Header was not closed before end of line.");
			}
			headerText += fileText[pos];
			pos++;
		}
		return headerText;
	}

	static string ParseString(string fileText, ref int pos) {
		string stringText = "";
		while (fileText[pos] != '"') {
			if (pos == fileText.Length - 1) {
				throw new Exception("INI parse error: String was not closed before EOF");
			}
			if (fileText[pos] == '\n') {
				throw new Exception("INI parse error: String was not closed before end of line.");
			}
			stringText += fileText[pos];
			pos++;
		}
		return stringText;
	}

	static string ParseLiteral(string fileText, ref int pos) {
		string stringText = "";
		while (pos < fileText.Length && !Char.IsWhiteSpace(fileText[pos]) &&
			(Char.IsLetterOrDigit(fileText[pos]) || fileText[pos] == '.')
		) {
			stringText += fileText[pos];
			pos++;
		}
		pos--;
		if (stringText[^1] == '.') {
			throw new Exception("INI parse error: Literals cannot end with '.'");
		}
		return stringText;
	}

	static (Token token, string value) ParseLiteralType(string literal) {
		// For numbers.
		if (literal.All((char arg) => (Char.IsDigit(arg) || arg == '.'))) {
			if (literal.StartsWith(".")) {
				literal = "0" + literal;
			}
			return (Token.Number, literal);
		}
		// Variable names.
		return (Token.Literal, literal);
	}
}
