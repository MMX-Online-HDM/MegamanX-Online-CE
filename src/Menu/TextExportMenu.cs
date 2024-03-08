using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MMXOnline;

public class TextExportMenu : IMainMenu {
	string textFileName;
	string text;
	List<string> lines;
	IMainMenu prevMenu;
	bool inGame;
	uint textSize;

	string fileError;
	float fileTime;
	float clipboardTime;
#if WINDOWS
	bool canCopyToClipboard = true;
#else
	bool canCopyToClipboard = false;
#endif

	public TextExportMenu(string[] lines, string textFileName, string text, IMainMenu prevMenu, bool inGame = false, uint textSize = 24) {
		this.lines = new List<string>(lines);
		this.textFileName = textFileName;
		this.text = text;
		this.textSize = textSize;
		this.lines.Add(text);
		this.prevMenu = prevMenu;
		this.inGame = inGame;
	}

	/// <summary>
	/// Sets clipboard to value.
	/// </summary>
	/// <param name="value">String to set the clipboard to.</param>
	public static void SetClipboard(string value) {
		if (value == null) {
			throw new ArgumentNullException("Attempt to set clipboard with null");
		}
		Process clipboardExecutable = new Process();
		// Creates the process
		clipboardExecutable.StartInfo = new ProcessStartInfo {
			RedirectStandardInput = true,
			FileName = @"clip",
			CreateNoWindow = true
		};
		clipboardExecutable.Start();

		// CLIP uses STDIN as input.
		// When we are done writing all the string, close it so clip doesn't wait and get stuck
		clipboardExecutable.StandardInput.Write(value);
		clipboardExecutable.StandardInput.Close();

		return;
	}

	public void update() {
		Helpers.decrementTime(ref fileTime);
		Helpers.decrementTime(ref clipboardTime);

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		} else if (Global.input.isPressedMenu(Control.MenuConfirm) &&
			canCopyToClipboard && clipboardTime == 0
		) {
#if WINDOWS
			SetClipboard(text);
			clipboardTime = 2;
#endif
		} else if (Global.input.isPressedMenu(Control.MenuAlt) && fileTime == 0) {
			fileError = Helpers.WriteToFile(textFileName + ".txt", text);
			fileTime = 2;
		}
	}

	public void render() {
		int top = MathInt.Round(Global.screenH * 0.475f) - (lines.Count * 10 / 2) - 5;
		int bot = 198;

		if (inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
		}

		int i = 0;
		for (; i < lines.Count; i++) {
			Fonts.drawText(
				FontType.Green, lines[i], Global.screenW / 2, top + (i * 10),
				alignment: Alignment.Center
			);
		}
		Fonts.drawText(
			FontType.Orange, "TEXT EXPORT",
			Global.screenW / 2, 20, alignment: Alignment.Center
		);
		if (canCopyToClipboard) {
			Fonts.drawTextEX(
				FontType.Grey, clipboardTime == 0 ? "[OK]: copy to clipboard" : "Copied to clipboard.",
				Global.screenW / 2, bot - 20, alignment: Alignment.Center
			);
		}
		string fileMessage = (
			string.IsNullOrEmpty(fileError)
			? ("Wrote to file " + textFileName + ".txt in game folder")
			: "Failed to write to file."
		);
		Fonts.drawTextEX(
			FontType.Grey, fileTime == 0 ? "[ALT]: export to file" : fileMessage,
			Global.screenW / 2, bot - 10, alignment: Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.Grey, "[BACK]: Back", Global.screenW / 2,
			bot, alignment: Alignment.Center
		);
	}
}
