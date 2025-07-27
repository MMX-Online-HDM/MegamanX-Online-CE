using System;

namespace MMXOnline;

public class PreOptionsMenu : IMainMenu {
	public int selectY;
	public int[] optionPos = new int[10];
	public int lineH = 14;
	public MainMenu prevMenu1;
	public IMainMenu prevMenu;
	public string message;
	public Action yesAction;
	public bool inGame;
	public bool isAxl;
	public float startX = Global.halfScreenW;
	public float Time = 1, Time2;
	public bool Confirm = false, Confirm2 = false;
	public int[] optionPos2 = {
		(int)Global.halfScreenW - 60,
		(int)Global.halfScreenW - 60,
		(int)Global.halfScreenW - 60,
		(int)Global.halfScreenW - 35,
		(int)Global.halfScreenW - 40,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 50,
	};
	public int[] optionPos3 = {
		(int)Global.halfScreenW + 60,
		(int)Global.halfScreenW + 60,
		(int)Global.halfScreenW + 60,
		(int)Global.halfScreenW + 35,
		(int)Global.halfScreenW + 40,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 50,

	};
	public PreOptionsMenu(MainMenu prevMenu1, bool inGame) {
		this.prevMenu1 = prevMenu1;
		this.inGame = inGame;
		for (int i = 0; i < optionPos.Length; i++) {
			optionPos[i] = 45 + lineH * i;
		}
	}

	public void TimeUpdate() {
		if (!inGame) {
			if (Confirm == false) Time -= Global.spf * 2;
			if (Time <= 0) {
				Confirm = true;
				Time = 0;
			}
			if (Global.input.isPressedMenu(Control.MenuBack)) Confirm2 = true;
			if (Confirm2 == true) Time2 += Global.spf * 2;
		}
		if (inGame) {
			Time = 0;
			Time2 = 0;
		}
	}
	public void update() {
		Helpers.menuUpDown(ref selectY, 0, 9);
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			int? charNum = null;
			if (selectY == 4) charNum = 0;
			if (selectY == 5) charNum = 1;
			if (selectY == 6) charNum = 2;
			if (selectY == 7) charNum = 3;
			if (selectY == 8) charNum = 4;
			Menu.change(new OptionsMenu(this, inGame, charNum, selectY));
		}
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectY == 3) { Menu.change(new PreControlMenu(this, false)); }
			if (!inGame)
				if (selectY == 9) { Menu.change(new SoundMode(this, false)); }
		}
		if (Options.main.blackFade) {
			TimeUpdate();
			if (Time2 >= 1 && !inGame) {
				Menu.change(prevMenu1);
				if (prevMenu1 != null) {
					prevMenu1.Time = 0;
					prevMenu1.Time2 = 1;
					prevMenu1.Confirm = false;
					prevMenu1.Confirm2 = false;
				}
			}
		} else {
			if (Global.input.isPressedMenu(Control.MenuBack) && !inGame) {
				Menu.change(prevMenu1);
			}
		}
		if (Global.input.isPressedMenu(Control.MenuBack) && inGame) {
			Menu.change(prevMenu1);
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			//DrawWrappers.DrawTextureMenu(
			//Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace)
			//);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		}
		//Global.sprites["cursor"].drawToHUD(0, startX - 60, 48 + (selectY * lineH));
		FontType tileFont = FontType.Purple;
		FontType menuFont = FontType.DarkBlue;
		if (inGame) {
			tileFont = FontType.Yellow;
			menuFont = FontType.Blue;
		}
		if (Global.frameCount % 60 < 30) {
			for (int i = 0; i < 10; i++) {
				if (selectY == i) {
					Fonts.drawText(FontType.Blue, "<", optionPos2[i], optionPos[i], Alignment.Center, selected: selectY == i,
					selectedFont: inGame ? FontType.Orange : FontType.DarkOrange);
					Fonts.drawText(FontType.Blue, ">", optionPos3[i]-1, optionPos[i], Alignment.Center, selected: selectY == i,
					selectedFont: inGame ? FontType.Orange : FontType.DarkOrange);
				}
			}
		}
		Global.sprites["optionMode"].drawToHUD(inGame ? 2 : 2, Global.screenW * 0.5f + 1, 20);
		//Fonts.drawText(tileFont, "SETTINGS", Global.screenW * 0.5f, 20, Alignment.Center);

		Fonts.drawText(menuFont, "General settings", startX, optionPos[0], Alignment.Center, selected: selectY == 0);
		Fonts.drawText(menuFont, "Gameplay settings", startX, optionPos[1], Alignment.Center, selected: selectY == 1);
		Fonts.drawText(menuFont, "Graphics settings", startX, optionPos[2], Alignment.Center, selected: selectY == 2);
		Fonts.drawText(menuFont, "Controls", startX, optionPos[3], Alignment.Center, selected: selectY == 3);
		Fonts.drawText(menuFont, "X settings", startX, optionPos[4], Alignment.Center, selected: selectY == 4);
		Fonts.drawText(menuFont, "Zero settings", startX, optionPos[5], Alignment.Center, selected: selectY == 5);
		Fonts.drawText(menuFont, "Vile settings", startX, optionPos[6], Alignment.Center, selected: selectY == 6);
		Fonts.drawText(menuFont, "Axl settings", startX, optionPos[7], Alignment.Center, selected: selectY == 7);
		Fonts.drawText(menuFont, "Sigma settings", startX, optionPos[8], Alignment.Center, selected: selectY == 8);
		if (!inGame)
			Fonts.drawText(menuFont, "Sound Mode", startX, optionPos[9], Alignment.Center, selected: selectY == 9, selectedFont: FontType.Purple);

		Fonts.drawTextEX(FontType.Grey, "[OK]: Choose, [BACK]: Back", Global.halfScreenW, 198, Alignment.Center);
		if (Options.main.blackFade) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time);
				DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time2);
			}
		}
	}
}
