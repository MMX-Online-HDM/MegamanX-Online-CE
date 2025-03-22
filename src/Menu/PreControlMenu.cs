using System;
using System.Collections.Generic;

namespace MMXOnline;

public class PreControlMenu : IMainMenu {
	public int selArrowPosY;
	public Point optionPos1 = new Point(40, 70);
	public Point optionPos2 = new Point(40, 90);
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
			return "< " + str + " >";
		}
		return "  " + str + "  ";
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			Global.sprites["cursor"].drawToHUD(0, optionPos1.x - 10, 73 + (selArrowPosY * 20));
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			Global.sprites["cursor"].drawToHUD(0, optionPos1.x - 10, 73 + (selArrowPosY * 20));
		}

		Fonts.drawText(
			FontType.Yellow, "SELECT INPUT TO CONFIGURE",
			Global.screenW * 0.5f, 24, Alignment.Center
		);

		Fonts.drawText(
			FontType.FBlue, getLeftRightStr("KEYBOARD " + getCharStr(0)),
			optionPos1.x, optionPos1.y, selected: selArrowPosY == 0
		);

		if (Control.isJoystick()) {
			Fonts.drawText(
				FontType.FBlue, getLeftRightStr("CONTROLLER " + getCharStr(1)),
				optionPos2.x, optionPos2.y, selected: selArrowPosY == 1
			);
		} else {
			Fonts.drawText(
				FontType.Grey, "CONTROLLER (NOT DETECTED)",
				optionPos2.x, optionPos2.y, selected: selArrowPosY == 1
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
			FontType.Grey, "[OK]: Choose, [BACK]: Back",
			Global.halfScreenW, Global.screenH - 16, Alignment.Center
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
