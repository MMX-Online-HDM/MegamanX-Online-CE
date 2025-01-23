using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SFML.Graphics;
using SFML.System;

namespace MMXOnline;

public class Fonts {
	public static void drawTextEX(
		FontType fontType, string textStr, float x, float y,
		Alignment alignment = Alignment.Left, bool isWorldPos = false, bool selected = false,
		FontType? selectedFont = null, long depth = 0, byte? alpha = null, Color? color = null
	) {
		drawText(
			fontType, Helpers.controlText(textStr), x, y,
			alignment, isWorldPos, selected,
			selectedFont, depth, alpha, color
		);
	}

	public static void drawText(
		FontType fontType, string textStr, float x, float y,
		Alignment alignment = Alignment.Left, bool isWorldPos = false, bool selected = false,
		FontType? selectedFont = null, long depth = ZIndex.HUD, byte? alpha = null, Color? color = null,
		bool isLoading = false
	) {
		// To prevent crashes.
		if (string.IsNullOrEmpty(textStr)) { return; }
		if (isWorldPos && Global.level == null) { return; }
		// Get the font propieties.
		if (color == null) {
			color = Color.White;
		}
		string[] textLines = textStr.Split('\n');
		string fontStr = "";
		if (!selected) {
			fontStr = getFontSrt(fontType);
		} else {
			if (selectedFont == null) {
				selectedFont = getFontAlt(fontType);
			}
			fontStr = getFontSrt(selectedFont.Value);
		}
		bool deferred = false;
		int fontTextureSize = 8;
		int fontGridSpacing = 1;
		int fontDefaultWidth = 7;
		int fontSpacing = 1;
		int newLineSpacing = 10;
		int fontSpaceWidth = 8;
		if (baseFontData.ContainsKey(fontStr)) {
			fontTextureSize = baseFontData[fontStr][0];
			fontGridSpacing = baseFontData[fontStr][1];
			fontDefaultWidth = baseFontData[fontStr][2];
			fontSpacing = baseFontData[fontStr][3];
		}
		// Set up drawing texture.
		Texture bitmapFontTexture = Global.fontTextures[fontStr];
		BatchDrawable batchDrawable = new BatchDrawable(bitmapFontTexture);
		// For in-stage drawing.
		if (isWorldPos) {
			x = (x - Global.level.camX) / Global.viewSize;
			y = (y - Global.level.camY) / Global.viewSize;
		}
		// Draw every character.
		for (int line = 0; line < textLines.Length; line++) {
			string textLine = textLines[line];
			var currentXOff = MathF.Round(x);

			if (alignment == Alignment.Center) {
				int textSize = measureText(fontStr, textLine);
				currentXOff -= MathInt.Round(textSize * 0.5f);
			} else if (alignment == Alignment.Right) {
				int textSize = measureText(fontStr, textLine);
				currentXOff -= textSize;
			}
			for (int pos = 0; pos < textLine.Length; pos++) {
				char letter = textLine[pos];
				int charInt = letter;
				if (charInt > 191) {
					letter = '?';
					charInt = letter;
				}
				int rx = charInt % 16;
				int ry = MathInt.Floor(charInt / 16.0);

				var textSprite = new SFML.Graphics.Sprite(
					bitmapFontTexture, new IntRect(
						(rx * fontTextureSize) + ((rx + 1) * fontGridSpacing),
						(ry * fontTextureSize) + ((ry + 1) * fontGridSpacing),
						fontTextureSize, fontTextureSize
					)
				);
				if (alpha != null) {
					textSprite.Color = new(255, 255, 255, alpha.Value);
				}
				// For variable width fonts.
				int fontWidth = fontDefaultWidth;
				if (fontSizes.ContainsKey(fontStr)) {
					fontWidth = fontSizes[fontStr][charInt];
					fontSpaceWidth = fontWidth;
				}
				float yPos = MathF.Round(y) + (line * newLineSpacing);
				textSprite.Position = new Vector2f(currentXOff, yPos);
				// Text spacing.
				if (Char.IsWhiteSpace(letter) ||
					pos >= textLines[line].Length - 1 ||
					Char.IsWhiteSpace(textLines[line][pos + 1])
				) {
					currentXOff += fontSpaceWidth;
				} else {
					currentXOff += fontWidth + fontSpacing;
				}
				// Add to array.
				DrawWrappers.addToVertexArray(batchDrawable, textSprite);
				textSprite.Dispose();
			}
		}
		// For the loading screen.
		if (isLoading) {
			DrawWrappers.drawToHUD(batchDrawable);
			return;
		}
		// Draw on HUD or inside the stage (AKA: worldPos).
		if (isWorldPos) {
			DrawLayer drawLayer;
			if (!DrawWrappers.walDrawObjects.ContainsKey(depth)) {
				DrawWrappers.walDrawObjects[depth] = new DrawLayer();
			}
			drawLayer = DrawWrappers.walDrawObjects[depth];
			drawLayer.oneOffs.Add(new DrawableWrapper(null, batchDrawable, color.Value));
		} else {
			if (!deferred) {
				DrawWrappers.drawToHUD(batchDrawable);
			} else {
				DrawWrappers.deferredTextDraws.Add(new Action(() => { DrawWrappers.drawToHUD(batchDrawable); }));
			}
		}
	}

	public static int measureText(string fontStr, string text) {
		int size = 0;
		int fontDefaultWidth = 7;
		int fontSpacing = 1;
		int fontSpaceWidth = 8;
		if (baseFontData.ContainsKey(fontStr)) {
			fontDefaultWidth = baseFontData[fontStr][2];
			fontSpacing = baseFontData[fontStr][3];
		}
		string[] textLines = text.Split('\n');
		for (int line = 0; line < textLines.Length; line++) {
			int tempSize = 0;
			for (int pos = 0; pos < textLines[line].Length; pos++) {
				char letter = textLines[line][pos];
				int charInt = letter;
				int fontWidth = fontDefaultWidth;
				if (fontSizes.ContainsKey(fontStr)) {
					fontWidth = fontSizes[fontStr][charInt];
					fontSpaceWidth = fontWidth;
				}
				if (Char.IsWhiteSpace(letter) ||
					pos >= textLines[line].Length - 1 ||
					Char.IsWhiteSpace(textLines[line][pos + 1])
				) {
					tempSize += fontSpaceWidth;
				} else {
					tempSize += fontWidth + fontSpacing;
				}
			}
			if (tempSize > size) {
				size = tempSize;
			}
		}
		return size;
	}

	public static int measureText(FontType fontType, string text) {
		return measureText(getFontSrt(fontType), text);
	}

	public static Dictionary<string, int[]> fontSizes = new();
	public static Dictionary<string, int[]> baseFontData = new();

	public static void loadFontSizes() {
		// Get all files with ".fontdef" extension.
		string[] files = Directory.GetFiles(Global.assetPath + "assets/fonts/", "*.fontdef");
		foreach (string fileLocation in files) {
			// Load text and replace all the new lines to make it easier to parse.
			// While also splitting it based on ";"
			string fileName = RemoveFromEnd(fileLocation.Split("/").Last(), ".fontdef");
			string[] text = File.ReadAllText(fileLocation).Replace("\n", "").Replace("\r", "").Split(";");
			// Parse basic data.
			string[] strBData = text[0].Split(",");
			int[] basicData = new int[strBData.Length];
			for (int i = 0; i < basicData.Length; i++) {
				basicData[i] = Int32.Parse(strBData[i]);
			}
			baseFontData[fileName] = basicData;
			// We check if optional detailed info exists.
			if (text.Length < 2) {
				continue;
			}
			// Optional info of individual characters size.
			string[] strSData = text[1].Split(",");
			int[] sizeData = new int[strSData.Length];
			for (int i = 0; i < sizeData.Length; i++) {
				sizeData[i] = Int32.Parse(strSData[i]);
			}
			fontSizes[fileName] = sizeData;
		}
	}

	public static void loadFontSprites() {
		var fontSprites = Helpers.getFiles(Global.assetPath + "assets/fonts", true, "png", "psd");
		for (int i = 0; i < fontSprites.Count; i++) {
			string path = fontSprites[i];
			Texture texture = new Texture(path);
			Global.fontTextures[Path.GetFileNameWithoutExtension(path)] = texture;
		}
	}

	public static string RemoveFromEnd(string str, string suffix) {
		if (str.EndsWith(suffix)) {
			return str.Substring(0, str.Length - suffix.Length);
		}

		return str;
	}

	public static string getFontSrt(FontType fontType) {
		return fontType switch {
			FontType.Blue => "Blue",
			FontType.DarkBlue => "DarkBlue",
			FontType.Golden => "Golden",
			FontType.Green => "Green",
			FontType.DarkGreen => "DarkGreen",
			FontType.Grey => "Grey",
			FontType.LightGrey => "LightGrey",
			FontType.Orange => "Orange",
			FontType.DarkOrange => "DarkOrange",
			FontType.Pink => "Pink",
			FontType.Purple => "Purple",
			FontType.DarkPurple => "DarkPurple",
			FontType.Red => "Red",
			FontType.RedishOrange => "RedishOrange",
			FontType.Yellow => "Yellow",
			FontType.FBlue => "FixedBlue",
			FontType.FOrange => "FixedOrange",
			FontType.BlueMenu => "BlueMenu",
			FontType.OrangeMenu => "OrangeMenu",
			_ => "Blue"
		};
	}

	public static FontType getFontAlt(FontType fontType) {
		return fontType switch {
			FontType.Orange => FontType.Red,
			FontType.FBlue => FontType.FOrange,
			FontType.FOrange => FontType.FBlue,
			FontType.BlueMenu => FontType.OrangeMenu,
			FontType.OrangeMenu => FontType.BlueMenu,
			FontType.DarkBlue => FontType.DarkOrange,
			_ => FontType.Orange
		};
	}
}

public enum FontType {
	Blue,
	DarkBlue,
	Golden,
	Green,
	DarkGreen,
	Grey,
	LightGrey,
	Orange,
	DarkOrange,
	Pink,
	Purple,
	DarkPurple,
	Red,
	RedishOrange,
	Yellow,
	FBlue,
	FOrange,
	BlueMenu,
	OrangeMenu
}
