using System.IO;
using System;
using System.Collections.Generic;

namespace MMXOnline;

public class PreControlMenu : IMainMenu {
	public int selArrowPosY;
	public Point optionPos1 = new Point(Global.halfScreenW, 70);
	public Point optionPos2 = new Point(Global.halfScreenW, 90);
	public Point optionPos3 = new Point(40, 110);
	public Point optionPos5 = new Point(50, 170);
	public IMainMenu prevMenu;
	public string message = "";
	public Action? yesAction;
	public bool inGame;
	public bool isAxl;

	public List<int> cursorToCharNum;

	public PreControlMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		cursorToCharNum = new List<int>() { -1, -1 };
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.MenuLeft)) {
			cursorToCharNum[selArrowPosY]--;
			if (cursorToCharNum[selArrowPosY] < -2) {
				cursorToCharNum[selArrowPosY] = 5;
			}
		} else if (Global.input.isPressedMenu(Control.MenuRight)) {
			cursorToCharNum[selArrowPosY]++;
			if (cursorToCharNum[selArrowPosY] > 5) {
				cursorToCharNum[selArrowPosY] = -2;
			}
		}

		Helpers.menuUpDown(ref selArrowPosY, 0, 1);
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			getCharNumAndAimMode(cursorToCharNum[selArrowPosY], out int charNum, out int aimMode);
			if (selArrowPosY == 0) {
				Menu.change(new ControlMenu(this, inGame, false, charNum, aimMode));
			} else if (selArrowPosY == 1) {
				if (Control.isJoystick()) {
					Menu.change(new ControlMenu(this, inGame, true, charNum, aimMode));
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}

	private void getCharNumAndAimMode(int rawCharNum, out int charNum, out int aimMode) {
		charNum = rawCharNum;
		aimMode = 0;
		if (rawCharNum == 4) {
			charNum = 3;
			aimMode = 2;
		}
		if (rawCharNum == 5) {
			charNum = 4;
		}
	}

	public string getLeftRightStr(string str) {
		if (Global.frameCount % 60 < 30) {
			return "<  " + str + "  >";
		}
		return "" + str + "";
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			//Global.sprites["cursor"].drawToHUD(0, optionPos1.x - 10, 73 + (selArrowPosY * 20));
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			//Global.sprites["cursor"].drawToHUD(0, optionPos1.x - 10, 73 + (selArrowPosY * 20));
		}

		Fonts.drawText(
			FontType.Golden, "SELECT INPUT TO CONFIGURE",
			Global.screenW * 0.5f, 24, Alignment.Center
		);

		Fonts.drawText(
			inGame ? FontType.Blue : FontType.DarkBlue,
			getLeftRightStr("KEYBOARD" + getCharStr(0)),
			optionPos1.x, optionPos1.y, Alignment.Center, selected: selArrowPosY == 0,
			selectedFont: inGame ? FontType.Orange : FontType.Pink
		);

		if (Control.isJoystick()) {
			Fonts.drawText(
				inGame ? FontType.Blue : FontType.DarkBlue,
				getLeftRightStr("CONTROLLER " + getCharStr(1)),
				optionPos2.x, optionPos2.y, Alignment.Center,
				selected: selArrowPosY == 1,
				selectedFont: inGame ? FontType.Orange : FontType.Pink
			);
		} else {
			Fonts.drawText(
				FontType.Grey, "CONTROLLER (NOT DETECTED)",
				optionPos2.x, optionPos2.y, Alignment.Center,
				selected: selArrowPosY == 1,
				selectedFont: inGame ? FontType.Orange : FontType.Pink
			);
		}

		Fonts.drawTextEX(
			FontType.Grey, "Use [MLEFT]/[MRIGHT] to switch\n" +
			"character to configure controls for.\n" +
			"If a binding does not exist on\n" +
			"a char-specific control config,\n" +
			"it will fall back to the ALL config.",
			Global.halfScreenW, 130, Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.DarkPurple,
			"character to configure controls for.\n",
			Global.halfScreenW, 140, Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.DarkPurple,
			"a char-specific control config,\n",
			Global.halfScreenW, 160, Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.Grey, "[OK]: Choose, [BACK]: Back",
			Global.halfScreenW, Global.screenH - 18, Alignment.Center
		);
	}

	public string getCharStr(int yPos) {
		int charNum = cursorToCharNum[yPos];
		return charNum switch {
			-2 => "(Menu)",
			-1 => "(All)",
			0 => "(X)",
			1 => "(Zero)",
			2 => "(Vile)",
			3 => "(Directional Axl)",
			4 => "(Cursor Axl)",
			5 => "(Sigma)",
			_ => "(ERROR)"
		};
	}
}
public class SoundMode : IMainMenu {
	public int selArrowPosY;
	public Point optionPos1 = new Point(Global.halfScreenW, 70);
	public IMainMenu prevMenu;
	public string message = "";
	private List<string> musicFiles = new List<string>();
	private List<string> soundFiles = new List<string>();
	int musicIndex = 0;
	int sfxIndex = 0;
	int visibleCount = 3;
	int musicScroll = 0;
	int sfxScroll = 0;
	public bool inGame;
	private bool selectingMusic = true; // si está en música o SFX

	public SoundMode(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		string path = Global.assetPath + "assets/music";
		if (Directory.Exists(path)) {
			var files = Helpers.getFiles(path, true, "wav", "ogg", "flac");
			foreach (var file in files) {
				string relativePath = Path.GetRelativePath(path, file).Replace("\\", "/");
				musicFiles.Add(relativePath);
			}
		}
		string soundsPath = Global.assetPath + "assets/sounds";
		if (Directory.Exists(soundsPath)) {
			var files = Helpers.getFiles(soundsPath, true, "wav", "ogg", "flac");
			foreach (var file in files) {
				string relativePath = Path.GetRelativePath(soundsPath, file).Replace("\\", "/");
				soundFiles.Add(relativePath);
			}
		}
	}

	public void update() {
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
		if (Global.input.isPressedMenu(Control.Special2)) {
			selectingMusic = !selectingMusic;
		}
		if (Global.input.isPressedMenu(Control.MenuUp)) {
			if (selectingMusic) {
				musicIndex = Math.Max(0, musicIndex - 1);
				if (musicIndex < musicScroll) musicScroll = musicIndex;
			} else {
				sfxIndex = Math.Max(0, sfxIndex - 1);
				if (sfxIndex < sfxScroll) sfxScroll = sfxIndex;
			}
		}
		if (Global.input.isPressedMenu(Control.MenuDown)) {
			if (selectingMusic) {
				musicIndex = Math.Min(musicFiles.Count - 1, musicIndex + 1);
				if (musicIndex >= musicScroll + visibleCount) musicScroll = musicIndex - visibleCount + 1;
			} else {
				sfxIndex = Math.Min(soundFiles.Count - 1, sfxIndex + 1);
				if (sfxIndex >= sfxScroll + visibleCount) sfxScroll = sfxIndex - visibleCount + 1;
			}
		}
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectingMusic && musicFiles.Count > 0) {
				string file = Path.GetFileNameWithoutExtension(musicFiles[musicIndex]);
				Global.changeMusic(file);
			} else if (!selectingMusic && soundFiles.Count > 0) {
				string file = Path.GetFileNameWithoutExtension(soundFiles[sfxIndex]);
				Global.playSound(file);
			}
		}
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
		float y = 20;
		Fonts.drawText(FontType.Yellow, "Select Back Ground Music", Global.halfScreenW, y, Alignment.Center);
		y += 24;
		for (int i = musicScroll; i < Math.Min(musicScroll + visibleCount, musicFiles.Count); i++) {
			string name = Path.GetFileNameWithoutExtension(musicFiles[i]);
			FontType font = (selectingMusic && i == musicIndex) ? FontType.Orange : FontType.Blue;
			Fonts.drawText(font, name, Global.halfScreenW, y, Alignment.Center);
			y += 20;
		}
		y += 2;
		Fonts.drawText(FontType.Yellow, "Select Sound Effect", Global.halfScreenW, y, Alignment.Center);
		y += 24;
		for (int i = sfxScroll; i < Math.Min(sfxScroll + visibleCount, soundFiles.Count); i++) {
			string name = Path.GetFileNameWithoutExtension(soundFiles[i]);
			FontType font = (!selectingMusic && i == sfxIndex) ? FontType.Orange : FontType.Blue;
			Fonts.drawText(font, name, Global.halfScreenW, y, Alignment.Center);
			y += 20;
		}
		Fonts.drawTextEX(
			FontType.Grey,
			"[MUP]/[MDOWN]: Navigate, [OK]: Choose, [BACK]: Back, [CMD]: Change Select",
			Global.halfScreenW-4, Global.screenH - 18, Alignment.Center
		);
	}
}